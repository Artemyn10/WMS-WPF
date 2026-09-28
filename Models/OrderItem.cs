namespace WMS.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    // Сколько единиц из этой строки уже собрано (списано с ячеек)
    public int PickedQuantity { get; set; }

    public Order? Order { get; set; }
    public Product? Product { get; set; }
}