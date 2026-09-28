using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;
using WMS.Views;

namespace WMS.ViewModels;

public class WarehousesViewModel : ViewModelBase
{
    private readonly WarehouseService _warehouseService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<Warehouse> Warehouses { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) _ = LoadAsync(); }
    }

    private Warehouse? _selectedWarehouse;
    public Warehouse? SelectedWarehouse
    {
        get => _selectedWarehouse;
        set => SetField(ref _selectedWarehouse, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public WarehousesViewModel(WarehouseService warehouseService, IServiceProvider serviceProvider)
    {
        _warehouseService = warehouseService;
        _serviceProvider = serviceProvider;

        AddCommand = new RelayCommand(async _ => await OpenEditDialogAsync(null));
        EditCommand = new RelayCommand(async _ => await OpenEditDialogAsync(SelectedWarehouse), _ => SelectedWarehouse != null);
        DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedWarehouse != null);

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _warehouseService.GetAllAsync(SearchText);
        Warehouses.Clear();
        foreach (var item in items) Warehouses.Add(item);
    }

    private async Task OpenEditDialogAsync(Warehouse? warehouse)
    {
        var window = _serviceProvider.GetRequiredService<WarehouseEditWindow>();
        var viewModel = (WarehouseEditViewModel)window.DataContext;

        if (warehouse == null) viewModel.InitializeForAdd();
        else viewModel.InitializeForEdit(warehouse);

        if (window.ShowDialog() == true)
        {
            await LoadAsync();
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedWarehouse == null) return;

        var result = await _warehouseService.DeleteAsync(SelectedWarehouse.Id);
        StatusMessage = result.Success ? string.Empty : (result.Error ?? string.Empty);

        if (result.Success) await LoadAsync();
    }
}