using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SNAPPY.Models;
using SNAPPY.Services;

namespace SNAPPY;

public partial class OutdoorStationListWindow : Window
{
    private readonly ObservableCollection<OutdoorStation> _stations;

    private readonly ConfigurationService _configurationService;

    public OutdoorStationListWindow(
        ObservableCollection<OutdoorStation> stations,
        ConfigurationService configurationService)
    {
        InitializeComponent();

        _stations = stations;

        _configurationService =
            configurationService;

        StationList.ItemsSource =
            _stations;
    }

    private async void AddButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var station =
            new OutdoorStation
            {
                Name =
                    $"Outdoor {_stations.Count + 1:00}",

                Port = 8000,

                Enabled = true
            };

        var window =
            new OutdoorConfigWindow(station)
            {
                Owner = this
            };

        if (window.ShowDialog() == true)
        {
            _stations.Add(station);

            await SaveAsync();
        }
    }

    private async void EditButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (StationList.SelectedItem
            is not OutdoorStation station)
        {
            MessageBox.Show(
                "Please select an outdoor station.",
                "SNAPPY",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var window =
            new OutdoorConfigWindow(station)
            {
                Owner = this
            };

        if (window.ShowDialog() == true)
        {
            StationList.Items.Refresh();

            await SaveAsync();
        }
    }

    private async void DeleteButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (StationList.SelectedItem
            is not OutdoorStation station)
        {
            MessageBox.Show(
                "Please select an outdoor station.",
                "SNAPPY",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        MessageBoxResult result =
            MessageBox.Show(
                $"Delete '{station.DisplayName}'?",
                "SNAPPY",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        _stations.Remove(station);

        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        await _configurationService
            .SaveStationsAsync(_stations);

        MessageBox.Show(
            "Outdoor station configuration saved.",
            "SNAPPY",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void StationList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        // Selection is intentionally handled by
        // the Edit/Delete buttons.
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
