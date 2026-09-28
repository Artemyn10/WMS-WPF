using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class SupplierService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public SupplierService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Supplier>> GetAllAsync(string? search = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Suppliers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(s));
        }

        return await query.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> AddAsync(Supplier supplier)
    {
        if (string.IsNullOrWhiteSpace(supplier.Name))
        {
            return (false, "Название поставщика обязательно для заполнения.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Suppliers.Add(new Supplier
        {
            Name = supplier.Name.Trim(),
            Phone = string.IsNullOrWhiteSpace(supplier.Phone) ? null : supplier.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(supplier.Email) ? null : supplier.Email.Trim(),
            Address = string.IsNullOrWhiteSpace(supplier.Address) ? null : supplier.Address.Trim()
        });

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Supplier supplier)
    {
        if (string.IsNullOrWhiteSpace(supplier.Name))
        {
            return (false, "Название поставщика обязательно для заполнения.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Suppliers.FindAsync(supplier.Id);
        if (existing == null)
        {
            return (false, "Поставщик не найден.");
        }

        existing.Name = supplier.Name.Trim();
        existing.Phone = string.IsNullOrWhiteSpace(supplier.Phone) ? null : supplier.Phone.Trim();
        existing.Email = string.IsNullOrWhiteSpace(supplier.Email) ? null : supplier.Email.Trim();
        existing.Address = string.IsNullOrWhiteSpace(supplier.Address) ? null : supplier.Address.Trim();

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var supplier = await context.Suppliers.FindAsync(id);
        if (supplier == null)
        {
            return (false, "Поставщик не найден.");
        }

        context.Suppliers.Remove(supplier);

        try
        {
            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Невозможно удалить поставщика: он используется в документах приёмки.");
        }
    }
}