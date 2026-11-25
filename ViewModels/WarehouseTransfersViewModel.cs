using System.Collections.ObjectModel;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class WarehouseTransfersViewModel : ViewModelBase
{
    private readonly WarehouseTransferService _transferService;

    public ObservableCollection<Warehouse> SourceWarehouses { get; } = new();
    public ObservableCollection<Warehouse> DestinationWarehouses { get; } = new();
    public ObservableCollection<WarehouseStaffOption> SourceStaff { get; } = new();
    public ObservableCollection<WarehouseStaffOption> DestinationStaff { get; } = new();
    public ObservableCollection<ProductStockOption> Products { get; } = new();
    public ObservableCollection<SourceLocationOption> SourceLocations { get; } = new();
    public ObservableCollection<LocationOption> DestinationLocations { get; } = new();
    public ObservableCollection<WarehouseTransferLineViewModel> DraftItems { get; } = new();
    public ObservableCollection<WarehouseTransfer> Transfers { get; } = new();

    private Warehouse? _selectedSourceWarehouse;
    public Warehouse? SelectedSourceWarehouse
    {
        get => _selectedSourceWarehouse;
        set
        {
            if (SetField(ref _selectedSourceWarehouse, value))
                _ = LoadSourceWarehouseOptionsAsync();
        }
    }

    private Warehouse? _selectedDestinationWarehouse;
    public Warehouse? SelectedDestinationWarehouse
    {
        get => _selectedDestinationWarehouse;
        set
        {
            if (SetField(ref _selectedDestinationWarehouse, value))
                _ = LoadDestinationWarehouseOptionsAsync();
        }
    }

    private WarehouseStaffOption? _selectedSourceStaff;
    public WarehouseStaffOption? SelectedSourceStaff
    {
        get => _selectedSourceStaff;
        set => SetField(ref _selectedSourceStaff, value);
    }

    private WarehouseStaffOption? _selectedDestinationStaff;
    public WarehouseStaffOption? SelectedDestinationStaff
    {
        get => _selectedDestinationStaff;
        set => SetField(ref _selectedDestinationStaff, value);
    }

    private ProductStockOption? _selectedProduct;
    public ProductStockOption? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetField(ref _selectedProduct, value))
                _ = LoadSourceLocationsAsync();
        }
    }

    private SourceLocationOption? _selectedSourceLocation;
    public SourceLocationOption? SelectedSourceLocation
    {
        get => _selectedSourceLocation;
        set => SetField(ref _selectedSourceLocation, value);
    }

    private LocationOption? _selectedDestinationLocation;
    public LocationOption? SelectedDestinationLocation
    {
        get => _selectedDestinationLocation;
        set => SetField(ref _selectedDestinationLocation, value);
    }

    private int _quantity = 1;
    public int Quantity
    {
        get => _quantity;
        set => SetField(ref _quantity, value);
    }

    private DateTime? _plannedDeliveryDate = DateTime.Today;
    public DateTime? PlannedDeliveryDate
    {
        get => _plannedDeliveryDate;
        set => SetField(ref _plannedDeliveryDate, value);
    }

    private WarehouseTransfer? _selectedTransfer;
    public WarehouseTransfer? SelectedTransfer
    {
        get => _selectedTransfer;
        set => SetField(ref _selectedTransfer, value);
    }

    private string _message = string.Empty;
    public string Message
    {
        get => _message;
        set => SetField(ref _message, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand ConfirmShipmentCommand { get; }
    public ICommand ConfirmReceiptCommand { get; }
    public ICommand RefreshCommand { get; }

    public WarehouseTransfersViewModel(
        WarehouseTransferService transferService)
    {
        _transferService = transferService;

        AddItemCommand = new RelayCommand(_ => AddItem());
        RemoveItemCommand = new RelayCommand(RemoveItem);
        CreateCommand = new RelayCommand(async _ => await CreateAsync());
        ConfirmShipmentCommand =
            new RelayCommand(async _ => await ConfirmShipmentAsync());
        ConfirmReceiptCommand =
            new RelayCommand(async _ => await ConfirmReceiptAsync());
        RefreshCommand =
            new RelayCommand(async _ => await LoadTransfersAsync());

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var warehouses = await _transferService.GetWarehousesAsync();
        var scopeId = CurrentSession.WarehouseScopeId;

        foreach (var warehouse in warehouses)
        {
            DestinationWarehouses.Add(warehouse);

            if (scopeId == null || scopeId == warehouse.Id)
                SourceWarehouses.Add(warehouse);
        }

        _selectedSourceWarehouse = SourceWarehouses.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedSourceWarehouse));

        _selectedDestinationWarehouse = DestinationWarehouses.FirstOrDefault(
            warehouse => warehouse.Id != _selectedSourceWarehouse?.Id);
        OnPropertyChanged(nameof(SelectedDestinationWarehouse));

        await LoadSourceWarehouseOptionsAsync();
        await LoadDestinationWarehouseOptionsAsync();
        await LoadTransfersAsync();
    }

    private async Task LoadSourceWarehouseOptionsAsync()
    {
        Products.Clear();
        SourceLocations.Clear();
        SourceStaff.Clear();

        SelectedProduct = null;
        SelectedSourceLocation = null;
        SelectedSourceStaff = null;

        var warehouse = SelectedSourceWarehouse;
        if (warehouse == null)
            return;

        if (SelectedDestinationWarehouse?.Id == warehouse.Id)
        {
            SelectedDestinationWarehouse = DestinationWarehouses.FirstOrDefault(
                item => item.Id != warehouse.Id);
        }

        var products =
            await _transferService.GetProductsWithStockAsync(warehouse.Id);
        foreach (var product in products)
            Products.Add(product);

        var staff = await _transferService.GetStaffAsync(warehouse.Id);
        foreach (var person in staff)
            SourceStaff.Add(person);

        SelectedSourceStaff = SourceStaff.FirstOrDefault(
            person => person.IsResponsible) ?? SourceStaff.FirstOrDefault();
    }

    private async Task LoadDestinationWarehouseOptionsAsync()
    {
        DestinationLocations.Clear();
        DestinationStaff.Clear();

        SelectedDestinationLocation = null;
        SelectedDestinationStaff = null;

        var warehouse = SelectedDestinationWarehouse;
        if (warehouse == null)
            return;

        if (SelectedSourceWarehouse?.Id == warehouse.Id)
        {
            SelectedDestinationWarehouse = DestinationWarehouses.FirstOrDefault(
                item => item.Id != warehouse.Id);
            return;
        }

        var locations =
            await _transferService.GetDestinationLocationsAsync(warehouse.Id);
        foreach (var location in locations)
            DestinationLocations.Add(location);

        var staff = await _transferService.GetStaffAsync(warehouse.Id);
        foreach (var person in staff)
            DestinationStaff.Add(person);

        SelectedDestinationStaff = DestinationStaff.FirstOrDefault(
            person => person.IsResponsible) ?? DestinationStaff.FirstOrDefault();

        SelectedDestinationLocation = DestinationLocations.FirstOrDefault();
    }

    private async Task LoadSourceLocationsAsync()
    {
        SourceLocations.Clear();
        SelectedSourceLocation = null;

        if (SelectedSourceWarehouse == null || SelectedProduct == null)
            return;

        var locations = await _transferService.GetSourceLocationsAsync(
            SelectedSourceWarehouse.Id,
            SelectedProduct.ProductId);

        foreach (var location in locations)
            SourceLocations.Add(location);

        SelectedSourceLocation = SourceLocations.FirstOrDefault();
    }

    private void AddItem()
    {
        ErrorMessage = string.Empty;
        Message = string.Empty;

        if (SelectedProduct == null ||
            SelectedSourceLocation == null ||
            SelectedDestinationLocation == null)
        {
            ErrorMessage = "Выберите товар, исходную и целевую ячейки.";
            return;
        }

        if (Quantity <= 0)
        {
            ErrorMessage = "Количество должно быть больше нуля.";
            return;
        }

        var alreadyRequested = DraftItems
            .Where(item =>
                item.ProductId == SelectedProduct.ProductId &&
                item.SourceLocationId == SelectedSourceLocation.LocationId)
            .Sum(item => item.Quantity);

        if (alreadyRequested + Quantity > SelectedSourceLocation.Available)
        {
            ErrorMessage =
                $"В ячейке доступно {SelectedSourceLocation.Available} ед.; " +
                $"в документе уже указано {alreadyRequested} ед.";
            return;
        }

        var targetAlreadyRequested = DraftItems
            .Where(item =>
                item.DestinationLocationId == SelectedDestinationLocation.Id)
            .Sum(item => item.Quantity);

        if (SelectedDestinationLocation.Used +
            targetAlreadyRequested +
            Quantity > SelectedDestinationLocation.Capacity)
        {
            ErrorMessage =
                "В выбранной ячейке получателя недостаточно свободного места.";
            return;
        }

        DraftItems.Add(new WarehouseTransferLineViewModel
        {
            ProductId = SelectedProduct.ProductId,
            SourceLocationId = SelectedSourceLocation.LocationId,
            DestinationLocationId = SelectedDestinationLocation.Id,
            ProductName = SelectedProduct.Name,
            Article = SelectedProduct.Article,
            SourceLocationCode = SelectedSourceLocation.Code,
            DestinationLocationCode = SelectedDestinationLocation.Code,
            Quantity = Quantity
        });

        Quantity = 1;
    }

    private void RemoveItem(object? parameter)
    {
        if (parameter is WarehouseTransferLineViewModel item)
            DraftItems.Remove(item);
    }

    private async Task CreateAsync()
    {
        ErrorMessage = string.Empty;
        Message = string.Empty;

        if (SelectedSourceWarehouse == null ||
            SelectedDestinationWarehouse == null ||
            SelectedSourceStaff == null ||
            SelectedDestinationStaff == null ||
            PlannedDeliveryDate == null)
        {
            ErrorMessage =
                "Укажите оба склада, ответственных и планируемую дату доставки.";
            return;
        }

        var items = DraftItems.Select(item => new WarehouseTransferItemInput(
            item.ProductId,
            item.SourceLocationId,
            item.DestinationLocationId,
            item.Quantity)).ToList();

        var result = await _transferService.CreateAsync(
            SelectedSourceWarehouse.Id,
            SelectedDestinationWarehouse.Id,
            SelectedSourceStaff,
            SelectedDestinationStaff,
            PlannedDeliveryDate.Value,
            items);

        if (!result.Success || result.Transfer == null)
        {
            ErrorMessage =
                result.Error ?? "Не удалось создать документ перемещения.";
            return;
        }

        DraftItems.Clear();
        Message = $"Создан документ {result.Transfer.DocumentNumber}.";
        await LoadTransfersAsync(result.Transfer.Id);
    }

    private async Task ConfirmShipmentAsync()
    {
        ErrorMessage = string.Empty;
        Message = string.Empty;

        if (SelectedTransfer == null)
        {
            ErrorMessage = "Выберите документ.";
            return;
        }

        var transferId = SelectedTransfer.Id;
        var result =
            await _transferService.ConfirmShipmentAsync(transferId);

        if (!result.Success)
        {
            ErrorMessage =
                result.Error ?? "Не удалось подтвердить отправку.";
            return;
        }

        Message =
            "Отправка подтверждена. Остаток списан со склада-отправителя.";
        await LoadTransfersAsync(transferId);
    }

    private async Task ConfirmReceiptAsync()
    {
        ErrorMessage = string.Empty;
        Message = string.Empty;

        if (SelectedTransfer == null)
        {
            ErrorMessage = "Выберите документ.";
            return;
        }

        var transferId = SelectedTransfer.Id;
        var result =
            await _transferService.ConfirmReceiptAsync(transferId);

        if (!result.Success)
        {
            ErrorMessage =
                result.Error ?? "Не удалось подтвердить получение.";
            return;
        }

        Message =
            "Получение подтверждено. Товар зачислен на склад-получатель.";
        await LoadTransfersAsync(transferId);
    }

    private async Task LoadTransfersAsync(int? selectedTransferId = null)
    {
        var keepId = selectedTransferId ?? SelectedTransfer?.Id;
        var transfers = await _transferService.GetTransfersAsync();

        Transfers.Clear();
        foreach (var transfer in transfers)
            Transfers.Add(transfer);

        SelectedTransfer = Transfers.FirstOrDefault(
            transfer => transfer.Id == keepId) ?? Transfers.FirstOrDefault();
    }
}