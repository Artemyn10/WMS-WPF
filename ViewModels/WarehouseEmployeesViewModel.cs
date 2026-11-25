using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;
using WMS.Views;

namespace WMS.ViewModels;

public class WarehouseEmployeesViewModel : ViewModelBase
{
    private readonly WarehouseEmployeeService _employeeService;
    private readonly WarehouseService _warehouseService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<WarehouseStaffOption> Staff { get; } = new();
    public ObservableCollection<Warehouse> WarehouseFilters { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
                _ = LoadAsync();
        }
    }

    private Warehouse? _selectedWarehouseFilter;
    public Warehouse? SelectedWarehouseFilter
    {
        get => _selectedWarehouseFilter;
        set
        {
            if (SetField(ref _selectedWarehouseFilter, value))
                _ = LoadAsync();
        }
    }

    private WarehouseStaffOption? _selectedStaff;
    public WarehouseStaffOption? SelectedStaff
    {
        get => _selectedStaff;
        set => SetField(ref _selectedStaff, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleResponsibilityCommand { get; }

    public WarehouseEmployeesViewModel(
        WarehouseEmployeeService employeeService,
        WarehouseService warehouseService,
        IServiceProvider serviceProvider)
    {
        _employeeService = employeeService;
        _warehouseService = warehouseService;
        _serviceProvider = serviceProvider;

        AddCommand = new RelayCommand(async _ => await OpenEditDialogAsync(null));
        EditCommand = new RelayCommand(async _ => await EditSelectedAsync());
        DeleteCommand = new RelayCommand(async _ => await DeleteSelectedAsync());
        ToggleResponsibilityCommand =
            new RelayCommand(async parameter => await ToggleResponsibilityAsync(parameter));

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var warehouses = await _warehouseService.GetAllAsync();

        WarehouseFilters.Clear();
        WarehouseFilters.Add(new Warehouse { Id = 0, Name = "Все склады" });

        foreach (var warehouse in warehouses)
            WarehouseFilters.Add(warehouse);

        SelectedWarehouseFilter = WarehouseFilters[0];
    }

    private async Task LoadAsync()
    {
        var items = await _employeeService.GetStaffAsync(
            SearchText,
            SelectedWarehouseFilter?.Id ?? 0);

        Staff.Clear();
        foreach (var item in items)
            Staff.Add(item);
    }

    private async Task OpenEditDialogAsync(WarehouseEmployee? employee)
    {
        var window = _serviceProvider.GetRequiredService<WarehouseEmployeeEditWindow>();
        var viewModel = (WarehouseEmployeeEditViewModel)window.DataContext;

        if (employee == null)
        {
            await viewModel.InitializeForAddAsync(
                SelectedWarehouseFilter?.Id ?? 0);
        }
        else
        {
            await viewModel.InitializeForEditAsync(employee);
        }

        if (window.ShowDialog() == true)
            await LoadAsync();
    }

    private async Task EditSelectedAsync()
    {
        if (SelectedStaff == null ||
            SelectedStaff.Type != WarehouseStaffType.Employee)
        {
            StatusMessage = "Аккаунты пользователей изменяются во вкладке «Пользователи».";
            return;
        }

        var employee = new WarehouseEmployee
        {
            Id = SelectedStaff.Id,
            WarehouseId = SelectedStaff.WarehouseId,
            FullName = SelectedStaff.FullName,
            Position = SelectedStaff.Position ?? string.Empty,
            Phone = SelectedStaff.Phone,
            Email = SelectedStaff.Email,
            IsResponsible = SelectedStaff.IsResponsible
        };

        await OpenEditDialogAsync(employee);
    }

    private async Task DeleteSelectedAsync()
    {
        if (SelectedStaff == null ||
            SelectedStaff.Type != WarehouseStaffType.Employee)
        {
            StatusMessage = "Удалить можно только запись обычного сотрудника.";
            return;
        }

        var result = await _employeeService.DeleteAsync(SelectedStaff.Id);
        StatusMessage = result.Success
            ? string.Empty
            : result.Error ?? "Не удалось удалить сотрудника.";

        if (result.Success)
            await LoadAsync();
    }

    private async Task ToggleResponsibilityAsync(object? parameter)
    {
        if (parameter is not WarehouseStaffOption staff)
            return;

        var result = await _employeeService.SetResponsibilityAsync(
            staff.WarehouseId,
            staff.Id,
            staff.Type,
            !staff.IsResponsible);

        if (!result.Success)
        {
            StatusMessage = result.Error ?? "Не удалось изменить признак ответственности.";
            return;
        }

        StatusMessage = string.Empty;
        await LoadAsync();
    }
}