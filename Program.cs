using JobPipeline.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=jobpipeline.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // `dotnet run -- seed` fills an empty database with demo data and exits.
    if (args.Contains("seed"))
    {
        var error = Seeder.Run(db, Path.Combine(app.Environment.ContentRootPath, "data", "initial-data.json"));
        Console.WriteLine(error ?? "Seeded demo data.");
        Environment.ExitCode = error is null ? 0 : 1;
        return;
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
