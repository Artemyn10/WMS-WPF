using System.Windows;
using WMS.ViewModels;

namespace WMS.Views;

public partial class UserEditWindow : Window
{
    private readonly UserEditViewModel _viewModel;

    public UserEditWindow(UserEditViewModel viewModel)
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

    // Перед сохранением синхронизируем пароль из PasswordBox в ViewModel
    private void PasswordBoxControl_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.Password = PasswordBoxControl.Password;
    }
}