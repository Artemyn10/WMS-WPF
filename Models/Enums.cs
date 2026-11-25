namespace WMS.Models;

public enum UserRole
{
    Administrator = 0,
    Storekeeper = 1,
    Director = 2,
    WarehouseManager = 3,
    PurchasingManager = 4,
    SalesManager = 5,
    InventorySpecialist = 6
}

public enum ReceiptStatus
{
    New,
    Confirmed
}

public enum OrderStatus
{
    New,
    Assembling,
    Assembled,
    Shipped
}

public enum ShipmentStatus
{
    InProgress,
    Completed
}

public enum OperationType
{
    Receipt,
    Placement,
    Movement,
    Picking,
    Shipment,
    WriteOff,
    StockTaking,
    InterWarehouseTransfer
}

public enum StockTakingStatus
{
    InProgress,
    Completed
}

public enum PurchaseOrderStatus
{
    New,
    PartiallyReceived,
    Received,
    Cancelled
}

public enum ProductType
{
    Goods = 0,
    Material = 1,
    Equipment = 2
}

public enum ProductFileKind
{
    Photo = 0,
    Document = 1
}

public enum WarehouseTransferStatus
{
    Created = 0,
    Shipped = 1,
    Received = 2
}