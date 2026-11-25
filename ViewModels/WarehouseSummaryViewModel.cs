using System.Collections.ObjectModel;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Services;

namespace WMS.ViewModels;

public class WarehouseSummaryViewModel : ViewModelBase
{
    private readonly WarehouseSummaryService _summaryService;

    public ObservableCollection<SummaryCardViewModel> Cards { get; } = new();
    public ObservableCollection<WarehouseSummaryRow> Rows { get; } = new();

    public ICommand RefreshCommand { get; }

    public WarehouseSummaryViewModel(WarehouseSummaryService summaryService)
    {
        _summaryService = summaryService;

        RefreshCommand = new RelayCommand(async _ => await LoadAsync());

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var rows = await _summaryService.GetSummaryAsync();

        Rows.Clear();
        foreach (var row in rows) Rows.Add(row);

        var totalCapacity = rows.Sum(r => r.Capacity);
        var totalStored = rows.Sum(r => r.Stored);
        var totalArea = rows.Sum(r => r.Area ?? 0);
        var totalLoad = totalCapacity > 0 ? Math.Min(100, totalStored * 100.0 / totalCapacity) : 0;

        Cards.Clear();
        Cards.Add(new SummaryCardViewModel("Складов", rows.Count.ToString()));
        Cards.Add(new SummaryCardViewModel("Общая площадь, м²", totalArea.ToString("0.##")));
        Cards.Add(new SummaryCardViewModel("Общая вместимость", totalCapacity.ToString()));
        Cards.Add(new SummaryCardViewModel("Хранится, ед.", totalStored.ToString()));
        Cards.Add(new SummaryCardViewModel("Общая загруженность", $"{totalLoad:0.#}%"));
        Cards.Add(new SummaryCardViewModel("Сотрудников", rows.Sum(r => r.EmployeesCount).ToString()));
        Cards.Add(new SummaryCardViewModel("Приёмок", rows.Sum(r => r.ReceiptsCount).ToString()));
        Cards.Add(new SummaryCardViewModel("Заказов", rows.Sum(r => r.OrdersCount).ToString()));
        Cards.Add(new SummaryCardViewModel("Активных заказов", rows.Sum(r => r.ActiveOrdersCount).ToString()));
        Cards.Add(new SummaryCardViewModel("Отгрузок", rows.Sum(r => r.ShipmentsCount).ToString()));
    }
}