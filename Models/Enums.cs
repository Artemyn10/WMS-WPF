using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Models;

public enum UserRole
{
    Administrator,
    Storekeeper
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
    Shipment
}
