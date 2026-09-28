using System.Collections.ObjectModel;
using WMS.Models;

namespace WMS.ViewModels;

public class OrderItemRowViewModel : ViewModelBase
{
    public ObservableCollection<Product> AvailableProducts { get; }

    private Product? _selectedProduct;
    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set => SetField(ref _selectedProduct, value);
    }

    private int _quantity = 1;
    public int Quantity
    {
        get => _quantity;
        set => SetField(ref _quantity, value);
    }

    public OrderItemRowViewModel(ObservableCollection<Product> availableProducts)
    {
        AvailableProducts = availableProducts;
    }
}