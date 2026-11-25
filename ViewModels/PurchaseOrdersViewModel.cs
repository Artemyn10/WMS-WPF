using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;
using WMS.Views;

namespace WMS.ViewModels;

public class PurchaseOrdersViewModel : ViewModelBase
{
    private readonly PurchaseOrderService _purchaseOrderService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<PurchaseOrder> PurchaseOrders { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) _ = LoadAsync(); }
    }

    private PurchaseOrder? _selectedOrder;
    public PurchaseOrder? SelectedOrder
    {
        get => _selectedOrder;
        set => SetField(ref _selectedOrder, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

    public ICommand AddCommand { get; }
    public ICommand CancelOrderCommand { get; }

    public PurchaseOrdersViewModel(PurchaseOrderService purchaseOrderService, IServiceProvider serviceProvider)
    {
        _purchaseOrderService = purchaseOrderService;
        _serviceProvider = serviceProvider;

        AddCommand = new RelayCommand(async _ => await OpenCreateDialogAsync());
        CancelOrderCommand = new RelayCommand(
            async _ => await CancelOrderAsync(),
            _ => SelectedOrder?.Status == PurchaseOrderStatus.New);

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _purchaseOrderService.GetAllAsync(SearchText);
        PurchaseOrders.Clear();
        foreach (var item in items) PurchaseOrders.Add(item);
    }

    private async Task OpenCreateDialogAsync()
    {
        StatusMessage = string.Empty;

        var window = _serviceProvider.GetRequiredService<PurchaseOrderEditWindow>();
        var viewModel = (PurchaseOrderEditViewModel)window.DataContext;

        await viewModel.InitializeAsync();

        if (window.ShowDialog() == true)
        {
            await LoadAsync();
        }
    }

    private async Task CancelOrderAsync()
    {
        StatusMessage = string.Empty;

        var order = SelectedOrder;
        if (order == null) return;

        var answer = MessageBox.Show(
            $"Отменить заказ {order.OrderNumber}? Отменённый заказ нельзя вернуть в работу.",
            "Отмена заказа",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes) return;

        var result = await _purchaseOrderService.CancelAsync(order.Id);
        if (!result.Success)
        {
            StatusMessage = result.Error ?? "Не удалось отменить заказ.";
            return;
        }

        await LoadAsync();
    }
}