using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class InventoryService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public InventoryService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Inventory>> GetAllAsync(string? productSearch = null, string? locationSearch = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Inventories
            .Include(i => i.Product)
            .Include(i => i.StorageLocation)
                .ThenInclude(l => l!.Warehouse)
            .Where(i => i.Quantity > 0)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(productSearch))
        {
            var s = productSearch.ToLower();
            query = query.Where(i => i.Product!.Name.ToLower().Contains(s) || i.Product!.Article.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(locationSearch))
        {
            var s = locationSearch.ToLower();
            query = query.Where(i => i.StorageLocation!.Code.ToLower().Contains(s));
        }

        return await query
            .OrderBy(i => i.Product!.Name)
            .ThenBy(i => i.StorageLocation!.Code)
            .ToListAsync();
    }

    /// Получить текущий остаток товара в конкретной ячейке (0, если записи нет)
    public async Task<int> GetQuantityAsync(WmsDbContext context, int productId, int locationId)
    {
        var inventory = await context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.StorageLocationId == locationId);
        return inventory?.Quantity ?? 0;
    }

    /// Увеличить остаток товара в ячейке (создаёт запись, если её ещё нет). Использует переданный context - для работы внутри общей транзакции операции.
    public async Task IncreaseAsync(WmsDbContext context, int productId, int locationId, int quantity)
    {
        var inventory = await context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.StorageLocationId == locationId);

        if (inventory == null)
        {
            context.Inventories.Add(new Inventory
            {
                ProductId = productId,
                StorageLocationId = locationId,
                Quantity = quantity
            });
        }
        else
        {
            inventory.Quantity += quantity;
        }
    }

    /// Уменьшить остаток товара в ячейке. Бросает исключение, если товара недостаточно - вызывающий код должен проверить количество заранее через GetQuantityAsync.
    public async Task DecreaseAsync(WmsDbContext context, int productId, int locationId, int quantity)
    {
        var inventory = await context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.StorageLocationId == locationId);

        if (inventory == null || inventory.Quantity < quantity)
        {
            throw new InvalidOperationException("Недостаточно товара в ячейке для списания.");
        }

        inventory.Quantity -= quantity;
    }
}