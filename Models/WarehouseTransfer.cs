using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WMS.Models;

[Index(nameof(DocumentNumber), IsUnique = true)]
public class WarehouseTransfer
{
    public int Id { get; set; }

    [MaxLength(40)]
    public required string DocumentNumber { get; set; }

    public int SourceWarehouseId { get; set; }
    public int DestinationWarehouseId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateOnly PlannedDeliveryDate { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }

    public WarehouseTransferStatus Status { get; set; }

    public int? SourceResponsibleEmployeeId { get; set; }
    public int? SourceResponsibleUserId { get; set; }

    public int? DestinationResponsibleEmployeeId { get; set; }
    public int? DestinationResponsibleUserId { get; set; }

    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(SourceWarehouseId))]
    [InverseProperty(nameof(Warehouse.OutgoingTransfers))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Warehouse? SourceWarehouse { get; set; }

    [ForeignKey(nameof(DestinationWarehouseId))]
    [InverseProperty(nameof(Warehouse.IncomingTransfers))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Warehouse? DestinationWarehouse { get; set; }

    [ForeignKey(nameof(SourceResponsibleEmployeeId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public WarehouseEmployee? SourceResponsibleEmployee { get; set; }

    [ForeignKey(nameof(SourceResponsibleUserId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User? SourceResponsibleUser { get; set; }

    [ForeignKey(nameof(DestinationResponsibleEmployeeId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public WarehouseEmployee? DestinationResponsibleEmployee { get; set; }

    [ForeignKey(nameof(DestinationResponsibleUserId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User? DestinationResponsibleUser { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User? CreatedByUser { get; set; }

    [InverseProperty(nameof(WarehouseTransferItem.Transfer))]
    public ICollection<WarehouseTransferItem> Items { get; set; } =
        new List<WarehouseTransferItem>();

    [NotMapped]
    public string SourceResponsibleName =>
        SourceResponsibleUser?.FullName ??
        SourceResponsibleEmployee?.FullName ??
        "—";

    [NotMapped]
    public string DestinationResponsibleName =>
        DestinationResponsibleUser?.FullName ??
        DestinationResponsibleEmployee?.FullName ??
        "—";

    [NotMapped]
    public string StatusDisplay => Status switch
    {
        WarehouseTransferStatus.Created => "Создано",
        WarehouseTransferStatus.Shipped => "Отправлено",
        WarehouseTransferStatus.Received => "Получено",
        _ => Status.ToString()
    };

    [NotMapped]
    public int TotalQuantity => Items.Sum(item => item.Quantity);

    [NotMapped]
    public DateTime CreatedAtLocal => CreatedAt.ToLocalTime();

    [NotMapped]
    public DateTime? ShippedAtLocal => ShippedAt?.ToLocalTime();

    [NotMapped]
    public DateTime? ReceivedAtLocal => ReceivedAt?.ToLocalTime();
}