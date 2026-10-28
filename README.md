# Job Pipeline

A small personal tracker for job applications. Each entry has a company, title, link, applied date, status
(Wishlist → Applied → Screening → Interview → Offer / Rejected / Withdrawn / Closed), next step, salary and free notes.
You can list, filter by status, add, edit and delete entries.

Built with ASP.NET Core Razor Pages, EF Core and SQLite.

> [!TIP]
> This project is being developed with Claude Code as a way to learn ASP.NET Core for Node.js developers. [docs/architecture.md](docs/architecture.md) and the rest of the documentation map each ASP.NET feature to its Node counterpart, to help you build a mental model and learn faster.

## Screens
<img width="1720" height="984" alt="Image" src="https://github.com/user-attachments/assets/57c5be54-eaaf-4443-9ce8-bd1fbaa34f1f" />
<img width="1712" height="975" alt="Image" src="https://github.com/user-attachments/assets/84159475-8f3a-4468-86e3-a969cf31b335" />
<img width="1710" height="980" alt="Image" src="https://github.com/user-attachments/assets/34994b6f-6d0b-4b85-b3d4-520ade3bfcf9" />

## Requirements

- [.NET SDK 10](https://dotnet.microsoft.com/download) (macOS: `brew install --cask dotnet-sdk`)

SQLite needs no installation; it ships inside a NuGet package.

## Run it

```sh
git clone <repo-url> job-pipeline
cd job-pipeline

# optional: fill a fresh database with 12 demo applications
dotnet run -- seed

# start the app
dotnet watch
```

Open the URL printed as `Now listening on: http://localhost:...`.

The database is created automatically as `jobpipeline.db` in the project folder on first start (migrations are applied at startup).

## Demo data

`dotnet run -- seed` loads `data/initial-data.json` and exits. It only works on an empty database;
if `jobpipeline.db` already contains applications it stops with an error and changes nothing.
To start over with the demo data, stop the app, delete `jobpipeline.db` and run the seed command again.

If you use the app for real data, keep a copy of `jobpipeline.db` (it is git-ignored) before experimenting.

## Changing the model

Install the EF Core CLI once: `dotnet tool install -g dotnet-ef`. After editing `Models/JobApplication.cs`:

```sh
dotnet ef migrations add <Name>
```

The new migration is applied the next time the app starts.
