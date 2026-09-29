using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class SupplierEditViewModel : ViewModelBase
{
    private readonly SupplierService _supplierService;
    private int _id;

    private string _name = string.Empty;
    public string Name { get => _name; set => SetField(ref _name, value); }

    private string _phone = string.Empty;
    public string Phone { get => _phone; set => SetField(ref _phone, value); }

    private string _email = string.Empty;
    public string Email { get => _email; set => SetField(ref _email, value); }

    private string _address = string.Empty;
    public string Address { get => _address; set => SetField(ref _address, value); }

    private string _errorMessage = string.Empty;
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public string WindowTitle { get; private set; } = "Добавить поставщика";

    public event Action<bool?>? RequestClose;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public SupplierEditViewModel(SupplierService supplierService)
    {
        _supplierService = supplierService;
        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    public void InitializeForAdd()
    {
        _id = 0;
        Name = Phone = Email = Address = string.Empty;
        WindowTitle = "Добавить поставщика";
        OnPropertyChanged(nameof(WindowTitle));
    }

    public void InitializeForEdit(Supplier supplier)
    {
        _id = supplier.Id;
        Name = supplier.Name;
        Phone = supplier.Phone ?? string.Empty;
        Email = supplier.Email ?? string.Empty;
        Address = supplier.Address ?? string.Empty;
        WindowTitle = "Изменить поставщика";
        OnPropertyChanged(nameof(WindowTitle));
    }

    private async Task SaveAsync()
    {
        var supplier = new Supplier { Id = _id, Name = Name, Phone = Phone, Email = Email, Address = Address };

        var result = _id == 0
            ? await _supplierService.AddAsync(supplier)
            : await _supplierService.UpdateAsync(supplier);

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