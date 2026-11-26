namespace APIWeb.DTOs
{
    public class ProductoResponseDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal? UnitPrice { get; set; }
        public string? QuantityPerUnit { get; set; }
        public int? UnitsInStock { get; set; }
        public bool Discontinued { get; set; }
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
        public string CategoryName { get; set; } = "Sin categoría";
        public string SupplierName { get; set; } = "Sin proveedor";
    }
}
