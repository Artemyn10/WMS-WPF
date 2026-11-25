using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;
using WMS.Views;

namespace WMS.ViewModels;

public class StockTakingsViewModel : ViewModelBase
{
    private readonly StockTakingService _stockTakingService;
    private readonly WarehouseService _warehouseService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<StockTaking> StockTakings { get; } = new();

    // Позиции выбранного документа (акт с результатами пересчёта)
    public ObservableCollection<StockTakingItem> SelectedItems { get; } = new();

    public ObservableCollection<Warehouse> Warehouses { get; } = new();

    private Warehouse? _selectedWarehouse;
    public Warehouse? SelectedWarehouse { get => _selectedWarehouse; set => SetField(ref _selectedWarehouse, value); }

    private string _comment = string.Empty;
    public string Comment { get => _comment; set => SetField(ref _comment, value); }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) _ = LoadAsync(); }
    }

    private StockTaking? _selectedStockTaking;
    public StockTaking? SelectedStockTaking
    {
        get => _selectedStockTaking;
        set
        {
            if (SetField(ref _selectedStockTaking, value))
            {
                FillSelectedItems();
            }
        }
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

    public ICommand StartCommand { get; }
    public ICommand ContinueCommand { get; }
    public ICommand DeleteCommand { get; }

    public StockTakingsViewModel(
        StockTakingService stockTakingService,
        WarehouseService warehouseService,
        IServiceProvider serviceProvider)
    {
        _stockTakingService = stockTakingService;
        _warehouseService = warehouseService;
        _serviceProvider = serviceProvider;

        StartCommand = new RelayCommand(async _ => await StartAsync());
        ContinueCommand = new RelayCommand(
            async _ => await ContinueAsync(),
            _ => SelectedStockTaking?.Status == StockTakingStatus.InProgress);
        DeleteCommand = new RelayCommand(
            async _ => await DeleteAsync(),
            _ => SelectedStockTaking?.Status == StockTakingStatus.InProgress);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var warehouses = await _warehouseService.GetAllAsync();
        Warehouses.Clear();
        foreach (var warehouse in warehouses) Warehouses.Add(warehouse);

        // Если склад один (например, у заведующего), выбираем его сразу
        if (Warehouses.Count == 1)
        {
            SelectedWarehouse = Warehouses[0];
        }

        await LoadAsync();
    }

    private async Task LoadAsync(int? reselectId = null)
    {
        var items = await _stockTakingService.GetAllAsync(SearchText);

        StockTakings.Clear();
        foreach (var item in items) StockTakings.Add(item);

        SelectedStockTaking = reselectId != null
            ? StockTakings.FirstOrDefault(s => s.Id == reselectId)
            : null;
    }

    private void FillSelectedItems()
    {
        SelectedItems.Clear();
        if (SelectedStockTaking == null) return;

        foreach (var item in SelectedStockTaking.Items
                     .OrderBy(i => i.StorageLocation!.Code)
                     .ThenBy(i => i.Product!.Name))
        {
            SelectedItems.Add(item);
        }
    }

    private async Task StartAsync()
    {
        StatusMessage = string.Empty;

        var result = await _stockTakingService.CreateAsync(SelectedWarehouse?.Id ?? 0, Comment);
        if (!result.Success)
        {
            StatusMessage = result.Error ?? "Не удалось начать инвентаризацию.";
            return;
        }

        Comment = string.Empty;
        await LoadAsync(result.Id);

        // Сразу открываем окно пересчёта
        await OpenCountDialogAsync(result.Id!.Value);
    }

    private async Task ContinueAsync()
    {
        StatusMessage = string.Empty;

        if (SelectedStockTaking == null) return;

        await OpenCountDialogAsync(SelectedStockTaking.Id);
    }

    private async Task OpenCountDialogAsync(int id)
    {
        var window = _serviceProvider.GetRequiredService<StockTakingCountWindow>();
        var viewModel = (StockTakingCountViewModel)window.DataContext;

        if (!await viewModel.InitializeAsync(id))
        {
            StatusMessage = "Документ недоступен для пересчёта.";
            return;
        }

        window.ShowDialog();

        // После закрытия окна обновляем список: пересчёт мог быть сохранён или завершён
        await LoadAsync(id);
    }

    private async Task DeleteAsync()
    {
        StatusMessage = string.Empty;

        var document = SelectedStockTaking;
        if (document == null) return;

        var answer = MessageBox.Show(
            $"Удалить незавершённую инвентаризацию {document.DocumentNumber}? Введённые данные пересчёта будут потеряны.",
            "Удаление инвентаризации",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes) return;

        var result = await _stockTakingService.DeleteAsync(document.Id);
        if (!result.Success)
        {
            StatusMessage = result.Error ?? "Не удалось удалить инвентаризацию.";
            return;
        }

        await LoadAsync();
    }
}