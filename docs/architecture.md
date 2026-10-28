# App and stack overview

This document describes how Job Pipeline is built. It is written for someone who knows Node, React and
NestJS but has not seen ASP.NET Core before, so most concepts are mapped to their NestJS or React equivalents.

## The big picture

This is **not** an API plus a React SPA. It is a classic server-rendered app: the server builds HTML,
the browser shows it, and forms are plain HTML `<form method="post">` submissions followed by a redirect.
There is no client-side JavaScript framework, no bundler and no `package.json`.
The only JavaScript is Bootstrap's bundle and jQuery validation, both shipped as static files.

```
Browser ── GET /  ───────────────► Razor Page "Index"  ──► EF Core ──► SQLite file
        ◄── HTML (rendered) ───────        │
Browser ── POST /?handler=Delete ─►        └─ redirect back to GET /
```

Think of it as "Express + a template engine (Pug/EJS) + an ORM (TypeORM/Prisma)", with everything
from one framework.

## Stack at a glance

| Concern | Here | Closest Node equivalent |
|---|---|---|
| Language / runtime | C# on .NET 10 | TypeScript on Node |
| Web framework | ASP.NET Core (Kestrel server) | Express/Fastify under NestJS |
| UI layer | Razor Pages (`.cshtml` + C# class) | Next.js pages with server rendering, but with templates instead of JSX |
| ORM | Entity Framework Core | TypeORM / Prisma |
| Database | SQLite (single file) | better-sqlite3 |
| Migrations | `dotnet ef migrations` | TypeORM / Prisma migrate |
| Validation | Data annotations (attributes) | class-validator decorators |
| Dependencies | NuGet, listed in `.csproj` | npm, `package.json` |
| CSS | Bootstrap (static files in `wwwroot/lib`) | same |
| Dev server | `dotnet run` | `nest start` |

## Project layout

```
JobPipeline.csproj        # like package.json: target framework + NuGet packages
Program.cs                # the entry point and composition root (like main.ts + AppModule)
appsettings*.json         # configuration (like .env + @nestjs/config); Development file overrides the base one
Properties/launchSettings.json   # dev-only: which port `dotnet run` uses
Models/JobApplication.cs  # the entity and its validation rules
Persistence/AppDbContext.cs  # EF Core "database session": table sets + mapping config
Persistence/Seeder.cs        # demo data loader, run with `dotnet run -- seed`
Migrations/               # generated schema migrations (commit these)
Pages/                    # Razor Pages: URL routing is by file path
  Index.cshtml(.cs)       #   GET /   the list, plus the delete action
  Edit.cshtml(.cs)        #   GET/POST /Edit/{id?}   add and edit form
  Shared/_Layout.cshtml   #   the HTML shell wrapped around every page
  _ViewImports.cshtml     #   shared imports and tag-helper registration
wwwroot/                  # static files served as-is (like Express's static folder)
data/initial-data.json    # the demo data
```

Build output goes to `bin/` and `obj/` (git-ignored, like `dist/` and `node_modules`).

## Program.cs: the composition root

`Program.cs` is where the app is assembled. It has two phases, which you can compare to NestJS's
module setup and `app.use(...)`:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();                       // register services in the DI container
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(...));

var app = builder.Build();                              // container is frozen here

