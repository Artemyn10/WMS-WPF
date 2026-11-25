using WMS.Helpers;
using WMS.Models;

namespace WMS.Services;

public class ProductTypeOption
{
    public ProductType Value { get; }
    public string Display { get; }

    public ProductTypeOption(ProductType value, string display)
    {
        Value = value;
        Display = display;
    }

    public static List<ProductTypeOption> All => Enum.GetValues<ProductType>()
        .Select(type => new ProductTypeOption(type, ProductTypeNames.ToRussian(type)))
        .ToList();
}