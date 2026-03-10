using SQLite;

namespace BlazorHybrid.App.Data.Models;

[Table("Entities")]
public class EntityRecord
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Indexed]
    public string CaseWorkerId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;

    /// <summary>JSON-serialized dictionary of form field values.</summary>
    public string FormFieldsJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsDeleted { get; set; }
    public bool IsSynced { get; set; }
}
