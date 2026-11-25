using System.Collections.ObjectModel;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class PurchaseOrderEditViewModel : ViewModelBase
{
    private readonly PurchaseOrderService _purchaseOrderService;
    private readonly SupplierService _supplierService;
    private readonly WarehouseService _warehouseService;
    private readonly ProductService _productService;

    public ObservableCollection<Supplier> Suppliers { get; } = new();
    public ObservableCollection<Warehouse> Warehouses { get; } = new();
    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<ReceiptItemRowViewModel> Items { get; } = new();

    private Supplier? _selectedSupplier;
    public Supplier? SelectedSupplier { get => _selectedSupplier; set => SetField(ref _selectedSupplier, value); }

    private Warehouse? _selectedWarehouse;
    public Warehouse? SelectedWarehouse { get => _selectedWarehouse; set => SetField(ref _selectedWarehouse, value); }

    private string _orderNumber = string.Empty;
    public string OrderNumber { get => _orderNumber; set => SetField(ref _orderNumber, value); }

    private DateTime? _expectedDate;
    public DateTime? ExpectedDate { get => _expectedDate; set => SetField(ref _expectedDate, value); }

    private string _comment = string.Empty;
    public string Comment { get => _comment; set => SetField(ref _comment, value); }

    private string _errorMessage = string.Empty;
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public event Action<bool?>? RequestClose;

    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public PurchaseOrderEditViewModel(
        PurchaseOrderService purchaseOrderService,
        SupplierService supplierService,
        WarehouseService warehouseService,
        ProductService productService)
    {
        _purchaseOrderService = purchaseOrderService;
        _supplierService = supplierService;
        _warehouseService = warehouseService;
        _productService = productService;

        AddItemCommand = new RelayCommand(_ => Items.Add(new ReceiptItemRowViewModel(Products)));
        RemoveItemCommand = new RelayCommand(row => RemoveItem(row as ReceiptItemRowViewModel));
        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    public async Task InitializeAsync()
    {
        var suppliers = await _supplierService.GetAllAsync();
        Suppliers.Clear();
        foreach (var s in suppliers) Suppliers.Add(s);

        var warehouses = await _warehouseService.GetAllAsync();
        Warehouses.Clear();
        foreach (var w in warehouses) Warehouses.Add(w);

        var products = await _productService.GetAllAsync();
        Products.Clear();
        foreach (var p in products) Products.Add(p);

        SelectedSupplier = null;
        SelectedWarehouse = Warehouses.Count == 1 ? Warehouses[0] : null;
        OrderNumber = await _purchaseOrderService.GenerateNextOrderNumberAsync();
        ExpectedDate = null;
        Comment = string.Empty;
        ErrorMessage = string.Empty;

        Items.Clear();
        Items.Add(new ReceiptItemRowViewModel(Products));
    }

    private void RemoveItem(ReceiptItemRowViewModel? row)
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
            .Select(i => new PurchaseOrderItemInput { ProductId = i.SelectedProduct!.Id, Quantity = i.Quantity })
            .ToList();

        DateOnly? expectedDate = ExpectedDate != null ? DateOnly.FromDateTime(ExpectedDate.Value) : null;

        var result = await _purchaseOrderService.AddAsync(
            SelectedSupplier?.Id ?? 0,
            SelectedWarehouse?.Id ?? 0,
            OrderNumber,
            expectedDate,
            Comment,
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