using System.Windows;
using WMS.ViewModels;

namespace WMS.Views;

public partial class WarehouseEmployeeEditWindow : Window
{
    private readonly WarehouseEmployeeEditViewModel _viewModel;

    public WarehouseEmployeeEditWindow(WarehouseEmployeeEditViewModel viewModel)
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