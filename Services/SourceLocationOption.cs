namespace WMS.Services;

// Ячейка-источник для конкретного товара: код + сколько там лежит
public class SourceLocationOption
{
    public int LocationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Available { get; set; }

    public string Display => $"{Code} (доступно {Available})";
}