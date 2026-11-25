using System.Collections.ObjectModel;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class WarehouseEmployeeEditViewModel : ViewModelBase
{
    private readonly WarehouseEmployeeService _employeeService;
    private readonly WarehouseService _warehouseService;
    private int _id;

    public ObservableCollection<Warehouse> Warehouses { get; } = new();

    private Warehouse? _selectedWarehouse;
    public Warehouse? SelectedWarehouse { get => _selectedWarehouse; set => SetField(ref _selectedWarehouse, value); }

    private string _fullName = string.Empty;
    public string FullName { get => _fullName; set => SetField(ref _fullName, value); }

    private string _position = string.Empty;
    public string Position { get => _position; set => SetField(ref _position, value); }

    private string _phone = string.Empty;
    public string Phone { get => _phone; set => SetField(ref _phone, value); }

    private string _email = string.Empty;
    public string Email { get => _email; set => SetField(ref _email, value); }

    private bool _isResponsible;
    public bool IsResponsible { get => _isResponsible; set => SetField(ref _isResponsible, value); }

    private string _errorMessage = string.Empty;
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public string WindowTitle { get; private set; } = "Добавить сотрудника";

    public event Action<bool?>? RequestClose;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public WarehouseEmployeeEditViewModel(WarehouseEmployeeService employeeService, WarehouseService warehouseService)
    {
        _employeeService = employeeService;
        _warehouseService = warehouseService;

        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    /// preselectedWarehouseId > 0 - сразу выбрать этот склад (если в списке выбран фильтр по складу)
    public async Task InitializeForAddAsync(int preselectedWarehouseId)
    {
        await LoadWarehousesAsync();
        _id = 0;
        FullName = Position = Phone = Email = string.Empty;
        IsResponsible = false;
        SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == preselectedWarehouseId);
        ErrorMessage = string.Empty;
        WindowTitle = "Добавить сотрудника";
        OnPropertyChanged(nameof(WindowTitle));
    }

    public async Task InitializeForEditAsync(WarehouseEmployee employee)
    {
        await LoadWarehousesAsync();
        _id = employee.Id;
        FullName = employee.FullName;
        Position = employee.Position;
        Phone = employee.Phone ?? string.Empty;
        Email = employee.Email ?? string.Empty;
        IsResponsible = employee.IsResponsible;
        SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == employee.WarehouseId);
        ErrorMessage = string.Empty;
        WindowTitle = "Изменить сотрудника";
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
        var employee = new WarehouseEmployee
        {
            Id = _id,
            WarehouseId = SelectedWarehouse?.Id ?? 0,
            FullName = FullName,
            Position = Position,
            Phone = Phone,
            Email = Email,
            IsResponsible = IsResponsible
        };

        var result = _id == 0
            ? await _employeeService.AddAsync(employee)
            : await _employeeService.UpdateAsync(employee);

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