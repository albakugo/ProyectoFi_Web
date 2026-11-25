using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIWeb.Models
{
    [Table("usuarios")]
    public class Usuario
    {
        [Key]
        public int UsuarioId { get; set; }

        [Required]
        [MaxLength(50)]
        public string NombreUsuario { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Email { get; set; }

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Nombre { get; set; }

        [MaxLength(20)]
        public string Rol { get; set; } = "Cliente";

        [MaxLength(5)]
        public string? CustomerID { get; set; }

        public int? EmployeeID { get; set; }

        public int IntentosFallidos { get; set; }

        public DateTime? BloqueadoHasta { get; set; }

        public bool Activo { get; set; } = true;
    }
}