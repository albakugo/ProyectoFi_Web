using System.ComponentModel.DataAnnotations;

namespace APIWeb.DTOs;

public class AgregarCarritoItemDto
{
    [Required(ErrorMessage = "El ProductId es obligatorio.")]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Quantity { get; set; }
}