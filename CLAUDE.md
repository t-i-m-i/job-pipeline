# CLAUDE.md

Personal job-application tracker: ASP.NET Core (.NET 10) Razor Pages, EF Core, SQLite. Server-rendered, no SPA, no Node tooling.
It is also a learning project for ASP.NET Core aimed at Node/NestJS developers, so docs and comments should map .NET concepts to Node equivalents where useful.
See `README.md` for usage and `docs/architecture.md` for the code tour.

## Commands

- `dotnet run` - start the app (URL printed as "Now listening on"). Needs a restart after code/template changes; use `dotnet watch` for live reload.
- `dotnet run -- seed` - load `data/initial-data.json` into an empty database and exit. Refuses (exit code 1) if the database already has rows.
- `dotnet build` - compile only.
- `dotnet ef migrations add <Name>` - generate a migration after changing `Models/`. Migrations are applied automatically at startup.
- No test project exists yet.

## Layout

- `Program.cs` - DI registration, migration on startup, `seed` CLI hook, middleware.
- `Models/JobApplication.cs` - the only entity, plus `ApplicationStatus` enum (stored as text) and validation attributes.
- `Persistence/` - `AppDbContext` and `Seeder`. Namespace `JobPipeline.Persistence`.
- `Pages/Index.cshtml(.cs)` - list, status filter, delete handler, status legend modal. `Pages/Edit.cshtml(.cs)` - add/edit form (one page for both).
- `data/initial-data.json` - fictional demo data only. Never put real applications here; this repo is public.
- `wwwroot/lib/` - vendored Bootstrap/jQuery from the template; don't edit.

## Conventions and gotchas

- The SQLite file `jobpipeline.db` is git-ignored and holds the user's real data. Never delete or overwrite it; for tests point at another file with `ConnectionStrings__Default="Data Source=/some/path.db"`.
- `appsettings.Development.json` overrides `appsettings.json` in dev, so config changes usually need to go in both.
- `Edit` copies editable fields onto the loaded entity instead of saving the bound object (over-posting protection); keep that pattern when adding fields.
- Keep it simple: plain Razor Pages and Bootstrap, no extra front-end framework.
- macOS file system is case-insensitive: don't create folders that differ only by case (`data/` is for JSON, `Persistence/` for code).
