namespace WMS.Services;

public sealed record WarehouseTransferItemInput(
    int ProductId,
    int SourceStorageLocationId,
    int DestinationStorageLocationId,
    int Quantity);