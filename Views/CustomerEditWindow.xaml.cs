using System.Windows;
using WMS.ViewModels;

namespace WMS.Views;

public partial class CustomerEditWindow : Window
{
    private readonly CustomerEditViewModel _viewModel;

    public CustomerEditWindow(CustomerEditViewModel viewModel)
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