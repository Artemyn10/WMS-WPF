using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;
using WMS.Views;
using WMS.ViewModels;

namespace WMS.ViewModels;

public class OrdersViewModel : ViewModelBase
{
    private readonly OrderService _orderService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<Order> Orders { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) _ = LoadAsync(); }
    }

    private Order? _selectedOrder;
    public Order? SelectedOrder
    {
        get => _selectedOrder;
        set => SetField(ref _selectedOrder, value);
    }

    public ICommand AddCommand { get; }

    public OrdersViewModel(OrderService orderService, IServiceProvider serviceProvider)
    {
        _orderService = orderService;
        _serviceProvider = serviceProvider;

        AddCommand = new RelayCommand(async _ => await OpenCreateDialogAsync());

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _orderService.GetAllAsync(SearchText);
        Orders.Clear();
        foreach (var item in items) Orders.Add(item);
    }

    private async Task OpenCreateDialogAsync()
    {
        var window = _serviceProvider.GetRequiredService<OrderEditWindow>();
        var viewModel = (OrderEditViewModel)window.DataContext;

        await viewModel.InitializeAsync();

        if (window.ShowDialog() == true)
        {
            await LoadAsync();
        }
    }
}