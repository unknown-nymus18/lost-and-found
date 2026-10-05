# Campus Lost & Found API — agent context

This file is public API documentation and is also served verbatim at `GET /api/agents/context` (`text/markdown`). It describes how to use the API and how its behaviour works. It contains no secrets, credentials, or deployment details.

## Overview

- REST API over JSON, plus a SignalR hub for live notifications.
- Discovery: `GET /` returns the API name, version, and links; `GET /swagger` is the interactive OpenAPI UI; `GET /health` is a liveness check (it does not test the database).
- Students report lost or found items. The API automatically scores lost/found pairs and records likely **matches**. A student who believes a found item is theirs submits a **claim** with proof; an admin approves or rejects it. Every match and claim event creates a persisted **notification** that is also pushed live.
- Property names in JSON are camelCase. CORS allows any origin, method, and header.

## Conventions

- **Auth**: authenticated endpoints require `Authorization: Bearer <JWT>`. Tokens last 8 hours and there is no refresh endpoint; log in again when you get `401`.
- **Roles**: `Student` (every self-registered account) and `Admin`. Admin-only endpoints return `403` to students.
- **Enums are numbers in JSON** (request and response bodies). In query strings, `category` and `status` accept either the name or the number.

| Enum | Values |
| --- | --- |
| `ItemCategory` | `0` Electronics, `1` IdentityCard, `2` Wallet, `3` Keys, `4` Bag, `5` Book, `6` Clothing, `7` Jewellery, `8` Other |
| `ReportStatus` | `0` Open, `1` Matched, `2` Claimed, `3` Resolved, `4` Closed |
| `ClaimStatus` | `0` Pending, `1` Approved, `2` Rejected |
| `HandoverMethod` | `0` DropOff, `1` ContactFinder |

- **Dates**: all returned timestamps are UTC. Date-only or timezone-less inputs are interpreted as UTC; inputs with an explicit offset are converted to UTC.
- **Errors**: failures return RFC 7807 problem details (`application/problem+json`) with `status`, `title`, `detail`, and `traceId`. Validation failures (`400`) include an `errors` object keyed by field name.

## Authentication

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| POST | `/api/auth/register` | Public | Register with `{name,email,password}`; returns account details plus JWT. |
| POST | `/api/auth/login` | Public | Login with `{email,password}`; returns account details plus JWT. |
| GET | `/api/auth/me` | Bearer | Current user's `userId`, `name`, `email`, and `role` (no token). |

- Register and login both return `{userId, name, email, role, token}`.
- Registration always creates a `Student`. Name is at most 120 characters, email at most 160, and the password must be at least 6 characters. Emails are trimmed and lower-cased. A duplicate email returns `409`.
- Wrong email or password returns `401` with the same message either way.

