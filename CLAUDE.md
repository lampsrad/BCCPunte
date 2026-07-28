# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Don't approximate (mandatory)

When the user asks to match **Project Viewer** (or any other reference project/file):

1. **Open the real source first** — markup, CSS, and code-behind for that feature. Do not implement from memory or summary alone.
2. **Copy the interaction model and structure**, not a “BCC-shaped” reimplementation.
   - Example: Viewer Help_Merge is a **side flyout panel** on hover/focus of Help — **not** a separate Admin menu item, and **not** a nested `<ul>` submenu dropdown.
3. **Do not invent a close equivalent.** If the UI shell differs, still preserve trigger, placement, open/close behavior, and control hierarchy.
4. **Before finishing**, re-read the reference and verify: same parent control, same secondary control type (flyout vs submenu vs separate item), same user path (click vs hover).
5. If something cannot match 1:1, **say so and ask** — never silently approximate.

Same rule applies to “same as Viewer Help”, “same styling as X”, “do it like the other project”, etc.

## Project Overview

**BCCPunte** is an ASP.NET Core Blazor Server-side application for the Bloemfontein Camera Club (BCC), managing photographic salon competitions, member data, scores, and rankings.

## Commands

```bash
# Build
dotnet build

# Run (development — starts on http://localhost:5125)
dotnet run --project BCC/BCC.csproj

# Publish (release)
dotnet publish BCC/BCC.csproj -c Release

# EF Core migrations (from BCC/ directory)
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

There are no automated tests in this project.

## Architecture

### Startup & DI (`BCC/Program.cs`)

Services registered:
- `IDbContextFactory` → `DbContextFactory` — creates EF Core contexts on demand
- `State` (singleton) — global UI/event state bus
- `Repo` (scoped) — all database access goes through here
- `DataService` (scoped) — monthly data business logic
- `SalonImport` (scoped) — salon competition import logic

Machine name is detected at startup to select the correct connection string (`XPS`, `ROG`, or `Abshost`). The app auto-launches Chrome in production (controlled in `gData.cs`).

### Data Layer

**`DbContextFactory.cs`** — wraps `IDbContextFactory<BKKEntities>` to select the right SQL Server connection string per environment.

**`Models/BKKEntities.cs`** — EF Core `DbContext` with these main entity sets:
- `Masters` — club members
- `Monthlies` — monthly competition entries
- `Photos` — uploaded images
- `Ratings` — competition scores
- `Salons` / `SalonMasters` — salon events and member participation
- `Datums` — system metadata (e.g. last import date)
- `HitCounters`, `Clubs`

**`Repo.cs`** — the single data-access class. All pages and services inject `Repo` and call its async methods (Add, Delete, Update, query methods). Direct EF queries are not done outside `Repo`.

### Global State (`State.cs` and `gData.cs`)

`State` is a singleton injected into Blazor components. It exposes `Action` events (`OnProgress`, `OnMenuUpdate`, `OnMessage`, etc.) that components subscribe to for reactive UI updates — the standard pattern for cross-component communication in this app.

`gData` is a static class with app-wide constants and paths:
- API base: `https://bkk.co.za/`
- File system paths for photos, backups, imports, Word templates
- FTP credentials (read from `appsettings.json`)
- Club-specific settings (name, database name)

### Services (`BCC/Services/`)

| Service | Responsibility |
|---|---|
| `DataService` | Monthly record logic, member retrieval, import directory cleanup |
| `SalonImport` | Parses and imports salon competition result files |
| `ClubImport` | Imports club-level data from external files |
| `SendEmail` | Email dispatch |

### Presentation Layer (`BCC/Pages/` and `BCC/Adm/`)

Standard Blazor Server components (`.razor` files). Pages inject `Repo`, `State`, and services directly — there is no intermediate ViewModel/MVVM layer; data binding is done inline in the component.

Admin pages live under `Adm/Club/` (club admin, backup, honours) and `Adm/Salonne/` (salon CRUD and member assignments).

`Viewmodels/` contains lightweight DTOs (`ResultsVM`, `ScoresVM`, `PhotoVM`, etc.) used for projecting query results into page-specific shapes.

### Utilities (`BCC/Extensions.cs`)

Extension methods used throughout the codebase:
- `.toCap()` / `.toFileName()` — string formatting
- `.toDate()` / `.toDateFirstDay()` / `.toClubDate()` — date parsing; club fiscal year starts **1 November**
- `.toStarGroup()` — maps member rating to category
- `.toAward()` — maps award codes (`C`→Com, `G`→Gold, `S`→Silver, `B`→Bronz)

### Configuration (`BCC/appsettings.json`)

Contains connection strings for three environments (`XPS`, `ROG`, `Abshost`), FTP credentials, auth credentials, and Kestrel endpoint. User Secrets are not configured — credentials live in `appsettings.json`.

### Static Assets (`BCC/wwwroot/`)

| Folder | Content |
|---|---|
| `ClubPhotos/` | Member and competition photos |
| `Import/` | Staging area for incoming import files |
| `Backup/` | Database backup files |
| `html/` | HTML templates |

`FileChoose.razor` is explicitly excluded from the build in `BCC.csproj`.
