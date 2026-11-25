using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

public class StockTakingItem
{
    public int Id { get; set; }
    public int StockTakingId { get; set; }
    public int ProductId { get; set; }
    public int StorageLocationId { get; set; }

    // Сколько числилось в учёте на момент начала инвентаризации
    public int ExpectedQuantity { get; set; }

    // Сколько насчитали фактически (null - ещё не пересчитано)
    public int? ActualQuantity { get; set; }

    public StockTaking? StockTaking { get; set; }
    public Product? Product { get; set; }
    public StorageLocation? StorageLocation { get; set; }

    // Только для таблиц, в базе не хранятся
    [NotMapped]
    public int? Difference => ActualQuantity - ExpectedQuantity;

    [NotMapped]
    public string ResultDisplay
    {
        get
        {
            if (ActualQuantity is not int actual) return "Не пересчитано";
            if (actual > ExpectedQuantity) return "Излишек";
            if (actual < ExpectedQuantity) return "Недостача";
            return "Совпадает";
        }
    }
}