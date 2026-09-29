using System.Windows;
using WMS.ViewModels;

namespace WMS.Views;

public partial class SupplierEditWindow : Window
{
    private readonly SupplierEditViewModel _viewModel;

    public SupplierEditWindow(SupplierEditViewModel viewModel)
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