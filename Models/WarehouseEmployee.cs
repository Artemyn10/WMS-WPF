namespace WMS.Models;

public class WarehouseEmployee
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public required string FullName { get; set; }
    public required string Position { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    // true - ответственное лицо склада
    public bool IsResponsible { get; set; }

    public Warehouse? Warehouse { get; set; }
}