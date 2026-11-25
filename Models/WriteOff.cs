namespace WMS.Models;

public class WriteOff
{
    public int Id { get; set; }
    public required string DocumentNumber { get; set; }
    public DateTime Date { get; set; }
    public int WarehouseId { get; set; }
    public required string Reason { get; set; }
    public string? Comment { get; set; }

    // Кто оформил документ
    public int? CreatedByUserId { get; set; }

    public Warehouse? Warehouse { get; set; }
    public User? CreatedBy { get; set; }
    public ICollection<WriteOffItem> Items { get; set; } = new List<WriteOffItem>();
}