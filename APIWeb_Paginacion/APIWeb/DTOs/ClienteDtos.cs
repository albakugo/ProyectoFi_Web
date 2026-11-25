using System.ComponentModel.DataAnnotations;

namespace APIWeb.DTOs
{
    // DTO fijo de salida (solo lleva lo estrictamente necesario y con datos sensibles cubiertos)
    public class ClienteConsultaDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Country { get; set; }
        public string EmailEnmascarado { get; set; } = string.Empty;    // Por ejemplo, ab***@gmail.com
        public string TelefonoEnmascarado { get; set; } = string.Empty; // Por ejemplo, 445***01
        public bool Activo { get; set; }
    }

    // DTO de entrada para busqueda (sin SELECT * de todos los clientes)
    public class BuscarClienteDto
    {
        [Required(ErrorMessage = "Debes proporcionar un ID o término de búsqueda.")]
        [MinLength(2, ErrorMessage = "Ingresa al menos 2 caracteres para buscar.")]
        [MaxLength(40, ErrorMessage = "La búsqueda es demasiado larga.")]
        public string Criterio { get; set; } = string.Empty;
    }

    // DTO de entrada para actualizar datos autorizados de cliente sin exponer campos de mas
    public class ActualizarClienteDto
    {
        [Required(ErrorMessage = "El nombre de la empresa o cliente es obligatorio.")]
        [MaxLength(40)]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de contacto es obligatorio.")]
        [MaxLength(30)]
        public string ContactName { get; set; } = string.Empty;

        [MaxLength(15)]
        public string? City { get; set; }

        [MaxLength(15)]
        public string? Country { get; set; }

        [MaxLength(24)]
        public string? Phone { get; set; }
    }
}