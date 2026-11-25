using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

public class StockTaking
{
    public int Id { get; set; }
    public required string DocumentNumber { get; set; }
    public int WarehouseId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public StockTakingStatus Status { get; set; }
    public string? Comment { get; set; }

    // Кто начал инвентаризацию
    public int? CreatedByUserId { get; set; }

    public Warehouse? Warehouse { get; set; }
    public User? CreatedBy { get; set; }
    public ICollection<StockTakingItem> Items { get; set; } = new List<StockTakingItem>();

    // Только для таблиц, в базе не хранятся
    [NotMapped]
    public string StatusDisplay => Status == StockTakingStatus.Completed ? "Завершена" : "В процессе";

    [NotMapped]
    public int ItemsCount => Items.Count;

    [NotMapped]
    public int DifferencesCount =>
        Items.Count(i => i.ActualQuantity != null && i.ActualQuantity != i.ExpectedQuantity);
}