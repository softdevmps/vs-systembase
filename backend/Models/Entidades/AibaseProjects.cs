using System;

namespace Backend.Models.Entidades;

public partial class AibaseProjects
{
    public int Id { get; set; }

    public string Slug { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Language { get; set; } = "es";

    public string? Tone { get; set; }

    public string Status { get; set; } = "draft";

    public bool IsActive { get; set; }

    public int TemplateId { get; set; }

    public int? TenantId { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual AibaseTemplates Template { get; set; } = null!;
}
