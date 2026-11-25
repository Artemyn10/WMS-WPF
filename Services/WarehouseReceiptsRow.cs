namespace WMS.Services;

// Сводка поступлений по одному складу за выбранный период
public class WarehouseReceiptsRow
{
    public string WarehouseName { get; set; } = string.Empty;
    public int DocumentsCount { get; set; }
    public int TotalQuantity { get; set; }
    public DateOnly LastArrivalDate { get; set; }
}