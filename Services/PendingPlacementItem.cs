namespace WMS.Services;

// Строка приёмки, по которой ещё есть неразмещённый товар
public class PendingPlacementItem
{
    public int ReceiptItemId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int PlacedQuantity { get; set; }

    public int Remaining => Quantity - PlacedQuantity;
}