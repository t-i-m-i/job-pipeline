using JobPipeline.Models;
using Microsoft.EntityFrameworkCore;

namespace JobPipeline.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<JobApplication> Applications => Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Store the enum as readable text rather than a number.
        b.Entity<JobApplication>().Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
    }
}
