using System.ComponentModel.DataAnnotations;

namespace APIWeb.DTOs
{
    // DTO de entrada: unicamente el Admin puede dar de alta un Empleado o Administrador
    public class RegistrarEmpleadoDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [MinLength(4, ErrorMessage = "El usuario debe tener al menos 4 caracteres.")]
        [MaxLength(40)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [MaxLength(60)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio.")]
        [RegularExpression("^(Admin|Empleado)$", ErrorMessage = "El rol solo puede ser Admin o Empleado.")]
        public string Rol { get; set; } = "Empleado";

        [Required(ErrorMessage = "El nombre del empleado es obligatorio.")]
        [MaxLength(10)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido del empleado es obligatorio.")]
        [MaxLength(20)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Title { get; set; }

        [MaxLength(15)]
        public string? City { get; set; }

        [MaxLength(24)]
        public string? HomePhone { get; set; }
    }

    // DTO fijo de salida: muestra la informacion del empleado sin datos sensibles y/o privados
    public class EmpleadoConsultaDto
    {
        public int EmployeeId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? City { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string EmailEnmascarado { get; set; } = string.Empty;
        public string TelefonoEnmascarado { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}