namespace BlazorPWA.Shared.Models;

using BlazorPWA.Shared.Enums;

public class InspectionReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityAddress { get; set; } = string.Empty;
    public string InspectorName { get; set; } = string.Empty;
    public string InspectorId { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public string Notes { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;
    public List<MediaAttachment> Attachments { get; set; } = new();
    public List<InspectionNote> InspectionNotes { get; set; } = new();
}
