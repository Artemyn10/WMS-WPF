using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

public class Receipt
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public required string DocumentNumber { get; set; }

    // Когда документ оформлен в системе (UTC, ставится автоматически)
    public DateTime Date { get; set; }

    // Дата поступления товара на склад (её указывает пользователь)
    public DateOnly ArrivalDate { get; set; }

    public ReceiptStatus Status { get; set; }

    // Заказ поставщику, по которому принят товар (необязательно)
    public int? PurchaseOrderId { get; set; }

    // Кто оформил поступление
    public int? CreatedByUserId { get; set; }

    public Supplier? Supplier { get; set; }
    public Warehouse? Warehouse { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public User? CreatedBy { get; set; }
    public ICollection<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();

    // Только для таблиц, в базе не хранятся
    [NotMapped]
    public string StatusDisplay => Status == ReceiptStatus.Confirmed ? "Подтверждено" : "Черновик";

    [NotMapped]
    public DateTime DateLocal => Date.ToLocalTime();

    [NotMapped]
    public int ItemsCount => Items.Count;

    [NotMapped]
    public int TotalQuantity => Items.Sum(i => i.Quantity);
}