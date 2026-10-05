using System.Diagnostics;
using System.Text;
using CampusLostAndFound.Data;
using CampusLostAndFound.Hubs;
using CampusLostAndFound.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    DotNetEnv.Env.Load();
}

var connectionString =
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "PostgreSQL is not configured. Set DATABASE_URL or ConnectionStrings:Default.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsql =>
            npgsql.MigrationsHistoryTable(
                "__EFMigrationsHistory",
                "campus_lost_found")));

var jwt =
    builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? new JwtOptions();

if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be configured with a secret containing at least 32 bytes.");
}

if (!builder.Environment.IsDevelopment() &&
    string.IsNullOrWhiteSpace(builder.Configuration["BlobStorage:ConnectionString"]))
{
    throw new InvalidOperationException(
        "Blob storage is not configured. Set BlobStorage__ConnectionString in the app settings.");
}

builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<FileStorage>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<MatchingService>();
builder.Services.AddScoped<MatchQueryService>();
builder.Services.AddScoped<ClaimService>();
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

        context.ProblemDetails.Detail ??= context.ProblemDetails.Status switch
        {
            StatusCodes.Status404NotFound => "The requested endpoint was not found.",
            StatusCodes.Status405MethodNotAllowed => "This endpoint does not support that HTTP method.",
            StatusCodes.Status500InternalServerError => "An unexpected server error occurred. Refer to the trace ID when reporting it.",
            _ => null
        };
    };
});

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Campus Lost & Found API",
        Version = "v1"
    });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT returned by /api/auth/login.",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", scheme);

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [scheme] = Array.Empty<string>()
    });
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwt.Key)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken =
                    context.Request.Query["access_token"];

                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path
                        .StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.Headers["WWW-Authenticate"] = "Bearer";
                await Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    detail: "A valid Bearer token is required to access this endpoint.")
                    .ExecuteAsync(context.HttpContext);
            },
            OnForbidden = async context =>
            {
                await Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    detail: "You do not have permission to access this endpoint.")
                    .ExecuteAsync(context.HttpContext);
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Campus Lost & Found API v1");
});

app.UseRouting();
app.UseCors("AllowAll");
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    name = "Campus Lost & Found API",
    version = "v1",
    documentation = "/swagger",
    agents = "/api/agents/context",
    health = "/health"
}));

app.MapHealthChecks("/health");

var agentsContext = ReadAgentsContext();
app.MapGet("/api/agents/context", () => agentsContext is null
        ? Results.NotFound()
        : Results.Text(agentsContext, "text/markdown; charset=utf-8"))
    .AllowAnonymous();

app.MapGet("/uploads/{name}", async (string name, FileStorage files) =>
{
    var content = await files.ReadAsync(name);
    return content is null
        ? Results.NotFound()
        : Results.File(content, FileStorage.ContentType(name));
});

app.MapControllers();

app.MapHub<NotificationHub>("/hubs/notifications");

await DbSeeder.SeedAsync(app.Services);

app.Run();

static string? ReadAgentsContext()
{
    using var stream = typeof(Program).Assembly.GetManifestResourceStream("AGENTS.md");
    if (stream is null) return null;
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}
