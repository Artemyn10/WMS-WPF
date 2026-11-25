namespace WMS.Models;

public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int? FromLocationId { get; set; }
    public int? ToLocationId { get; set; }
    public int Quantity { get; set; }
    public OperationType OperationType { get; set; }
    public DateTime Date { get; set; }

    // Склад, на котором выполнена операция
    public int? WarehouseId { get; set; }

    public Product? Product { get; set; }
    public StorageLocation? FromLocation { get; set; }
    public StorageLocation? ToLocation { get; set; }
    public Warehouse? Warehouse { get; set; }
}