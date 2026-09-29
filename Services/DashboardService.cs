using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Helpers;
using WMS.Models;

namespace WMS.Services;

public class DashboardService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public DashboardService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<DashboardStats> GetStatsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return new DashboardStats
        {
            ProductsCount = await context.Products.CountAsync(),
            WarehousesCount = await context.Warehouses.CountAsync(),
            LocationsCount = await context.StorageLocations.CountAsync(),
            ActiveOrdersCount = await context.Orders.CountAsync(o =>
                o.Status == OrderStatus.New || o.Status == OrderStatus.Assembling || o.Status == OrderStatus.Assembled),
            ReceiptsCount = await context.Receipts.CountAsync(),
            ShipmentsCount = await context.Shipments.CountAsync()
        };
    }

    public async Task<List<StockMovementRow>> GetRecentMovementsAsync(int count = 10)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var movements = await context.StockMovements
            .Include(m => m.Product)
            .Include(m => m.FromLocation)
            .Include(m => m.ToLocation)
            .OrderByDescending(m => m.Date)
            .Take(count)
            .ToListAsync();

        return movements.Select(m => new StockMovementRow
        {
            Date = m.Date,
            OperationTypeDisplay = OperationTypeDisplay.ToRussian(m.OperationType),
            ProductName = m.Product!.Name,
            Quantity = m.Quantity,
            FromLocationCode = m.FromLocation?.Code ?? "—",
            ToLocationCode = m.ToLocation?.Code ?? "—"
        }).ToList();
    }
}