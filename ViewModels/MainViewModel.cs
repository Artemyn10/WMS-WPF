using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WMS.Helpers;
using WMS.Services;

namespace WMS.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly NavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<MenuItemViewModel> MenuItems { get; } = new();

    private ViewModelBase? _currentView;
    public ViewModelBase? CurrentView
    {
        get => _currentView;
        set => SetField(ref _currentView, value);
    }

    public string CurrentUserName => CurrentSession.CurrentUser?.FullName ?? string.Empty;

    public string CurrentUserRole
    {
        get
        {
            var user = CurrentSession.CurrentUser;
            if (user == null)
                return string.Empty;

            var role = UserRoleNames.ToRussian(user.Role);
            return user.Warehouse != null ? $"{role} · {user.Warehouse.Name}" : role;
        }
    }

    public event Action? LogoutRequested;
    public event Action? ExitRequested;

    public ICommand LogoutCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand ChangePasswordCommand { get; }

    public MainViewModel(
        NavigationService navigationService,
        IServiceProvider serviceProvider)
    {
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;

        _navigationService.CurrentViewChanged += viewModel => CurrentView = viewModel;

        BuildMenu();

        LogoutCommand = new RelayCommand(_ => LogoutRequested?.Invoke());
        ExitCommand = new RelayCommand(_ => ExitRequested?.Invoke());
        ChangePasswordCommand = new RelayCommand(_ => OpenChangePasswordDialog());

        MenuItems.FirstOrDefault()?.NavigateCommand.Execute(null);
    }

    private void OpenChangePasswordDialog()
    {
        var window = _serviceProvider.GetRequiredService<Views.ChangePasswordWindow>();
        window.Owner = System.Windows.Application.Current.MainWindow;
        window.ShowDialog();
    }

    private void BuildMenu()
    {
        AddMenuItem("Главная", AppSection.Dashboard,
            () => _serviceProvider.GetRequiredService<DashboardViewModel>());
        AddMenuItem("Товары", AppSection.Products,
            () => _serviceProvider.GetRequiredService<ProductsViewModel>());
        AddMenuItem("Категории", AppSection.Categories,
            () => _serviceProvider.GetRequiredService<CategoriesViewModel>());
        AddMenuItem("Поставщики", AppSection.Suppliers,
            () => _serviceProvider.GetRequiredService<SuppliersViewModel>());
        AddMenuItem("Клиенты", AppSection.Customers,
            () => _serviceProvider.GetRequiredService<CustomersViewModel>());
        AddMenuItem("Склады", AppSection.Warehouses,
            () => _serviceProvider.GetRequiredService<WarehousesViewModel>());
        AddMenuItem("Ячейки", AppSection.StorageLocations,
            () => _serviceProvider.GetRequiredService<StorageLocationsViewModel>());
        AddMenuItem("Сотрудники складов", AppSection.WarehouseEmployees,
            () => _serviceProvider.GetRequiredService<WarehouseEmployeesViewModel>());
        AddMenuItem("Приёмка", AppSection.Receipts,
            () => _serviceProvider.GetRequiredService<ReceiptsViewModel>());
        AddMenuItem("Размещение", AppSection.Placement,
            () => _serviceProvider.GetRequiredService<PlacementViewModel>());
        AddMenuItem("Перемещение внутри склада", AppSection.Movement,
            () => _serviceProvider.GetRequiredService<MovementViewModel>());
        AddMenuItem("Перемещение между складами", AppSection.InterWarehouseTransfers,
            () => _serviceProvider.GetRequiredService<WarehouseTransfersViewModel>());
        AddMenuItem("Остатки", AppSection.Inventory,
            () => _serviceProvider.GetRequiredService<InventoryViewModel>());
        AddMenuItem("Сводка по складам", AppSection.WarehouseSummary,
            () => _serviceProvider.GetRequiredService<WarehouseSummaryViewModel>());
        AddMenuItem("Заказы", AppSection.Orders,
            () => _serviceProvider.GetRequiredService<OrdersViewModel>());
        AddMenuItem("Заказы поставщикам", AppSection.PurchaseOrders,
            () => _serviceProvider.GetRequiredService<PurchaseOrdersViewModel>());
        AddMenuItem("Комплектация", AppSection.Picking,
            () => _serviceProvider.GetRequiredService<PickingViewModel>());
        AddMenuItem("Отгрузка", AppSection.Shipment,
            () => _serviceProvider.GetRequiredService<ShipmentViewModel>());
        AddMenuItem("Списание", AppSection.WriteOff,
            () => _serviceProvider.GetRequiredService<WriteOffsViewModel>());
        AddMenuItem("Инвентаризация", AppSection.StockTaking,
            () => _serviceProvider.GetRequiredService<StockTakingsViewModel>());
        AddMenuItem("История операций", AppSection.History,
            () => _serviceProvider.GetRequiredService<StockMovementHistoryViewModel>());
        AddMenuItem("Пользователи", AppSection.Users,
            () => _serviceProvider.GetRequiredService<UsersViewModel>());
    }

    private void AddMenuItem(
        string title,
        AppSection section,
        Func<ViewModelBase> viewModelFactory)
    {
        if (!CurrentSession.CanAccess(section))
            return;

        MenuItemViewModel? menuItem = null;
        menuItem = new MenuItemViewModel(title, () =>
        {
            _navigationService.NavigateTo(viewModelFactory());
            SelectMenuItem(menuItem!);
        });

        MenuItems.Add(menuItem);
    }

    private void SelectMenuItem(MenuItemViewModel selected)
    {
        foreach (var item in MenuItems)
            item.IsSelected = item == selected;
    }
}