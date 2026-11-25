using System.ComponentModel.DataAnnotations.Schema;
using WMS.Helpers;

namespace WMS.Models;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Article { get; set; }
    public string? Barcode { get; set; }
    public int CategoryId { get; set; }
    public required string Unit { get; set; }
    public string? Description { get; set; }
    public int? ReleaseYear { get; set; }

    // Вид номенклатуры (товар, материал, оборудование)
    public ProductType ItemType { get; set; }

    public string? Manufacturer { get; set; }

    // Основной поставщик
    public int? SupplierId { get; set; }

    // Технические характеристики (свободный текст)
    public string? Characteristics { get; set; }

    // Закупочная и учётная стоимость
    public decimal? PurchasePrice { get; set; }
    public decimal? AccountingPrice { get; set; }

    public Category? Category { get; set; }
    public Supplier? Supplier { get; set; }
    public ICollection<Inventory> InventoryRecords { get; set; } = new List<Inventory>();
    public ICollection<ProductFile> Files { get; set; } = new List<ProductFile>();

    // Только для таблиц, в базе не хранятся
    [NotMapped]
    public int FilesCount { get; set; }

    [NotMapped]
    public string ItemTypeDisplay => ProductTypeNames.ToRussian(ItemType);
}