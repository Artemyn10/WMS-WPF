using System.Collections.ObjectModel;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class OrderEditViewModel : ViewModelBase
{
    private readonly OrderService _orderService;
    private readonly CustomerService _customerService;
    private readonly WarehouseService _warehouseService;
    private readonly ProductService _productService;

    public ObservableCollection<Customer> Customers { get; } = new();
    public ObservableCollection<Warehouse> Warehouses { get; } = new();
    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<OrderItemRowViewModel> Items { get; } = new();

    private Customer? _selectedCustomer;
    public Customer? SelectedCustomer { get => _selectedCustomer; set => SetField(ref _selectedCustomer, value); }

    private Warehouse? _selectedWarehouse;
    public Warehouse? SelectedWarehouse { get => _selectedWarehouse; set => SetField(ref _selectedWarehouse, value); }

    private string _orderNumber = string.Empty;
    public string OrderNumber { get => _orderNumber; set => SetField(ref _orderNumber, value); }

    private string _errorMessage = string.Empty;
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public event Action<bool?>? RequestClose;

    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public OrderEditViewModel(
        OrderService orderService,
        CustomerService customerService,
        WarehouseService warehouseService,
        ProductService productService)
    {
        _orderService = orderService;
        _customerService = customerService;
        _warehouseService = warehouseService;
        _productService = productService;

        AddItemCommand = new RelayCommand(_ => Items.Add(new OrderItemRowViewModel(Products)));
        RemoveItemCommand = new RelayCommand(row => RemoveItem(row as OrderItemRowViewModel));
        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    public async Task InitializeAsync()
    {
        var customers = await _customerService.GetAllAsync();
        Customers.Clear();
        foreach (var c in customers) Customers.Add(c);

        var warehouses = await _warehouseService.GetAllAsync();
        Warehouses.Clear();
        foreach (var w in warehouses) Warehouses.Add(w);

        var products = await _productService.GetAllAsync();
        Products.Clear();
        foreach (var p in products) Products.Add(p);

        SelectedCustomer = null;
        SelectedWarehouse = null;
        OrderNumber = await _orderService.GenerateNextOrderNumberAsync();
        ErrorMessage = string.Empty;

        Items.Clear();
        Items.Add(new OrderItemRowViewModel(Products));
    }

    private void RemoveItem(OrderItemRowViewModel? row)
    {
        if (row != null && Items.Contains(row))
        {
            Items.Remove(row);
        }
    }

    private async Task SaveAsync()
    {
        var items = Items
            .Where(i => i.SelectedProduct != null)
            .Select(i => new OrderItemInput { ProductId = i.SelectedProduct!.Id, Quantity = i.Quantity })
            .ToList();

        var result = await _orderService.AddAsync(
            SelectedCustomer?.Id ?? 0,
            SelectedWarehouse?.Id ?? 0,
            OrderNumber,
            items);

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