using System.ComponentModel.DataAnnotations;

namespace JobPipeline.Models;

public enum ApplicationStatus
{
    Wishlist,
    Applied,
    Screening,
    Interview,
    Offer,
    Rejected,
    Withdrawn,
    Closed,
}

public class JobApplication
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Company { get; set; } = "";

    [Required, StringLength(300)]
    public string Title { get; set; } = "";

    [Url, StringLength(2000)]
    public string? Url { get; set; }

    [DataType(DataType.Date), Display(Name = "Applied on")]
    public DateOnly? AppliedOn { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

    [StringLength(300), Display(Name = "Next step")]
    public string? NextStep { get; set; }

    [DataType(DataType.Date), Display(Name = "Next step date")]
    public DateOnly? NextStepDate { get; set; }

    [StringLength(200), Display(Name = "Salary")]
    public string? SalaryText { get; set; }

    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
