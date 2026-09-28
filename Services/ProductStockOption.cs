namespace WMS.Services;

// Товар, у которого есть хотя бы один остаток - для выбора в перемещении
public class ProductStockOption
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
}