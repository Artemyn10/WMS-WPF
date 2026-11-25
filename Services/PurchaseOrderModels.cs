namespace WMS.Services;

// Строка, которую вводят при создании заказа поставщику
public class PurchaseOrderItemInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

// Сколько ещё осталось получить по товару заказа - для заполнения приёмки
public class PurchaseOrderLine
{
    public int ProductId { get; set; }
    public int Remaining { get; set; }
}

// Вариант заказа для выпадающего списка в окне приёмки
public class PurchaseOrderOption
{
    public int Id { get; set; }
    public string Display { get; set; } = string.Empty;

    // Пункт "приёмка без заказа"
    public static PurchaseOrderOption None() => new() { Id = 0, Display = "— без заказа —" };
}