using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class CustomerEditViewModel : ViewModelBase
{
    private readonly CustomerService _customerService;
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

    public string WindowTitle { get; private set; } = "Добавить клиента";

    public event Action<bool?>? RequestClose;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public CustomerEditViewModel(CustomerService customerService)
    {
        _customerService = customerService;
        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    public void InitializeForAdd()
    {
        _id = 0;
        Name = Phone = Email = Address = string.Empty;
        WindowTitle = "Добавить клиента";
        OnPropertyChanged(nameof(WindowTitle));
    }

    public void InitializeForEdit(Customer customer)
    {
        _id = customer.Id;
        Name = customer.Name;
        Phone = customer.Phone ?? string.Empty;
        Email = customer.Email ?? string.Empty;
        Address = customer.Address ?? string.Empty;
        WindowTitle = "Изменить клиента";
        OnPropertyChanged(nameof(WindowTitle));
    }

    private async Task SaveAsync()
    {
        var customer = new Customer { Id = _id, Name = Name, Phone = Phone, Email = Email, Address = Address };

        var result = _id == 0
            ? await _customerService.AddAsync(customer)
            : await _customerService.UpdateAsync(customer);

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