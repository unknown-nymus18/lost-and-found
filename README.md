# UG Campus Lost & Found

A web app that helps University of Ghana students and staff report lost or found items, get matched automatically, and claim items securely.

Built with **Blazor WebAssembly (.NET 10)**.

## 🎨 UI/UX Design

The full UI/UX was designed in Figma.

| Resource | Link |
|---|---|
| Figma design file | [Open in Figma](https://www.figma.com/design/VbqyGGigDR4jWhv8GDRg06/Campus-Lost---Found---UI-UX) |
| Clickable prototype (Student flow) | [View prototype](https://www.figma.com/proto/VbqyGGigDR4jWhv8GDRg06/Campus-Lost---Found---UI-UX?node-id=2-2&starting-point-node-id=2%3A2) |
| Clickable prototype (Admin flow) | [View prototype](https://www.figma.com/proto/VbqyGGigDR4jWhv8GDRg06/Campus-Lost---Found---UI-UX?node-id=8-2&starting-point-node-id=8%3A2) |

**UI/UX Designer:** Kingsley Ofori-Atta

### Screens (19)

- **Student:** Home Dashboard, Report Item, Browse & Search, Item Detail & Claim, My Claims & Reports, Notifications, Profile
- **Auth:** Sign In, Register, Forgot Password
- **Confirmations:** Report Submitted, Claim Submitted
- **Admin:** Dashboard, All Reports, Matches, Claims Queue, Claim Review, Users, Settings

### Design System

| Token | Value |
|---|---|
| Primary | `#2563EB` |
| Text (headings) | `#111827` |
| Text (body) | `#374151` |
| Muted text | `#6B7280` |
| Background | `#F9FAFB` |
| Border | `#E5E7EB` |
| Success / Warning / Danger | `#16A34A` / `#D97706` / `#DC2626` |
| Font | Inter (Regular 400, Medium 500, Semi Bold 600, Bold 700) |
| Corner radius | 8px buttons & inputs, 12px cards |

## Getting Started

```bash
git clone https://github.com/unknown-nymus18/lost-and-found.git
cd lost-and-found
dotnet run
```

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

## Project Structure

```
Pages/        # Routable pages (Home, Browse, ...)
Components/   # Reusable UI components (ItemCard, ...)
Layout/       # MainLayout and NavMenu
wwwroot/      # Static assets, CSS, index.html
```
