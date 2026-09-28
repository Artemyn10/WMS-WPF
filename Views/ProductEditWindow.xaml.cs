using System.Windows;
using WMS.ViewModels;

namespace WMS.Views;

public partial class ProductEditWindow : Window
{
    private readonly ProductEditViewModel _viewModel;

    public ProductEditWindow(ProductEditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.RequestClose += result =>
        {
            DialogResult = result;
            Close();
        };
    }
}