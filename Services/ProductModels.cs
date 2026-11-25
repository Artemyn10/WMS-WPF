using WMS.Models;

namespace WMS.Services;

// Новый файл, который пользователь выбрал в окне товара и который ещё не сохранён
public class ProductFileInput
{
    public string FileName { get; set; } = string.Empty;
    public ProductFileKind Kind { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
}

// Сведения о сохранённом файле без самого содержимого
public class ProductFileInfo
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public ProductFileKind Kind { get; set; }
    public int Size { get; set; }
    public DateTime UploadedAt { get; set; }
}

// Наличие товара на складе - для блока "Наличие по складам"
public class ProductStockRow
{
    public string WarehouseName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}