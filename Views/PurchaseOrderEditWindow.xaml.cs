using System.Windows;
using WMS.ViewModels;

namespace WMS.Views;

public partial class PurchaseOrderEditWindow : Window
{
    private readonly PurchaseOrderEditViewModel _viewModel;

    public PurchaseOrderEditWindow(PurchaseOrderEditViewModel viewModel)
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