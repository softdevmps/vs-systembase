using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Incidentes
{
    public class IncidentesAudioUploadRequest
    {
        [Required]
        public IFormFile Audio { get; set; } = null!;
        public string? Descripcion { get; set; }
    }
}
