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

    /// <summary>Role held by the user at the time of the action (e.g. "Admin", "Doctor").</summary>
    [MaxLength(100)]
    public string? UserRole { get; set; }

    /// <summary>Client IP address (IPv4 or IPv6, max 45 chars). Populated from X-Forwarded-For or RemoteIpAddress.</summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>"Success" or "Failure" — outcome of the audited operation.</summary>
    [MaxLength(20)]
    public string? Outcome { get; set; }

    /// <summary>HTTP verb of the originating request (GET, POST, PUT, DELETE, PATCH).</summary>
    [MaxLength(10)]
    public string? HttpMethod { get; set; }

    /// <summary>Request path of the originating HTTP call.</summary>
    [MaxLength(500)]
    public string? RequestPath { get; set; }

    public string? ChangesJson { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }
}
