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

        var warehouses = context.Warehouses.AsQueryable();
        var locations = context.StorageLocations.AsQueryable();
        var orders = context.Orders.AsQueryable();
        var receipts = context.Receipts.AsQueryable();
        var shipments = context.Shipments.AsQueryable();

        if (CurrentSession.WarehouseScopeId is int scopeId)
        {
            warehouses = warehouses.Where(w => w.Id == scopeId);
            locations = locations.Where(l => l.WarehouseId == scopeId);
            orders = orders.Where(o => o.WarehouseId == scopeId);
            receipts = receipts.Where(r => r.WarehouseId == scopeId);
            shipments = shipments.Where(s => s.Order!.WarehouseId == scopeId);
        }

        return new DashboardStats
        {
            ProductsCount = await context.Products.CountAsync(),
            WarehousesCount = await warehouses.CountAsync(),
            LocationsCount = await locations.CountAsync(),
            ActiveOrdersCount = await orders.CountAsync(o =>
                o.Status == OrderStatus.New || o.Status == OrderStatus.Assembling || o.Status == OrderStatus.Assembled),
            ReceiptsCount = await receipts.CountAsync(),
            ShipmentsCount = await shipments.CountAsync()
        };
    }

    public async Task<List<StockMovementRow>> GetRecentMovementsAsync(int count = 10)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.StockMovements
            .Include(m => m.Product)
            .Include(m => m.FromLocation)
            .Include(m => m.ToLocation)
            .AsQueryable();

        if (CurrentSession.WarehouseScopeId is int scopeId)
        {
            query = query.Where(m => m.WarehouseId == scopeId);
        }

        var movements = await query
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