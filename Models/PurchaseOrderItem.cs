using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

public class PurchaseOrderItem
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ProductId { get; set; }

    // Сколько заказано
    public int Quantity { get; set; }

    // Сколько уже получено по приёмкам
    public int ReceivedQuantity { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }
    public Product? Product { get; set; }

    [NotMapped]
    public int Remaining => Quantity - ReceivedQuantity;
}