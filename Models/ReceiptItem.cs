namespace WMS.Models;

public class ReceiptItem
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    // Сколько единиц из этой строки уже разложено по ячейкам
    public int PlacedQuantity { get; set; }

    public Receipt? Receipt { get; set; }
    public Product? Product { get; set; }
}