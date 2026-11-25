using WMS.Models;

namespace WMS.ViewModels;

public class StockTakingRowViewModel : ViewModelBase
{
    public int ItemId { get; }
    public string LocationCode { get; }
    public string ProductName { get; }
    public string Article { get; }
    public int Expected { get; }

    private string _actualText;

    // Текст из ячейки таблицы: пусто = ещё не пересчитано
    public string ActualText
    {
        get => _actualText;
        set
        {
            if (SetField(ref _actualText, value))
            {
                OnPropertyChanged(nameof(Difference));
                OnPropertyChanged(nameof(Result));
            }
        }
    }

    public int? Actual => int.TryParse(ActualText?.Trim(), out var value) ? value : null;

    public string Difference =>
        Actual is int actual ? (actual - Expected).ToString("+0;-0;0") : string.Empty;

    public string Result
    {
        get
        {
            if (Actual is not int actual) return "Не пересчитано";
            if (actual > Expected) return "Излишек";
            if (actual < Expected) return "Недостача";
            return "Совпадает";
        }
    }

    public StockTakingRowViewModel(StockTakingItem item)
    {
        ItemId = item.Id;
        LocationCode = item.StorageLocation?.Code ?? string.Empty;
        ProductName = item.Product?.Name ?? string.Empty;
        Article = item.Product?.Article ?? string.Empty;
        Expected = item.ExpectedQuantity;
        _actualText = item.ActualQuantity?.ToString() ?? string.Empty;
    }
}