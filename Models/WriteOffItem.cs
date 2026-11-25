namespace WMS.Models;

public class WriteOffItem
{
    public int Id { get; set; }
    public int WriteOffId { get; set; }
    public int ProductId { get; set; }

    // Из какой ячейки списан товар
    public int StorageLocationId { get; set; }
    public int Quantity { get; set; }

    public WriteOff? WriteOff { get; set; }
    public Product? Product { get; set; }
    public StorageLocation? StorageLocation { get; set; }
}