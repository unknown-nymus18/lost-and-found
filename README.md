# Campus Lost & Found Backend

Standalone ASP.NET Core 10 API for the Campus Lost & Found system. The Blazor frontend runs separately and communicates with this server through REST APIs and SignalR.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker, or access to a PostgreSQL database

## Quick start

### 1. Configure the environment

Copy the example environment file:

```bash
cp .env.example .env
```

The default values work with the included Docker PostgreSQL service. For another database, update `DATABASE_URL` in `.env`:

```env
DATABASE_URL=Host=localhost;Port=5432;Database=campus_lost_found;Username=postgres;Password=postgres
Jwt__Key=replace-with-a-random-secret-containing-at-least-32-bytes
```

Do not commit `.env`. It is already ignored by Git.

### 2. Start PostgreSQL

```bash
docker compose up -d
```

Skip this step when `DATABASE_URL` points to an existing PostgreSQL server.

### 3. Start the API

```bash
dotnet restore
dotnet run
```

The API automatically applies EF Core migrations and adds development seed data the first time it starts.

## Swagger documentation

After starting the API, open:

```text
http://localhost:5080/swagger
```

Swagger lists all available endpoints and lets you test them from the browser.

### Test authenticated endpoints

1. Open `POST /api/auth/login`.
2. Use a demo account:

```json
{
  "email": "admin@ug.edu.gh",
  "password": "Admin@123"
}
```

3. Copy the `token` from the response.
4. Click **Authorize** at the top of Swagger.
5. Paste the token and submit.
6. You can now call protected endpoints.

## Useful URLs

| Service         | URL                                        |
| --------------- | ------------------------------------------ |
| Swagger         | `http://localhost:5080/swagger`            |
| API root        | `http://localhost:5080`                    |
| Health check    | `http://localhost:5080/health`             |
| SignalR hub     | `http://localhost:5080/hubs/notifications` |
| Uploaded photos | `http://localhost:5080/uploads/{filename}` |

## Demo accounts

| Role    | Email                   | Password      |
| ------- | ----------------------- | ------------- |
| Admin   | `admin@ug.edu.gh`       | `Admin@123`   |
| Student | `ama@st.ug.edu.gh`      | `Password123` |
| Student | `kofi@st.ug.edu.gh`     | `Password123` |
| Student | `kingsley@st.ug.edu.gh` | `Password123` |

## Database migrations

Migrations run automatically when the API starts. After changing a model, create a new migration with:

```bash
dotnet ef migrations add DescribeYourChange --output-dir Data/Migrations
```

## Blazor frontend setup

Add the Blazor application's URL to `Cors:AllowedOrigins` in `appsettings.Development.json`:

```json
{
  "Cors": {
    "AllowedOrigins": ["https://localhost:7081"]
  }
}
```

Send the JWT on protected API requests:

```http
Authorization: Bearer <token>
```

The frontend can connect to `/hubs/notifications` with the same JWT to receive `MatchFound` SignalR events.

## Stop the local database

```bash
docker compose down
```

To also delete all Docker PostgreSQL data:

```bash
docker compose down -v
```
