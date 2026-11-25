using System.Windows;
using System.Windows.Controls;
using WMS.ViewModels;

namespace WMS.Views;

public partial class ProductsView : UserControl
{
    public ProductsView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is ProductsViewModel viewModel)
            {
                var visibility = viewModel.CanSeePrices ? Visibility.Visible : Visibility.Collapsed;
                PurchasePriceColumn.Visibility = visibility;
                AccountingPriceColumn.Visibility = visibility;
            }
        };
    }
}