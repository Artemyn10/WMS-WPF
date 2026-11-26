namespace WMS.Services;

public class ReportingStockRow
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "—";
    public int ProductId { get; set; }
    public string Article { get; set; } = "—";
    public string ProductName { get; set; } = "—";
    public int Quantity { get; set; }
}

public class ReportingActivityRow
{
    public string Operation { get; set; } = "—";
    public string WarehouseName { get; set; } = "—";
    public string PartnerName { get; set; } = "—";
    public string DocumentNumber { get; set; } = "—";
    public DateTime Date { get; set; }
    public int Quantity { get; set; }
}

public class ReportingTransferRow
{
    public string DocumentNumber { get; set; } = "—";
    public DateTime Date { get; set; }
    public string SourceWarehouse { get; set; } = "—";
    public string DestinationWarehouse { get; set; } = "—";
    public int Quantity { get; set; }
    public string Status { get; set; } = "—";
}

public class ReportingPartnerRow
{
    public string Type { get; set; } = "—";
    public string Name { get; set; } = "—";
    public int DocumentsCount { get; set; }
    public int Quantity { get; set; }
}

public class ReportingStockTakingRow
{
    public string DocumentNumber { get; set; } = "—";
    public string WarehouseName { get; set; } = "—";
    public DateTime StartDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public int PositionsCount { get; set; }
    public int DifferencesCount { get; set; }
    public int ExcessQuantity { get; set; }
    public int DeficitQuantity { get; set; }
    public string Status { get; set; } = "—";
}

public class ReportingCapacityRow
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "—";
    public decimal Area { get; set; }
    public int LocationsCount { get; set; }
    public int Capacity { get; set; }
    public int Stored { get; set; }
    public int Free => Math.Max(0, Capacity - Stored);

    public double LoadPercent =>
        Capacity > 0
            ? Math.Min(100, Stored * 100.0 / Capacity)
            : 0;
}