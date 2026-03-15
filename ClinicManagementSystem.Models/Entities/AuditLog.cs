using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models.Entities;

public class AuditLog : BaseEntity
{
    [Required, MaxLength(200)]
    public string EntityName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ActionType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public Guid? PerformedByUserId { get; set; }

    public string? ChangesJson { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}
