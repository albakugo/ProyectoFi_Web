using System.ComponentModel.DataAnnotations;

namespace APIWeb.DTOs
{
    public class RegistroClienteRequest
    {
        [Required]
        [MaxLength(50)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string Empresa { get; set; } = string.Empty;

        [MaxLength(24)]
        public string? Telefono { get; set; }
    }
}