app.UseRouting();                                       // middleware pipeline, in order, like Express app.use()
app.MapRazorPages();                                    // map routes
app.Run();
```

- **Dependency injection** is built in. `builder.Services.Add...` is like putting providers in a
  NestJS module. Lifetimes exist too: `AddDbContext` registers it as *scoped*, meaning one instance per HTTP request
  (like `Scope.REQUEST`, but it is the default and cheap here).
- **Middleware** works like Express: an ordered pipeline, each piece calling the next.
  `UseExceptionHandler`, `UseRouting` and `UseAuthorization` are middleware. Order matters.
- The `args` check for `seed` is a tiny CLI hook: `dotnet run -- seed` passes `seed` to the program,
  which seeds and exits before the web server starts. Everything after `--` goes to your app instead of to `dotnet`.
- `db.Database.Migrate()` runs pending migrations on startup. That is fine for a single-user SQLite app;
  larger systems usually run migrations as a separate deploy step.

## Razor Pages vs NestJS controllers

This is the biggest mental shift. NestJS separates controller classes from routes declared by decorators.
Razor Pages makes the **page** the unit, and the route comes from the file location.

| NestJS | Razor Pages |
|---|---|
| `@Controller('applications')` + `@Get()` | `Pages/Applications/Index.cshtml` answers `GET /Applications` |
| `@Get(':id')` | `@page "{id:int?}"` on the first line of the `.cshtml` (route template, `int` constraint, `?` optional) |
| Method decorated `@Get()` | Method `OnGet` / `OnGetAsync` |
| Method decorated `@Post()` | Method `OnPost` / `OnPostAsync` |
| Several actions on one controller | **Named handlers**: `OnPostDeleteAsync` is hit with `?handler=Delete` |
| `@Param('id') id` | Method parameter `int id` (bound by name from route/query/form) |
| `@Body() dto` | A property marked `[BindProperty]` |
| `@Query()` | `[BindProperty(SupportsGet = true)]` on a property, or a method parameter |
| `res.redirect()` | `return RedirectToPage("Index");` |
| `throw new NotFoundException()` | `return NotFound();` |
| `@Render('view') + return data` | The page model's public properties are the view's data |

Each page is two files with a pair of classes:

- `Index.cshtml.cs` is the **PageModel**: a C# class holding state (properties) and handlers. It's the "controller + view model" in one.
- `Index.cshtml` is the **template**. `@model IndexModel` at the top tells it which class backs it,
  and `Model.Applications` reads that class's properties. Think of the `.cs` file as the component's logic
  and the `.cshtml` as its JSX, except it renders on the server only and has no client state.

Example from `Pages/Index.cshtml.cs`:

```csharp
public class IndexModel(AppDbContext db) : PageModel       // `db` is injected via the constructor
{
    public List<JobApplication> Applications { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public ApplicationStatus? Status { get; set; }         // bound from ?status=Interview

    public async Task OnGetAsync() { ... }                 // GET /
    public async Task<IActionResult> OnPostDeleteAsync(int id) { ... }   // POST /?handler=Delete&id=3
}
```

Constructor injection works like in NestJS, except no `@Injectable()` or `@Inject()` decorators are needed.
Anything registered in `builder.Services` is resolved by type from the constructor parameters.
Here `(AppDbContext db)` on the class is a *primary constructor*, the C# shorthand for the field-plus-constructor boilerplate.

### Razor template syntax

In a `.cshtml` file, HTML is the default and `@` switches into C#:

```cshtml
@foreach (var a in Model.Applications)
{
    <tr><td>@a.Company</td></tr>      @* @expression is HTML-escaped automatically *@
}
```

It is like JSX with `{...}` replaced by `@...`. Output is escaped by default, which prevents XSS, the same as React.

**Tag helpers** are the attributes starting with `asp-`. They are how markup gets wired up to the C# side without hand-writing URLs and names:

```cshtml
<a asp-page="Edit" asp-route-id="@a.Id">Edit</a>        renders: <a href="/Edit/7">Edit</a>
<input asp-for="Application.Company" class="form-control" />
   renders: <input name="Application.Company" id="Application_Company" value="Acme" data-val-required="..." />
<span asp-validation-for="Application.Company"></span>   the validation message slot
```

`asp-for` is the key one: it generates the right `name`, `id`, current value and validation attributes from the model property,
so the form posts back in a shape the model binder understands.

## Model binding and validation

When a form posts, ASP.NET Core maps form fields onto the `[BindProperty]` property automatically
(the "model binder"). That is the equivalent of NestJS's `@Body()` plus `ValidationPipe`, with `class-transformer` built in.

Validation rules are attributes on the model, exactly like `class-validator` decorators:

```csharp
[Required, StringLength(200)]
public string Company { get; set; } = "";

[Url, StringLength(2000)]
public string? Url { get; set; }
```

In the handler you check `ModelState.IsValid`; if it fails you return `Page()` to re-render the form with the errors.
The same attributes also generate `data-val-*` HTML attributes, and the jQuery validation scripts
(`_ValidationScriptsPartial`) use them for instant client-side checks, so the rules are written once.

`string?` vs `string` is C#'s nullable reference types: `string?` means "may be null" and `string` means "never null".
It is compile-time only, like TypeScript's `| null`, but the compiler warns instead of silently allowing it.
Since this is the opposite default from TypeScript's loose objects, expect to write `?` a lot.

**Over-posting.** Binding a whole entity from a form lets a malicious user set fields you didn't render (e.g. `CreatedAt`).
`Edit.cshtml.cs` therefore loads the existing row and copies only the editable fields onto it instead of saving the bound object directly.
In larger apps you'd use a separate input DTO/view model, as you would in NestJS.

## Data layer: Entity Framework Core

EF Core is the ORM. The pieces:

- **Entity**: `Models/JobApplication.cs`, a plain class with properties. No decorators required for columns;
  `Id` is recognised as the primary key by convention.
- **DbContext**: `Persistence/AppDbContext.cs`. It represents a database session and exposes tables as `DbSet<T>` properties.
  It is like a TypeORM `DataSource` + repositories, or the Prisma client. `db.Applications` is the "table".
- **LINQ queries**: C# collection operations that EF translates to SQL:

```csharp
var query = db.Applications.AsQueryable();
if (Status is { } s) query = query.Where(a => a.Status == s);   // builds SQL WHERE
var list = await query.ToListAsync();                           // executes here
```

  Compare with Prisma's `findMany({ where: { status } })`. The query is lazy and runs at `ToListAsync()`.
- **Change tracking**: EF remembers the entities it loaded. Changing a property and calling `SaveChangesAsync()` writes an UPDATE
  with no explicit `update()` call. `db.Applications.Add(x)` / `.Remove(x)` queue an INSERT / DELETE until `SaveChangesAsync()`.
  The rule is that nothing hits the database until you save.
- **`async`/`await`** works like in JS: `Task<T>` is `Promise<T>`. By convention async methods end in `Async`.
- **Enum mapping**: `OnModelCreating` stores `ApplicationStatus` as readable text instead of an integer, so the database stays human-friendly.
- `DateOnly` is a date without a time, which suits "applied on" better than a full timestamp.

### Migrations

Migrations are generated from the difference between your C# model and the last snapshot, like `prisma migrate dev`
or `typeorm migration:generate`:

```sh
dotnet ef migrations add AddSomething     # writes files into Migrations/
```

The app applies them at startup with `Database.Migrate()`. Commit the `Migrations/` folder.
`dotnet-ef` is a global CLI tool (installed once with `dotnet tool install -g dotnet-ef`), unlike NuGet packages that live in the project.

## Configuration

`appsettings.json` holds defaults and `appsettings.Development.json` overrides them when `ASPNETCORE_ENVIRONMENT=Development`
(which `dotnet run` sets via `launchSettings.json`). It is similar to `.env` plus `.env.development`, but structured JSON.
Environment variables override both, with `__` as the nested separator, so `ConnectionStrings__Default=...` overrides
`ConnectionStrings:Default`. The app reads the connection string with `Configuration.GetConnectionString("Default")`
and falls back to `Data Source=jobpipeline.db`.

Here the config is also used to quieten EF Core's SQL logging (`"Microsoft.EntityFrameworkCore": "Warning"`).

## Static files and styling

Anything in `wwwroot/` is served from the site root. The template ships Bootstrap and jQuery under `wwwroot/lib`.
`MapStaticAssets()` serves them (with caching and fingerprinting) and `asp-append-version="true"` adds a hash for cache busting.
Our own CSS is `wwwroot/css/site.css`.

## Request walk-through: deleting an entry

1. In `Index.cshtml`, each row has a `<form method="post" asp-page-handler="Delete" asp-route-id="7">`.
2. The browser POSTs to `/?handler=Delete&id=7`. The form also carries an anti-forgery token, added automatically by the form tag helper.
3. Razor Pages validates the token (CSRF protection, with nothing for you to write), finds `OnPostDeleteAsync(int id)` and binds `id`.
4. The handler loads the row, calls `db.Applications.Remove(app)` and `SaveChangesAsync()`.
5. It returns `RedirectToPage()`, so the browser GETs `/` again (the Post/Redirect/Get pattern) and sees the updated list.
   Hitting refresh does not re-submit the delete.

## Running and developing

| Command | What it does |
|---|---|
| `dotnet run` | Build and start the app (prints the URL) |
| `dotnet watch` | Same, but rebuilds and reloads on file changes |
| `dotnet run -- seed` | Load demo data into an empty database, then exit |
| `dotnet build` | Compile only |
| `dotnet ef migrations add Name` | Generate a migration after changing a model |

Razor templates are recompiled on change under `dotnet watch`. Plain `dotnet run` needs a restart after any code or template change.

## Where to go next

If you want to extend the app, these are natural next steps and their NestJS analogues:

- Move the data access out of the pages into an injected service class (a NestJS provider).
- Add a JSON API with **minimal APIs** (`app.MapGet("/api/applications", ...)`, similar to Express handlers) or **controllers**
  (`[ApiController]`, `[HttpGet]`, `[FromBody]`, the closest thing to NestJS controllers).
  Controllers with attributes are where ASP.NET Core is most similar to NestJS.
- Add sorting and search via more LINQ `Where` and `OrderBy` clauses.
