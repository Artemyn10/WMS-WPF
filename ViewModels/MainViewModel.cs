using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
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
    public string CurrentUserRole => CurrentSession.CurrentUser?.Role == Models.UserRole.Administrator
        ? "Администратор"
        : "Кладовщик";

   

    public MainViewModel(NavigationService navigationService, IServiceProvider serviceProvider)
    {
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;

        _navigationService.CurrentViewChanged += vm => CurrentView = vm;

        BuildMenu();

        var firstItem = MenuItems.FirstOrDefault();
        firstItem?.NavigateCommand.Execute(null);
        if (firstItem != null)
        {
            SelectMenuItem(firstItem);
        }
    }

    private void BuildMenu()
    {
        var isAdmin = CurrentSession.IsAdministrator;

        AddMenuItem("Главная", adminOnly: false, () => _serviceProvider.GetRequiredService<DashboardViewModel>());
        AddMenuItem("Товары", adminOnly: false, () => _serviceProvider.GetRequiredService<ProductsViewModel>());
        AddMenuItem("Категории", adminOnly: true, () => _serviceProvider.GetRequiredService<CategoriesViewModel>());
        AddMenuItem("Поставщики", adminOnly: true, () => _serviceProvider.GetRequiredService<SuppliersViewModel>());
        AddMenuItem("Клиенты", adminOnly: true, () => _serviceProvider.GetRequiredService<CustomersViewModel>());
        AddMenuItem("Склады", adminOnly: true, () => _serviceProvider.GetRequiredService<WarehousesViewModel>());
        AddMenuItem("Ячейки", adminOnly: true, () => _serviceProvider.GetRequiredService<StorageLocationsViewModel>());
        AddMenuItem("Приёмка", adminOnly: false, () => _serviceProvider.GetRequiredService<ReceiptsViewModel>());
        AddMenuItem("Размещение", adminOnly: false, () => _serviceProvider.GetRequiredService<PlacementViewModel>());
        AddMenuItem("Перемещение", adminOnly: false, () => _serviceProvider.GetRequiredService<MovementViewModel>());
        AddMenuItem("Остатки", adminOnly: false, () => _serviceProvider.GetRequiredService<InventoryViewModel>());
        AddMenuItem("Заказы", adminOnly: false, () => _serviceProvider.GetRequiredService<OrdersViewModel>());
        AddMenuItem("Комплектация", adminOnly: false, () => _serviceProvider.GetRequiredService<PickingViewModel>());
        AddMenuItem("Отгрузка", adminOnly: false, () => _serviceProvider.GetRequiredService<ShipmentViewModel>());
        AddMenuItem("История операций", adminOnly: false, () => _serviceProvider.GetRequiredService<StockMovementHistoryViewModel>());
        AddMenuItem("Пользователи", adminOnly: true, () => _serviceProvider.GetRequiredService<UsersViewModel>());

        if (!isAdmin)
        {
            var visibleItems = MenuItems.Where(m => !m.AdminOnly).ToList();
            MenuItems.Clear();
            foreach (var item in visibleItems)
            {
                MenuItems.Add(item);
            }
        }
    }

    private void AddMenuItem(string title, bool adminOnly, Func<ViewModelBase> viewModelFactory)
    {
        MenuItemViewModel? menuItem = null;
        menuItem = new MenuItemViewModel(title, adminOnly, () =>
        {
            _navigationService.NavigateTo(viewModelFactory());
            SelectMenuItem(menuItem!);
        });
        MenuItems.Add(menuItem);
    }

    private void SelectMenuItem(MenuItemViewModel selected)
    {
        foreach (var item in MenuItems)
        {
            item.IsSelected = item == selected;
        }
    }
}