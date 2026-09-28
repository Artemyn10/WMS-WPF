using System.Collections.ObjectModel;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class PickingViewModel : ViewModelBase
{
    private readonly PickingService _pickingService;

    public ObservableCollection<Order> Orders { get; } = new();
    public ObservableCollection<PendingPickingItem> PendingItems { get; } = new();
    public ObservableCollection<SourceLocationOption> Locations { get; } = new();

    private Order? _selectedOrder;
    public Order? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            if (SetField(ref _selectedOrder, value))
            {
                _ = OnOrderChangedAsync();
            }
        }
    }

    private PendingPickingItem? _selectedItem;
    public PendingPickingItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetField(ref _selectedItem, value))
            {
                _ = OnSelectedItemChangedAsync();
            }
        }
    }

    private SourceLocationOption? _selectedLocation;
    public SourceLocationOption? SelectedLocation
    {
        get => _selectedLocation;
        set => SetField(ref _selectedLocation, value);
    }

    private int _quantity;
    public int Quantity
    {
        get => _quantity;
        set => SetField(ref _quantity, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    private string _successMessage = string.Empty;
    public string SuccessMessage
    {
        get => _successMessage;
        set => SetField(ref _successMessage, value);
    }

    public bool IsEmpty => Orders.Count == 0;

    public ICommand PickCommand { get; }

    public PickingViewModel(PickingService pickingService)
    {
        _pickingService = pickingService;

        PickCommand = new RelayCommand(async _ => await PickAsync(), _ => SelectedItem != null);

        _ = LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync(int? reselectOrderId = null)
    {
        var orders = await _pickingService.GetOrdersPendingPickingAsync();

        Orders.Clear();
        foreach (var order in orders) Orders.Add(order);

        OnPropertyChanged(nameof(IsEmpty));

        if (reselectOrderId != null)
        {
            SelectedOrder = Orders.FirstOrDefault(o => o.Id == reselectOrderId);
        }
    }

    private async Task OnOrderChangedAsync()
    {
        PendingItems.Clear();
        SelectedItem = null;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        var order = SelectedOrder;
        if (order == null) return;

        var items = await _pickingService.GetPendingItemsAsync(order.Id);

        if (SelectedOrder?.Id != order.Id) return;

        foreach (var item in items) PendingItems.Add(item);
    }

    private async Task OnSelectedItemChangedAsync()
    {
        Locations.Clear();
        SelectedLocation = null;
        ErrorMessage = string.Empty;

        var order = SelectedOrder;
        var item = SelectedItem;
        if (order == null || item == null)
        {
            Quantity = 0;
            return;
        }

        Quantity = item.Remaining;

        var options = await _pickingService.GetSourceLocationsAsync(order.WarehouseId, item.ProductId);

        if (SelectedItem?.OrderItemId != item.OrderItemId) return;

        foreach (var option in options) Locations.Add(option);

        if (Locations.Count == 0)
        {
            ErrorMessage = $"На складе нет доступного остатка товара «{item.ProductName}».";
        }
    }

    private async Task PickAsync()
    {
        SuccessMessage = string.Empty;
        ErrorMessage = string.Empty;

        var order = SelectedOrder;
        var item = SelectedItem;
        var location = SelectedLocation;
        if (order == null || item == null || location == null) return;

        var quantity = Quantity;

        var result = await _pickingService.PickAsync(item.OrderItemId, location.LocationId, quantity);

        if (!result.Success)
        {
            ErrorMessage = result.Error ?? "Ошибка комплектации.";
            return;
        }

        if (result.OrderCompleted)
        {
            SuccessMessage = $"Позиция собрана. Заказ №{order.OrderNumber} полностью укомплектован!";
            await LoadOrdersAsync();
        }
        else
        {
            SuccessMessage = $"Собрано {quantity} ед. товара «{item.ProductName}» из ячейки {location.Code}.";
            var items = await _pickingService.GetPendingItemsAsync(order.Id);
            PendingItems.Clear();
            foreach (var i in items) PendingItems.Add(i);
            SelectedItem = PendingItems.FirstOrDefault(i => i.OrderItemId == item.OrderItemId);
        }
    }
}