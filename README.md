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

## API errors

Application-generated errors use JSON problem details (`application/problem+json`) with `status`, `title`, and `detail`; validation errors include an `errors` object keyed by field. Invalid or missing Bearer tokens return `401`, insufficient permissions return `403`, missing resources return `404`, and duplicate accounts, pending claims, or already-decided claims return `409`. Unexpected server failures return `500` with a trace ID instead of exposing internal exception details. Successful response bodies are unchanged.

## Report dates

When creating a report, send `date` as an ISO 8601 timestamp with an offset (for example, `2026-09-30T14:30:00+00:00`) or as a date-only value (`2026-09-30`). Date-only values and timestamps without an offset are interpreted as UTC; offset timestamps are converted to UTC before storage.

## Item details

Use the report's `kind` and `id` from `GET /api/reports` to load its details:

- `GET /api/reports/lost/{id}`
- `GET /api/reports/found/{id}`

Both endpoints are public and return the same `ReportDto` as the browse list, or `404` if the report does not exist. Lost and found reports have separate ID sequences, so the kind is required to identify the item.

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
