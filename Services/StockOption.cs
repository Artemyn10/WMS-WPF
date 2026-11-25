namespace WMS.Services;

// Остаток товара в ячейке - вариант для выбора в строке списания
public class StockOption
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public int Available { get; set; }

    public string Display => $"{ProductName} — {LocationCode} (доступно {Available})";
}