## Reports and photos

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/api/reports` | Public | Browse reports (see filters below). |
| GET | `/api/reports/lost/{id}` and `/api/reports/found/{id}` | Public | One report by kind and ID, in any status. IDs are independent across lost and found. A found report includes `handover` only for the finder, admins, and the approved claimant (send the bearer token to get it). |
| GET | `/api/reports/mine` | Bearer | All of the current user's reports, in every status. |
| POST | `/api/reports/lost` and `/api/reports/found` | Bearer | Create a report from a JSON body `{title,description,category,location,date}`. |
| POST | `/api/reports/lost/with-photo` and `/api/reports/found/with-photo` | Bearer | Create via `multipart/form-data` with fields `Title`, `Description`, `Category`, `Location`, `Date`, and an optional `Photo` file. |
| PUT | `/api/reports/lost/{id}` and `/api/reports/found/{id}` | Owner bearer | Replace the editable fields with the same JSON body as create. The existing photo is kept. |
| DELETE | `/api/reports/lost/{id}` and `/api/reports/found/{id}` | Owner bearer | Permanently delete the report (`204`), its photo, and its matches. Deleting a found report also deletes its claims. |
| POST | `/api/reports/found/{id}/resolve` | Finder, approved claimant, or admin | Confirm a `Claimed` item was handed over; the report becomes `Resolved`. |
| GET | `/uploads/{filename}` | Public | Fetch a report photo. |

**Report DTO**: `{id, kind, title, description, category, location, date, photoUrl, status, reportedBy, createdAt, handover}`.

- `kind` is the string `"Lost"` or `"Found"`. `reportedBy` is the reporter's display name.
- `date` is when the item was lost or found. `createdAt` is when the report was filed.
- `photoUrl` is a relative `/uploads/...` path, or `null` when there is no photo. A frontend on another origin must prefix it with the API base URL.
- `handover` is `{method, dropOffLocation, finderName, contactPhone}` (see **Handover** below). It is always `null` for lost reports, in browsing, and in matches. It is filled in for the finder, for admins, and for the approved claimant.

**Create**:

- `title` ≤140 characters, `description` ≤2000, `location` ≤160. `date` is required.
- Responds `200` with the report DTO and an `X-Matches-Found` header holding the number of matches created immediately.
- Photo rules: at most 5 MB (the whole request is capped at 6 MB); `.jpg`, `.jpeg`, `.png`, `.webp`, or `.gif`. A bad photo returns `400` with an error on `Photo`.

**Handover** (found reports only; lost reports ignore these fields):

The finder says how the item gets back to its owner. The JSON body or form adds `handoverMethod`, `dropOffLocation`, and `contactPhone`.

| `handoverMethod` | Required field | What the approved claimant gets |
| --- | --- | --- |
| `DropOff` (default) | `dropOffLocation` (≤160), e.g. `Campus Security Office` | "Collect it from Campus Security Office." |
| `ContactFinder` | `contactPhone` (≤30, digits with optional `+`, spaces, `()`, `-`) | The finder's name and phone number. |

- A missing or invalid required field returns `400` with an error on that field.
- Only the field for the chosen method is stored. A phone number sent with `DropOff` is discarded.
- **Privacy**: the drop-off location and phone number are never public. Before approval, only the finder and admins can see them. After approval, the approved claimant can see them too. Rejected and competing claimants never see them.
- PUT on a found report replaces the handover details too, so send them again.
- Found reports created before this feature have `DropOff` with no location. Their approval message relies on the admin's note.

**Browse filters** (all optional, combined with AND):

- `kind`: exactly `Lost` or `Found` (case-sensitive). Omit it to get both. Any other value returns an empty list.
- `category`: an `ItemCategory` name or number.
- `status`: one `ReportStatus`. Without it, only `Open` and `Matched` reports are returned. `?status=Claimed` (for example) returns only that status.
- `location`: case-insensitive substring of the location.
- `query`: case-insensitive substring of the title or description.
- `from` / `to`: inclusive bounds on the report `date`, not on `createdAt`.
- Lost and found results are merged and sorted by `createdAt`, newest first. There is no pagination.

**Edit and delete**:

- Only the report's owner can edit or delete it, including for admins. Another user gets `403`, and a missing ID gets `404`.
- Editing deletes the report's existing matches. A `Matched` report goes back to `Open`, and an `Open` report is re-scanned for new matches. A counterpart left with no matches also returns from `Matched` to `Open`.

## Matching

When a report is created or edited, it is scored against every `Open` or `Matched` report of the opposite kind. Each pair gets a score from 0 to 100:

| Signal | Points |
| --- | --- |
| Same category | 40 |
| Location: identical / one contains the other / shared word | 30 / 22 / 15 |
| Date gap: ≤1 day / ≤3 / ≤7 / ≤14 | 20 / 15 / 10 / 5 |
| Shared keywords in title + description (4 each, words of 3+ letters, common words ignored) | up to 10 |

- A pair scoring **50 or more** becomes a match with a `score` and a human-readable `reason`, for example `same category (+40), close dates (+20)`.
- Both reports move from `Open` to `Matched`, and both owners are notified.
- A pair is only matched once.

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/api/matches/mine` | Bearer | Matches involving the user's reports. |
| GET | `/api/matches` | Admin | All matches. |

**Match DTO**: `{id, score, reason, lost, found, createdAt}`, where `lost` and `found` are full report DTOs.

## Claims

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| POST | `/api/claims` | Bearer | Submit `{foundReportId,proofDescription}`; creates a `Pending` claim. |
| GET | `/api/claims/mine` | Bearer | The user's own claims and their decisions, newest first. |
| GET | `/api/claims` | Admin | All claims for review, newest first. |
| POST | `/api/claims/{id}/decide` | Admin | Submit `{approve:true|false,note:string}`. `note` is required for both approval and rejection. |

**Claim DTO**: `{id, foundReportId, foundItemTitle, claimerName, proofDescription, status, reviewNote, createdAt, decidedAt, handover}`.

- `handover` holds the found item's handover details. It is always included for admins (`GET /api/claims` and the decide response).
- In `GET /api/claims/mine` it is included only on the user's `Approved` claims, and is `null` otherwise.

**Submitting a claim**:

- `proofDescription` should describe something only the owner would know. Blank proof returns `400`.
- An unknown found report returns `404`.
- A found report that is `Claimed`, `Resolved`, or `Closed` returns `409`.
- A second pending claim by the same user on the same item returns `409`.

