using System.Windows;
using WMS.ViewModels;

namespace WMS.Views;

public partial class StorageLocationEditWindow : Window
{
    private readonly StorageLocationEditViewModel _viewModel;

    public StorageLocationEditWindow(StorageLocationEditViewModel viewModel)
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