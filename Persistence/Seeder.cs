using System.Text.Json;
using System.Text.Json.Serialization;
using JobPipeline.Models;

namespace JobPipeline.Persistence;

// Loads demo data from data/initial-data.json. Only runs on `dotnet run -- seed`, never on a normal start.
public static class Seeder
{
    record Row(string Company, string Title, string? Url, DateOnly? AppliedOn, ApplicationStatus Status,
               string? NextStep, DateOnly? NextStepDate, string? SalaryText, string? Notes);

    // Returns an error message, or null on success.
    public static string? Run(AppDbContext db, string path)
    {
        if (db.Applications.Any())
            return "Database already contains data - refusing to seed. Delete jobpipeline.db to start from scratch.";
        if (!File.Exists(path))
            return $"Seed file not found: {path}";

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        var rows = JsonSerializer.Deserialize<List<Row>>(File.ReadAllText(path), options)!;

        db.Applications.AddRange(rows.Select(r => new JobApplication
        {
            Company = r.Company, Title = r.Title, Url = r.Url, AppliedOn = r.AppliedOn, Status = r.Status,
            NextStep = r.NextStep, NextStepDate = r.NextStepDate, SalaryText = r.SalaryText, Notes = r.Notes,
        }));
        db.SaveChanges();
        return null;
    }
}
