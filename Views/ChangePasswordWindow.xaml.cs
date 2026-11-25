using System.Windows;
using WMS.ViewModels;

namespace WMS.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly ChangePasswordViewModel _viewModel;

    public ChangePasswordWindow(ChangePasswordViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.RequestClose += result =>
        {
            if (result == true)
            {
                MessageBox.Show(this, "Пароль успешно изменён.", "Смена пароля",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            DialogResult = result;
            Close();
        };
    }

    private void CurrentPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.CurrentPassword = CurrentPasswordBox.Password;
    }

    private void NewPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.NewPassword = NewPasswordBox.Password;
    }

    private void ConfirmPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
    }
}