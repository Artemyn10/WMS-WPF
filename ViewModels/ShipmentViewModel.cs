using System.Collections.ObjectModel;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class ShipmentViewModel : ViewModelBase
{
    private readonly ShipmentService _shipmentService;

    public ObservableCollection<Order> PendingOrders { get; } = new();
    public ObservableCollection<Shipment> Shipments { get; } = new();

    private Order? _selectedOrder;
    public Order? SelectedOrder
    {
        get => _selectedOrder;
        set => SetField(ref _selectedOrder, value);
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

    public bool IsEmpty => PendingOrders.Count == 0;

    public ICommand ShipCommand { get; }

    public ShipmentViewModel(ShipmentService shipmentService)
    {
        _shipmentService = shipmentService;

        ShipCommand = new RelayCommand(async _ => await ShipAsync(), _ => SelectedOrder != null);

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var pending = await _shipmentService.GetOrdersPendingShipmentAsync();
        PendingOrders.Clear();
        foreach (var order in pending) PendingOrders.Add(order);
        OnPropertyChanged(nameof(IsEmpty));

        var shipments = await _shipmentService.GetShipmentsAsync();
        Shipments.Clear();
        foreach (var shipment in shipments) Shipments.Add(shipment);
    }

    private async Task ShipAsync()
    {
        SuccessMessage = string.Empty;
        ErrorMessage = string.Empty;

        var order = SelectedOrder;
        if (order == null) return;

        var result = await _shipmentService.ShipAsync(order.Id);

        if (!result.Success)
        {
            ErrorMessage = result.Error ?? "Ошибка отгрузки.";
            return;
        }

        SuccessMessage = $"Заказ №{order.OrderNumber} отгружен.";
        SelectedOrder = null;
        await LoadAsync();
    }
}