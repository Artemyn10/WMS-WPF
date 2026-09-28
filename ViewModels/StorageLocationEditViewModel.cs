using System.Collections.ObjectModel;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class StorageLocationEditViewModel : ViewModelBase
{
    private readonly StorageLocationService _locationService;
    private readonly WarehouseService _warehouseService;
    private int _id;

    public ObservableCollection<Warehouse> Warehouses { get; } = new();

    private Warehouse? _selectedWarehouse;
    public Warehouse? SelectedWarehouse { get => _selectedWarehouse; set => SetField(ref _selectedWarehouse, value); }

    private string _code = string.Empty;
    public string Code { get => _code; set => SetField(ref _code, value); }

    private int _capacity;
    public int Capacity { get => _capacity; set => SetField(ref _capacity, value); }

    private string _errorMessage = string.Empty;
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public string WindowTitle { get; private set; } = "Добавить ячейку";

    public event Action<bool?>? RequestClose;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public StorageLocationEditViewModel(StorageLocationService locationService, WarehouseService warehouseService)
    {
        _locationService = locationService;
        _warehouseService = warehouseService;

        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    public async Task InitializeForAddAsync()
    {
        await LoadWarehousesAsync();
        _id = 0;
        Code = string.Empty;
        Capacity = 0;
        SelectedWarehouse = null;
        WindowTitle = "Добавить ячейку";
        OnPropertyChanged(nameof(WindowTitle));
    }

    public async Task InitializeForEditAsync(StorageLocation location)
    {
        await LoadWarehousesAsync();
        _id = location.Id;
        Code = location.Code;
        Capacity = location.Capacity;
        SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == location.WarehouseId);
        WindowTitle = "Изменить ячейку";
        OnPropertyChanged(nameof(WindowTitle));
    }

    private async Task LoadWarehousesAsync()
    {
        var items = await _warehouseService.GetAllAsync();
        Warehouses.Clear();
        foreach (var item in items) Warehouses.Add(item);
    }

    private async Task SaveAsync()
    {
        var warehouseId = SelectedWarehouse?.Id ?? 0;

        var result = _id == 0
            ? await _locationService.AddAsync(warehouseId, Code, Capacity)
            : await _locationService.UpdateAsync(_id, warehouseId, Code, Capacity);

        if (result.Success)
        {
            ErrorMessage = string.Empty;
            RequestClose?.Invoke(true);
        }
        else
        {
            ErrorMessage = result.Error ?? "Ошибка сохранения.";
        }
    }
}