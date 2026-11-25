using System.Collections.ObjectModel;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class ReceiptEditViewModel : ViewModelBase
{
    private readonly ReceiptService _receiptService;
    private readonly SupplierService _supplierService;
    private readonly WarehouseService _warehouseService;
    private readonly ProductService _productService;
    private readonly PurchaseOrderService _purchaseOrderService;

    public ObservableCollection<Supplier> Suppliers { get; } = new();
    public ObservableCollection<Warehouse> Warehouses { get; } = new();
    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<ReceiptItemRowViewModel> Items { get; } = new();

    // Первый элемент - "без заказа" (Id = 0), дальше открытые заказы поставщика
    public ObservableCollection<PurchaseOrderOption> PurchaseOrders { get; } = new();

    private Supplier? _selectedSupplier;
    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetField(ref _selectedSupplier, value))
            {
                _ = ReloadPurchaseOrdersAsync();
            }
        }
    }

    private Warehouse? _selectedWarehouse;
    public Warehouse? SelectedWarehouse
    {
        get => _selectedWarehouse;
        set
        {
            if (SetField(ref _selectedWarehouse, value))
            {
                _ = ReloadPurchaseOrdersAsync();
            }
        }
    }

    private PurchaseOrderOption? _selectedPurchaseOrder;
    public PurchaseOrderOption? SelectedPurchaseOrder
    {
        get => _selectedPurchaseOrder;
        set
        {
            var previous = _selectedPurchaseOrder;
            if (SetField(ref _selectedPurchaseOrder, value))
            {
                _ = OnPurchaseOrderChangedAsync(previous);
            }
        }
    }

    private string _documentNumber = string.Empty;
    public string DocumentNumber { get => _documentNumber; set => SetField(ref _documentNumber, value); }
    private DateTime? _arrivalDate = DateTime.Today;
    public DateTime? ArrivalDate { get => _arrivalDate; set => SetField(ref _arrivalDate, value); }

    private string _errorMessage = string.Empty;
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public event Action<bool?>? RequestClose;

    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public ReceiptEditViewModel(
        ReceiptService receiptService,
        SupplierService supplierService,
        WarehouseService warehouseService,
        ProductService productService,
        PurchaseOrderService purchaseOrderService)
    {
        _receiptService = receiptService;
        _supplierService = supplierService;
        _warehouseService = warehouseService;
        _productService = productService;
        _purchaseOrderService = purchaseOrderService;

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
        SelectedWarehouse = null;
        DocumentNumber = await _receiptService.GenerateNextDocumentNumberAsync();
        ArrivalDate = DateTime.Today;
        ErrorMessage = string.Empty;

        Items.Clear();
        Items.Add(new ReceiptItemRowViewModel(Products));

        await ReloadPurchaseOrdersAsync();
    }

    // Открытые заказы зависят от выбранных поставщика и склада
    private async Task ReloadPurchaseOrdersAsync()
    {
        var supplierId = SelectedSupplier?.Id ?? 0;
        var warehouseId = SelectedWarehouse?.Id ?? 0;

        var options = supplierId > 0 && warehouseId > 0
            ? await _purchaseOrderService.GetOpenForReceiptAsync(supplierId, warehouseId)
            : new List<PurchaseOrderOption>();

        // Пока грузились заказы, пользователь мог выбрать другого поставщика или склад
        if ((SelectedSupplier?.Id ?? 0) != supplierId || (SelectedWarehouse?.Id ?? 0) != warehouseId)
        {
            return;
        }

        PurchaseOrders.Clear();
        PurchaseOrders.Add(PurchaseOrderOption.None());
        foreach (var option in options) PurchaseOrders.Add(option);

        SelectedPurchaseOrder = PurchaseOrders[0];
    }

    private async Task OnPurchaseOrderChangedAsync(PurchaseOrderOption? previous)
    {
        var current = SelectedPurchaseOrder;

        // Выбрали "без заказа": если до этого был выбран заказ, строки начинаем заново
        if (current == null || current.Id == 0)
        {
            if (previous != null && previous.Id > 0)
            {
                Items.Clear();
                Items.Add(new ReceiptItemRowViewModel(Products));
            }
            return;
        }

        // Выбрали заказ: строки заполняются тем, что осталось получить
        var lines = await _purchaseOrderService.GetRemainingLinesAsync(current.Id);

        if (SelectedPurchaseOrder?.Id != current.Id) return;

        Items.Clear();
        foreach (var line in lines)
        {
            Items.Add(new ReceiptItemRowViewModel(Products)
            {
                SelectedProduct = Products.FirstOrDefault(p => p.Id == line.ProductId),
                Quantity = line.Remaining
            });
        }
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
            .Select(i => new ReceiptItemInput { ProductId = i.SelectedProduct!.Id, Quantity = i.Quantity })
            .ToList();

        int? purchaseOrderId = SelectedPurchaseOrder != null && SelectedPurchaseOrder.Id > 0
            ? SelectedPurchaseOrder.Id
            : null;

        DateOnly? arrivalDate = ArrivalDate != null ? DateOnly.FromDateTime(ArrivalDate.Value) : null;

        var result = await _receiptService.AddAsync(
            SelectedSupplier?.Id ?? 0,
            SelectedWarehouse?.Id ?? 0,
            DocumentNumber,
            arrivalDate,
            items,
            purchaseOrderId);

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