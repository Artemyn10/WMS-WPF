using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

public class PurchaseOrder
{
    public int Id { get; set; }
    public required string OrderNumber { get; set; }
    public int SupplierId { get; set; }

    // Склад, на который ожидается поставка
    public int WarehouseId { get; set; }

    public DateTime Date { get; set; }

    // Ожидаемая дата поставки (только дата, без времени)
    public DateOnly? ExpectedDate { get; set; }

    public PurchaseOrderStatus Status { get; set; }
    public string? Comment { get; set; }

    // Кто оформил заказ
    public int? CreatedByUserId { get; set; }

    public Supplier? Supplier { get; set; }
    public Warehouse? Warehouse { get; set; }
    public User? CreatedBy { get; set; }
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();

    // Только для таблиц, в базе не хранятся
    [NotMapped]
    public string StatusDisplay => Status switch
    {
        PurchaseOrderStatus.New => "Ожидает поставки",
        PurchaseOrderStatus.PartiallyReceived => "Получен частично",
        PurchaseOrderStatus.Received => "Получен",
        PurchaseOrderStatus.Cancelled => "Отменён",
        _ => Status.ToString()
    };

    [NotMapped]
    public string ProgressDisplay =>
        $"{Items.Sum(i => i.ReceivedQuantity)} из {Items.Sum(i => i.Quantity)}";
}