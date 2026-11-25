namespace WMS.ViewModels;

public sealed class WarehouseTransferLineViewModel
{
    public int ProductId { get; init; }
    public int SourceLocationId { get; init; }
    public int DestinationLocationId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Article { get; init; } = string.Empty;
    public string SourceLocationCode { get; init; } = string.Empty;
    public string DestinationLocationCode { get; init; } = string.Empty;
    public int Quantity { get; init; }

    public string ProductDisplay => $"{ProductName} ({Article})";
}