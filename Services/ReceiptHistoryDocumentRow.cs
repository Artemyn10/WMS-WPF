namespace WMS.Services;

public class ReceiptHistoryDocumentRow
{
    public int WarehouseId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public DateOnly ArrivalDate { get; set; }
    public DateTime DateLocal { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string SourceDisplay { get; set; } = string.Empty;
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public int ItemsCount { get; set; }
    public int TotalQuantity { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public bool IsInterWarehouseTransfer { get; set; }
    public List<ReceiptHistoryItemRow> Items { get; set; } = new();
}

public class ReceiptHistoryItemRow
{
    public string ProductName { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int PlacedQuantity { get; set; }
    public int Remaining { get; set; }
    public string DestinationLocationCode { get; set; } = string.Empty;
}