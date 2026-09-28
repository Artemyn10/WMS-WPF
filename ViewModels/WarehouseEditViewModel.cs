using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class WarehouseEditViewModel : ViewModelBase
{
    private readonly WarehouseService _warehouseService;
    private int _id;

    private string _name = string.Empty;
    public string Name { get => _name; set => SetField(ref _name, value); }

    private string _address = string.Empty;
    public string Address { get => _address; set => SetField(ref _address, value); }

    private string _errorMessage = string.Empty;
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public string WindowTitle { get; private set; } = "Добавить склад";

    public event Action<bool?>? RequestClose;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public WarehouseEditViewModel(WarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    public void InitializeForAdd()
    {
        _id = 0;
        Name = string.Empty;
        Address = string.Empty;
        WindowTitle = "Добавить склад";
        OnPropertyChanged(nameof(WindowTitle));
    }

    public void InitializeForEdit(Warehouse warehouse)
    {
        _id = warehouse.Id;
        Name = warehouse.Name;
        Address = warehouse.Address ?? string.Empty;
        WindowTitle = "Изменить склад";
        OnPropertyChanged(nameof(WindowTitle));
    }

    private async Task SaveAsync()
    {
        var result = _id == 0
            ? await _warehouseService.AddAsync(Name, Address)
            : await _warehouseService.UpdateAsync(_id, Name, Address);

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