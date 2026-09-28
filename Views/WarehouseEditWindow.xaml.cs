using System.Windows;
using WMS.ViewModels;

namespace WMS.Views;

public partial class WarehouseEditWindow : Window
{
    private readonly WarehouseEditViewModel _viewModel;

    public WarehouseEditWindow(WarehouseEditViewModel viewModel)
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