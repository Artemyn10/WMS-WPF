namespace WMS.Services;

public class StockMovementRow
{
    public DateTime Date { get; set; }
    public string OperationTypeDisplay { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string FromLocationCode { get; set; } = string.Empty;
    public string ToLocationCode { get; set; } = string.Empty;
}