using JobPipeline.Persistence;
using JobPipeline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace JobPipeline.Pages;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<JobApplication> Applications { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public ApplicationStatus? Status { get; set; }

    public async Task OnGetAsync()
    {
        var query = db.Applications.AsQueryable();
        if (Status is { } s) query = query.Where(a => a.Status == s);

        // SQLite can't order by DateOnly? reliably in all cases, so sort in memory (small dataset).
        Applications = (await query.ToListAsync())
            .OrderByDescending(a => a.AppliedOn ?? DateOnly.MinValue)
            .ThenByDescending(a => a.Id)
            .ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var app = await db.Applications.FindAsync(id);
        if (app is not null)
        {
            db.Applications.Remove(app);
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
