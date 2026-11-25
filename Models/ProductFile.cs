namespace WMS.Models;

public class ProductFile
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public required string FileName { get; set; }
    public ProductFileKind Kind { get; set; }

    // Размер в байтах
    public int Size { get; set; }

    // Содержимое файла
    public required byte[] Data { get; set; }

    public DateTime UploadedAt { get; set; }

    public Product? Product { get; set; }
}