using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;
using WMS.Views;

namespace WMS.ViewModels;

public class CustomersViewModel : ViewModelBase
{
    private readonly CustomerService _customerService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<Customer> Customers { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) _ = LoadAsync(); }
    }

    private Customer? _selectedCustomer;
    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetField(ref _selectedCustomer, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public CustomersViewModel(CustomerService customerService, IServiceProvider serviceProvider)
    {
        _customerService = customerService;
        _serviceProvider = serviceProvider;

        AddCommand = new RelayCommand(async _ => await OpenEditDialogAsync(null));
        EditCommand = new RelayCommand(async _ => await OpenEditDialogAsync(SelectedCustomer), _ => SelectedCustomer != null);
        DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedCustomer != null);

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _customerService.GetAllAsync(SearchText);
        Customers.Clear();
        foreach (var item in items) Customers.Add(item);
    }

    private async Task OpenEditDialogAsync(Customer? customer)
    {
        var window = _serviceProvider.GetRequiredService<CustomerEditWindow>();
        var viewModel = (CustomerEditViewModel)window.DataContext;

        if (customer == null) viewModel.InitializeForAdd();
        else viewModel.InitializeForEdit(customer);

        if (window.ShowDialog() == true)
        {
            await LoadAsync();
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedCustomer == null) return;

        var result = await _customerService.DeleteAsync(SelectedCustomer.Id);
        StatusMessage = result.Success ? string.Empty : (result.Error ?? string.Empty);

        if (result.Success) await LoadAsync();
    }
}