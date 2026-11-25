namespace WMS.Services;

public class WarehouseSummaryRow
{
    public int WarehouseId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Общие сведения
    public string Address { get; set; } = "—";
    public decimal? Area { get; set; }
    public string Purpose { get; set; } = "—";
    public string WorkingHours { get; set; } = "—";
    public string Phone { get; set; } = "—";
    public string Email { get; set; } = "—";
    public string ResponsibleNames { get; set; } = "—";
    public int EmployeesCount { get; set; }

    // Загруженность
    public int LocationsCount { get; set; }
    public int Capacity { get; set; }
    public int Stored { get; set; }

    public int Free => Math.Max(0, Capacity - Stored);
    public double LoadPercent => Capacity > 0 ? Math.Min(100, Stored * 100.0 / Capacity) : 0;

    // Деятельность
    public int ReceiptsCount { get; set; }
    public int OrdersCount { get; set; }
    public int ActiveOrdersCount { get; set; }
    public int ShipmentsCount { get; set; }
}