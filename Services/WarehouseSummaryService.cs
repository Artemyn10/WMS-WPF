using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class WarehouseSummaryService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public WarehouseSummaryService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<WarehouseSummaryRow>> GetSummaryAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var warehousesQuery = context.Warehouses
            .Include(w => w.Employees)
            .AsQueryable();

        if (CurrentSession.WarehouseScopeId is int scopeId)
        {
            warehousesQuery = warehousesQuery.Where(w => w.Id == scopeId);
        }

        var warehouses = await warehousesQuery.OrderBy(w => w.Name).ToListAsync();

        // Ячейки: количество и общая вместимость по каждому складу
        var locationStats = await context.StorageLocations
            .GroupBy(l => l.WarehouseId)
            .Select(g => new { WarehouseId = g.Key, Count = g.Count(), Capacity = g.Sum(l => l.Capacity) })
            .ToDictionaryAsync(x => x.WarehouseId);

        // Сколько единиц товара сейчас лежит на каждом складе
        var storedStats = await context.Inventories
            .GroupBy(i => i.StorageLocation!.WarehouseId)
            .Select(g => new { WarehouseId = g.Key, Total = g.Sum(i => i.Quantity) })
            .ToDictionaryAsync(x => x.WarehouseId, x => x.Total);

        var receiptStats = await context.Receipts
            .GroupBy(r => r.WarehouseId)
            .Select(g => new { WarehouseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.WarehouseId, x => x.Count);

        // Активные заказы - все, которые ещё не отгружены
        var orderStats = await context.Orders
            .GroupBy(o => o.WarehouseId)
            .Select(g => new
            {
                WarehouseId = g.Key,
                Total = g.Count(),
                Active = g.Count(o => o.Status != OrderStatus.Shipped)
            })
            .ToDictionaryAsync(x => x.WarehouseId);

        // У отгрузки склада нет, склад берём из её заказа
        var shipmentStats = await context.Shipments
            .GroupBy(s => s.Order!.WarehouseId)
            .Select(g => new { WarehouseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.WarehouseId, x => x.Count);

        return warehouses.Select(w =>
        {
            locationStats.TryGetValue(w.Id, out var locations);
            orderStats.TryGetValue(w.Id, out var orders);

            return new WarehouseSummaryRow
            {
                WarehouseId = w.Id,
                Name = w.Name,
                Address = w.Address ?? "—",
                Area = w.Area,
                Purpose = w.Purpose ?? "—",
                WorkingHours = w.WorkingHours ?? "—",
                Phone = w.Phone ?? "—",
                Email = w.Email ?? "—",
                ResponsibleNames = string.IsNullOrEmpty(w.ResponsibleNames) ? "—" : w.ResponsibleNames,
                EmployeesCount = w.Employees.Count,

                LocationsCount = locations?.Count ?? 0,
                Capacity = locations?.Capacity ?? 0,
                Stored = storedStats.GetValueOrDefault(w.Id),

                ReceiptsCount = receiptStats.GetValueOrDefault(w.Id),
                OrdersCount = orders?.Total ?? 0,
                ActiveOrdersCount = orders?.Active ?? 0,
                ShipmentsCount = shipmentStats.GetValueOrDefault(w.Id)
            };
        }).ToList();
    }
}