using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;
using WMS.Views;

namespace WMS.ViewModels;

public class StorageLocationsViewModel : ViewModelBase
{
    private readonly StorageLocationService _locationService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<StorageLocation> Locations { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) _ = LoadAsync(); }
    }

    private StorageLocation? _selectedLocation;
    public StorageLocation? SelectedLocation
    {
        get => _selectedLocation;
        set => SetField(ref _selectedLocation, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public StorageLocationsViewModel(StorageLocationService locationService, IServiceProvider serviceProvider)
    {
        _locationService = locationService;
        _serviceProvider = serviceProvider;

        AddCommand = new RelayCommand(async _ => await OpenEditDialogAsync(null));
        EditCommand = new RelayCommand(async _ => await OpenEditDialogAsync(SelectedLocation), _ => SelectedLocation != null);
        DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedLocation != null);

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _locationService.GetAllAsync(SearchText);
        Locations.Clear();
        foreach (var item in items) Locations.Add(item);
    }

    private async Task OpenEditDialogAsync(StorageLocation? location)
    {
        var window = _serviceProvider.GetRequiredService<StorageLocationEditWindow>();
        var viewModel = (StorageLocationEditViewModel)window.DataContext;

        if (location == null) await viewModel.InitializeForAddAsync();
        else await viewModel.InitializeForEditAsync(location);

        if (window.ShowDialog() == true)
        {
            await LoadAsync();
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedLocation == null) return;

        var result = await _locationService.DeleteAsync(SelectedLocation.Id);
        StatusMessage = result.Success ? string.Empty : (result.Error ?? string.Empty);

        if (result.Success) await LoadAsync();
    }
}