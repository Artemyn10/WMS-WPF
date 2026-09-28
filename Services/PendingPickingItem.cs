namespace WMS.Services;

// Строка заказа, по которой ещё есть несобранный товар
public class PendingPickingItem
{
    public int OrderItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int PickedQuantity { get; set; }

    public int Remaining => Quantity - PickedQuantity;
}