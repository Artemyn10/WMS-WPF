using WMS.Models;

namespace WMS.Helpers;

public static class ProductTypeNames
{
    public static string ToRussian(ProductType type) => type switch
    {
        ProductType.Goods => "Товар",
        ProductType.Material => "Материал",
        ProductType.Equipment => "Оборудование",
        _ => type.ToString()
    };
}