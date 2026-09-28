using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class CustomerService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public CustomerService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Customer>> GetAllAsync(string? search = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(s));
        }

        return await query.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> AddAsync(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Name))
        {
            return (false, "Имя клиента обязательно для заполнения.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Customers.Add(new Customer
        {
            Name = customer.Name.Trim(),
            Phone = string.IsNullOrWhiteSpace(customer.Phone) ? null : customer.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(customer.Email) ? null : customer.Email.Trim(),
            Address = string.IsNullOrWhiteSpace(customer.Address) ? null : customer.Address.Trim()
        });

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Name))
        {
            return (false, "Имя клиента обязательно для заполнения.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Customers.FindAsync(customer.Id);
        if (existing == null)
        {
            return (false, "Клиент не найден.");
        }

        existing.Name = customer.Name.Trim();
        existing.Phone = string.IsNullOrWhiteSpace(customer.Phone) ? null : customer.Phone.Trim();
        existing.Email = string.IsNullOrWhiteSpace(customer.Email) ? null : customer.Email.Trim();
        existing.Address = string.IsNullOrWhiteSpace(customer.Address) ? null : customer.Address.Trim();

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var customer = await context.Customers.FindAsync(id);
        if (customer == null)
        {
            return (false, "Клиент не найден.");
        }

        context.Customers.Remove(customer);

        try
        {
            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Невозможно удалить клиента: он используется в заказах.");
        }
    }
}