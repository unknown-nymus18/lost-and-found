# Campus Lost & Found API

ASP.NET Core 10 backend for the Campus Lost & Found system. The repository now contains the backend only; the frontend team can build and maintain a separate Blazor application against the REST and SignalR endpoints.

## Backend features

- ASP.NET Core Web API controllers under `/api/*`
- PostgreSQL with EF Core and Npgsql
- JWT bearer authentication with Student and Admin roles
- SignalR match notifications at `/hubs/notifications`
- Lost/found matching and claim approval workflows
- Optional item photo uploads under `wwwroot/uploads`
- Swagger API documentation in development
- Seeded demo users and reports

## Requirements

- .NET 10 SDK
- Docker with Docker Compose, or an existing PostgreSQL server

## Start the backend

From this directory:

```bash
docker compose up -d
dotnet restore
dotnet run
```

The included PostgreSQL container uses the development connection string already present in `appsettings.json`. On startup, the API applies pending EF Core migrations and seeds demo data when the database is empty.

Default URLs:

- API: `http://localhost:5080`
- Swagger: `http://localhost:5080/swagger`
- Health check: `http://localhost:5080/health`
- SignalR: `http://localhost:5080/hubs/notifications`

Stop PostgreSQL with:

```bash
docker compose down
```

Use `docker compose down -v` only when you intentionally want to delete all local database data.

## PostgreSQL configuration

The default development connection is:

```text
Host=localhost;Port=5432;Database=campus_lost_found;Username=postgres;Password=postgres
```

For another PostgreSQL server, set `DATABASE_URL`. You can copy `.env.example` to `.env`; `DotNetEnv` loads it before ASP.NET configuration starts.

```bash
cp .env.example .env
```

Environment values override `appsettings.json`. Use a strong `Jwt__Key` and database password outside local development.

## Database migrations

Migrations are stored in `Data/Migrations` and are applied automatically when the API starts.

After changing an entity or `AppDbContext`, create a migration and review it before running the API:

```bash
dotnet ef migrations add DescribeYourChange --output-dir Data/Migrations
dotnet ef database update
```

`dotnet ef database update` is optional during normal startup because `Program.cs` calls `MigrateAsync`.

## Blazor frontend integration

### CORS

Add the Blazor application's exact origin to `Cors:AllowedOrigins` in `appsettings.Development.json` or production configuration. The default development origins include common HTTP/HTTPS ports `5000`, `5001`, `5081`, and `7081`.

Do not include a trailing slash in an origin. For example:

```json
{
  "Cors": {
    "AllowedOrigins": ["https://localhost:7081"]
  }
}
```

### Authentication

Register or log in to receive an `AuthResponse` containing the JWT:

- `POST /api/auth/register`
- `POST /api/auth/login`

Send that token on protected API requests:

```http
Authorization: Bearer <token>
```

### SignalR notifications

The hub authenticates the connection and automatically puts it in the signed-in user's notification group. Clients must not provide a user ID.

A Blazor client can connect with `Microsoft.AspNetCore.SignalR.Client`:

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/notifications", options =>
    {
        options.AccessTokenProvider = () => Task.FromResult<string?>(jwtToken);
    })
    .WithAutomaticReconnect()
    .Build();

connection.On<MatchAlert>("MatchFound", alert =>
{
    // Update the Blazor UI.
});

await connection.StartAsync();
```

`MatchAlert` contains `MatchId`, `Score`, and `Message`.

### Photos

JSON-only report creation:

- `POST /api/reports/lost`
- `POST /api/reports/found`

Multipart report creation with an optional `Photo` field:

- `POST /api/reports/lost/with-photo`
- `POST /api/reports/found/with-photo`

Multipart fields are `Title`, `Description`, `Category`, `Location`, `Date`, and optional `Photo`. Supported image extensions are `.jpg`, `.jpeg`, `.png`, `.webp`, and `.gif`, with a 5 MB file limit.

The returned `PhotoUrl` is relative, such as `/uploads/abc123.jpg`. Prefix it with the API base URL when displaying it from Blazor.

## Main API endpoints

| Method | Route                           | Access        |
| ------ | ------------------------------- | ------------- |
| POST   | `/api/auth/register`            | Anonymous     |
| POST   | `/api/auth/login`               | Anonymous     |
| GET    | `/api/reports`                  | Anonymous     |
| GET    | `/api/reports/mine`             | Authenticated |
| POST   | `/api/reports/lost`             | Authenticated |
| POST   | `/api/reports/found`            | Authenticated |
| POST   | `/api/reports/lost/with-photo`  | Authenticated |
| POST   | `/api/reports/found/with-photo` | Authenticated |
| GET    | `/api/matches/mine`             | Authenticated |
| GET    | `/api/matches`                  | Admin         |
| POST   | `/api/claims`                   | Authenticated |
| GET    | `/api/claims/mine`              | Authenticated |
| GET    | `/api/claims`                   | Admin         |
| POST   | `/api/claims/{id}/decide`       | Admin         |

Swagger contains the request and response schemas for all REST endpoints.

## Demo accounts

| Role    | Email                   | Password      |
| ------- | ----------------------- | ------------- |
| Admin   | `admin@ug.edu.gh`       | `Admin@123`   |
| Student | `ama@st.ug.edu.gh`      | `Password123` |
| Student | `kofi@st.ug.edu.gh`     | `Password123` |
| Student | `kingsley@st.ug.edu.gh` | `Password123` |

These credentials are development seed data and must not be used in production.

## Project layout

```text
CampusLostAndFound/
├── Controllers/       REST API controllers
├── Data/              EF Core DbContext and seed data
├── DTOs/              API request/response contracts
├── Hubs/              Authenticated SignalR notification hub
├── Models/            Database entities and enums
├── Services/          Authentication, reports, matching, claims, files, JWT
├── wwwroot/uploads/   Uploaded item photos
├── Program.cs         API, PostgreSQL, JWT, CORS, Swagger, SignalR wiring
├── docker-compose.yml Local PostgreSQL service
└── appsettings.json   Development defaults
```

The frontend is intentionally not included in this project.
