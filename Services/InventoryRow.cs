namespace WMS.Services;

// Строка раздела "Остатки": товар в ячейке либо принятый, но ещё не размещённый товар
public class InventoryRow
{
    public const string PendingLocationName = "Зона приёмки";

    public string ProductName { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public int Quantity { get; set; }

    // true - товар принят по поступлению, но ещё не размещён по ячейкам
    public bool IsPending { get; set; }

    public string StateDisplay => IsPending ? "Ожидает размещения" : "На хранении";
}