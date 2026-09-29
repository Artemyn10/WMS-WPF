using System.Collections.ObjectModel;
using WMS.Services;

namespace WMS.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly DashboardService _dashboardService;

    public ObservableCollection<DashboardCardViewModel> Cards { get; } = new();
    public ObservableCollection<StockMovementRow> RecentMovements { get; } = new();

    public DashboardViewModel(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var stats = await _dashboardService.GetStatsAsync();

        Cards.Clear();
        Cards.Add(new DashboardCardViewModel("Товары", stats.ProductsCount));
        Cards.Add(new DashboardCardViewModel("Склады", stats.WarehousesCount));
        Cards.Add(new DashboardCardViewModel("Ячейки", stats.LocationsCount));
        Cards.Add(new DashboardCardViewModel("Активные заказы", stats.ActiveOrdersCount));
        Cards.Add(new DashboardCardViewModel("Приёмки", stats.ReceiptsCount));
        Cards.Add(new DashboardCardViewModel("Отгрузки", stats.ShipmentsCount));

        var recent = await _dashboardService.GetRecentMovementsAsync();
        RecentMovements.Clear();
        foreach (var item in recent) RecentMovements.Add(item);
    }
}