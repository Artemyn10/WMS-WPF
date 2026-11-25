using System.Collections.ObjectModel;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Services;

namespace WMS.ViewModels;

public class InventoryViewModel : ViewModelBase
{
    private readonly InventoryService _inventoryService;

    public ObservableCollection<InventoryRow> InventoryRecords { get; } = new();

    private string _productSearch = string.Empty;
    public string ProductSearch
    {
        get => _productSearch;
        set
        {
            if (SetField(ref _productSearch, value))
                _ = LoadAsync();
        }
    }

    private string _locationSearch = string.Empty;
    public string LocationSearch
    {
        get => _locationSearch;
        set
        {
            if (SetField(ref _locationSearch, value))
                _ = LoadAsync();
        }
    }

    private DateTime? _selectedDate = DateTime.Today;
    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetField(ref _selectedDate, value))
                _ = LoadAsync();
        }
    }

    private string _summaryText = string.Empty;
    public string SummaryText
    {
        get => _summaryText;
        set => SetField(ref _summaryText, value);
    }

    public ICommand RefreshCommand { get; }

    public InventoryViewModel(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
        RefreshCommand = new RelayCommand(async _ => await LoadAsync());

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (SelectedDate is not DateTime selectedDate)
        {
            InventoryRecords.Clear();
            SummaryText = "Выберите дату ведомости.";
            return;
        }

        var rows = await _inventoryService.GetRowsOnDateAsync(
            selectedDate,
            ProductSearch,
            LocationSearch);

        InventoryRecords.Clear();
        foreach (var row in rows)
            InventoryRecords.Add(row);

        var total = rows.Sum(row => row.Quantity);
        var pending = rows
            .Where(row => row.IsPending)
            .Sum(row => row.Quantity);

        SummaryText =
            $"Ведомость на {selectedDate:dd.MM.yyyy} · " +
            $"Всего единиц: {total} · " +
            $"ожидает размещения: {pending}";
    }
}