**Deciding a claim**:

- A claim can only be decided once; deciding it again returns `409`.
- The admin's `note` is stored as the claim's `reviewNote`. It is required for both approval and rejection, and a missing or blank note returns `400`.
- **Approving**:
  - Sets the claim to `Approved` and the found report to `Claimed`, which removes the item from default browsing.
  - The claimant's message is built automatically: `Your claim on "<item>" was approved. <collection instructions> Admin note: <note>`. The collection instructions come from the finder's handover details. Use the note for extras, such as "bring your student ID".
  - The finder is told the claim was approved. With `ContactFinder`, the finder is also told the claimant has their phone number.
  - Every other pending claim on the item is rejected automatically, with the note `Another claim was approved for this item.`
- **Rejecting** sets only that claim to `Rejected`. The report status is unchanged.
- Both submitting and deciding can return `409` if the item changed at the same moment. Refresh and retry.

**Confirming handover** (`POST /api/reports/found/{id}/resolve`, no body):

- The finder, the approved claimant, or an admin confirms the item was handed over. The found report becomes `Resolved`, and the response is the report with `handover`.
- Anyone else gets `403`, and an unknown ID gets `404`.
- An item without an approved claim returns `409`, and so does an item that was already resolved.
- The finder and the approved claimant get an `ItemResolved` notification, except whoever confirmed it.

**Found item lifecycle**: `Open` → `Matched` (optional) → `Claimed` (admin approves a claim) → `Resolved` (handover confirmed). No endpoint sets `Closed` at the moment.

## Notifications

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/api/notifications` | Bearer | The user's notifications, newest first; `?unreadOnly=true` returns only unread ones. |
| PATCH | `/api/notifications/{id}/read` | Bearer owner | Mark a notification read and return it. Another user's ID returns `404`. Repeating the call keeps the original `readAt`. |
| PATCH | `/api/notifications/read-all` | Bearer | Mark all of the user's unread notifications read (`204`). Already-read ones keep their original `readAt`. |
| DELETE | `/api/notifications/{id}` | Bearer owner | Permanently delete one of the user's notifications (`204`). Another user's ID returns `404`. The match or claim it refers to is not affected. |
| SignalR | `/hubs/notifications` | Bearer | Live delivery of new notifications. |

**Notification DTO**: `{id, userId, type, referenceId, message, score, createdAt, readAt}`. `score` is set only for matches, and `readAt` is `null` until the notification is read.

| `type` | `referenceId` points to | Who receives it |
| --- | --- | --- |
| `Match` | match ID | Both report owners. |
| `ClaimSubmitted` | claim ID | All admins and the found item's reporter (never the claimant). |
| `ClaimApproved` | claim ID | The claimant, and the found item's reporter. |
| `ClaimRejected` | claim ID | The claimant. This includes competing claimants who are rejected automatically when another claim wins. |
| `ItemResolved` | found report ID | The finder and the approved claimant, except whoever confirmed the handover. |

Notifications are saved before they are pushed, so a user who was offline can load the history later. There is no email or browser push.

**Connecting to the hub**: browsers cannot set headers on WebSockets, so pass the JWT as a query parameter.

```js
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${API_BASE}/hubs/notifications`, { accessTokenFactory: () => token })
  .withAutomaticReconnect()
  .build();

connection.on("NotificationReceived", n => { /* Notification DTO */ });
connection.on("MatchFound", m => { /* { matchId, score, message } */ });
await connection.start();
```

- `accessTokenFactory` sends the token as `?access_token=<JWT>`.
- `NotificationReceived` fires for every notification type.
- `MatchFound` is a legacy event sent alongside `NotificationReceived` for matches. Listen to only one of them to avoid handling a match twice.
- A connection only receives its own user's events.

## Typical flow

1. `POST /api/auth/register` or `/api/auth/login`, then store `token`.
2. Student A: `POST /api/reports/lost`. Student B: `POST /api/reports/found/with-photo` with `HandoverMethod=DropOff` and `DropOffLocation=Campus Security Office`, or with `ContactFinder` and a phone number.
3. If the pair scores 50 or more, both students get a `Match` notification, and `GET /api/matches/mine` shows the pair.
4. Student A: `POST /api/claims` with the found report's ID and proof. The admins and Student B are notified.
5. Admin: `GET /api/claims`, then `POST /api/claims/{id}/decide` with a note. On approval:
   - Student A is told where to collect the item, or given Student B's phone number.
   - Student B is notified.
   - The found item becomes `Claimed`.
6. After the handover, Student A, Student B, or the admin calls `POST /api/reports/found/{id}/resolve`. The item becomes `Resolved`.
