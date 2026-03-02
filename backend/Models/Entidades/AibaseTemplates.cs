using System;
using System.Collections.Generic;

namespace Backend.Models.Entidades;

public partial class AibaseTemplates
{
    public int Id { get; set; }

    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string PipelineJson { get; set; } = "{}";

    public bool IsActive { get; set; }

    public string Version { get; set; } = "1.0";

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AibaseProjects> Projects { get; set; } = new List<AibaseProjects>();
}
