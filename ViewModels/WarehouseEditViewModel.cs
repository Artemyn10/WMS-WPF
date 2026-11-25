using System.Globalization;
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

    private string _areaText = string.Empty;
    public string AreaText { get => _areaText; set => SetField(ref _areaText, value); }

    private string _purpose = string.Empty;
    public string Purpose { get => _purpose; set => SetField(ref _purpose, value); }

    private string _workingHours = string.Empty;
    public string WorkingHours { get => _workingHours; set => SetField(ref _workingHours, value); }

    private string _phone = string.Empty;
    public string Phone { get => _phone; set => SetField(ref _phone, value); }

    private string _email = string.Empty;
    public string Email { get => _email; set => SetField(ref _email, value); }

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
        Name = Address = AreaText = Purpose = WorkingHours = Phone = Email = string.Empty;
        ErrorMessage = string.Empty;
        WindowTitle = "Добавить склад";
        OnPropertyChanged(nameof(WindowTitle));
    }

    public void InitializeForEdit(Warehouse warehouse)
    {
        _id = warehouse.Id;
        Name = warehouse.Name;
        Address = warehouse.Address ?? string.Empty;
        AreaText = warehouse.Area?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
        Purpose = warehouse.Purpose ?? string.Empty;
        WorkingHours = warehouse.WorkingHours ?? string.Empty;
        Phone = warehouse.Phone ?? string.Empty;
        Email = warehouse.Email ?? string.Empty;
        ErrorMessage = string.Empty;
        WindowTitle = "Изменить склад";
        OnPropertyChanged(nameof(WindowTitle));
    }

    private async Task SaveAsync()
    {
        decimal? area = null;
        if (!string.IsNullOrWhiteSpace(AreaText))
        {
            // Принимаем и запятую, и точку: "1500,5" и "1500.5"
            var normalized = AreaText.Trim().Replace(',', '.');
            if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                ErrorMessage = "Площадь должна быть числом.";
                return;
            }
            area = parsed;
        }

        var warehouse = new Warehouse
        {
            Id = _id,
            Name = Name,
            Address = Address,
            Area = area,
            Purpose = Purpose,
            WorkingHours = WorkingHours,
            Phone = Phone,
            Email = Email
        };

        var result = _id == 0
            ? await _warehouseService.AddAsync(warehouse)
            : await _warehouseService.UpdateAsync(warehouse);

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