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

## Azure Blob Storage for photos

1. In your Azure Storage Account, create a **private** blob container named `report-photos` (or choose a name and set `BlobStorage__ContainerName`). Public anonymous blob access is not required.
2. In the **App Service** hosting this API, go to **Settings → Environment variables → App settings** and add `BlobStorage__ConnectionString` with the Storage Account connection string from **Security + networking → Access keys**. Optionally set `BlobStorage__ContainerName` if you chose another container. Save and restart the App Service **before deploying this version**; production startup requires the connection string. Never put it in `appsettings.json` or commit it to Git.
3. Deploy the new `app.zip`, then create a report with a photo and open the returned `photoUrl` on the **API's origin** (for example, `https://your-api.azurewebsites.net/uploads/abc.jpg`). The frontend must prefix the relative `photoUrl` with the API base URL.

New uploads go to the private container and are served through the API at `/uploads/{filename}`. Development without blob configuration still uses local `wwwroot/uploads`. Previously uploaded photos stored on the App Service filesystem are not automatically migrated to Blob Storage; they will work only while those files remain on that server. Storage transactions and API bandwidth may incur Azure charges.

## API errors

Application-generated errors use JSON problem details (`application/problem+json`) with `status`, `title`, and `detail`; validation errors include an `errors` object keyed by field. Invalid or missing Bearer tokens return `401`, insufficient permissions return `403`, missing resources return `404`, and duplicate accounts, pending claims, or already-decided claims return `409`. Unexpected server failures return `500` with a trace ID instead of exposing internal exception details. Successful response bodies are unchanged.

## Report dates

When creating a report, send `date` as an ISO 8601 timestamp with an offset (for example, `2026-09-30T14:30:00+00:00`) or as a date-only value (`2026-09-30`). Date-only values and timestamps without an offset are interpreted as UTC; offset timestamps are converted to UTC before storage.

## Browsing reports

`GET /api/reports` shows only `Open` and `Matched` reports by default, so claimed, resolved, and closed items do not appear on the public browse list. To request a particular status, use the `status` query parameter, for example `GET /api/reports?status=Claimed` or `GET /api/reports?kind=Found&status=Resolved`. Available statuses are `Open`, `Matched`, `Claimed`, `Resolved`, and `Closed`. The `GET /api/reports/mine` and item-detail endpoints still return reports regardless of status.

## Editing and deleting your reports

Send your Bearer token to edit or delete a report you posted:

- `PUT /api/reports/lost/{id}` or `PUT /api/reports/found/{id}` — send the same JSON fields (`title`, `description`, `category`, `location`, `date`) used to create a report. Returns the updated report; an existing photo is retained. Matches are refreshed after an edit.
- `DELETE /api/reports/lost/{id}` or `DELETE /api/reports/found/{id}` — permanently removes the report and its photo, returning `204 No Content`. Deleting a found report also removes claims against it; linked matches are removed for either kind.

Nonexistent reports return `404`; reports owned by another user return `403`. Only the report owner can edit or delete it.

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

The frontend can connect to `/hubs/notifications` with the same JWT to receive `NotificationReceived` events (with `id`, `type`, `referenceId`, `message`, optional `score`, `createdAt`, and `readAt`). Match alerts also emit the older `MatchFound` event for existing clients. Use `GET /api/notifications` with a Bearer token for newest-first notification history, `?unreadOnly=true` for unread alerts, and `PATCH /api/notifications/{id}/read` to mark an alert read. Notifications are private to the recipient and persist when they are offline.

Submitting a claim sends a `ClaimSubmitted` notification to admins and the found-item reporter. Approving or rejecting sends a `ClaimApproved` or `ClaimRejected` notification to the claimant. Approving also rejects any other pending claims on that item and notifies those claimants; new claims against a claimed, resolved, or closed item return `409`. Claim approval also notifies the found-item reporter.

## Stop the local database

```bash
docker compose down
```

To also delete all Docker PostgreSQL data:

```bash
docker compose down -v
```
