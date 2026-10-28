using JobPipeline.Persistence;
using JobPipeline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JobPipeline.Pages;

public class EditModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public JobApplication Application { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null) return Page();

        var existing = await db.Applications.FindAsync(id);
        if (existing is null) return NotFound();
        Application = existing;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Application.CreatedAt");
        ModelState.Remove("Application.UpdatedAt");
        if (!ModelState.IsValid) return Page();

        if (Application.Id == 0)
        {
            db.Applications.Add(Application);
        }
        else
        {
            var existing = await db.Applications.FindAsync(Application.Id);
            if (existing is null) return NotFound();

            // Copy only the editable fields so CreatedAt is preserved and nothing else can be overposted.
            existing.Company = Application.Company;
            existing.Title = Application.Title;
            existing.Url = Application.Url;
            existing.AppliedOn = Application.AppliedOn;
            existing.Status = Application.Status;
            existing.NextStep = Application.NextStep;
            existing.NextStepDate = Application.NextStepDate;
            existing.SalaryText = Application.SalaryText;
            existing.Notes = Application.Notes;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
