using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WMS.Models;

public class WarehouseTransferItem
{
    public int Id { get; set; }
    public int WarehouseTransferId { get; set; }
    public int ProductId { get; set; }
    public int SourceStorageLocationId { get; set; }
    public int DestinationStorageLocationId { get; set; }
    public int Quantity { get; set; }

    [ForeignKey(nameof(WarehouseTransferId))]
    public WarehouseTransfer? Transfer { get; set; }

    [ForeignKey(nameof(ProductId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Product? Product { get; set; }

    [ForeignKey(nameof(SourceStorageLocationId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public StorageLocation? SourceStorageLocation { get; set; }

    [ForeignKey(nameof(DestinationStorageLocationId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public StorageLocation? DestinationStorageLocation { get; set; }
}