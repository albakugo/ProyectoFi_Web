using System;

namespace APIWeb.Models;

public partial class Usuario
{
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Rol { get; set; } = "Cliente";
    public string? CustomerId { get; set; }
    public int? EmployeeId { get; set; }
    public int IntentosFallidos { get; set; } = 0;
    public DateTime? BloqueadoHasta { get; set; }
    public bool Activo { get; set; } = true;
}