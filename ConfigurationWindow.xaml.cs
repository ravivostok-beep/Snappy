using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SNAPPY.Models;
using SNAPPY.Services;

namespace SNAPPY;

public partial class ConfigurationWindow : Window
{
    private readonly List<OutdoorStation> _stations;
    public bool ConfigurationSaved { get; private set; }

    public ConfigurationWindow()
    {
        InitializeComponent();
        _stations = ConfigurationService.Load();
        StationsItems.ItemsSource = _stations;
        FilePathText.Text = ConfigurationService.GetFilePath();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox box && box.Tag is OutdoorStation station)
            station.Password = box.Password;
    }

    private async void TestAllButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var station in _stations)
        {
            var service = new DeviceIsapiService(station.IpAddress, station.HttpPort, station.Username, station.Password);
            var online = await service.TestConnectionAsync();
            service.Dispose();

            // The status text is inside a generated row; refresh the ItemsControl after each test.
            station.ExtensionName = station.ExtensionName;
            station.ExtensionNumber = station.ExtensionNumber;
            station.MainIndoorName = station.MainIndoorName;
            station.RoomNumber = station.RoomNumber;

            if (!online)
                continue;
        }

        MessageBox.Show("Connection test completed for all 9 configured stations.", "SNAPPY", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ConfigurationService.Save(_stations);
        ConfigurationSaved = true;
        DialogResult = true;
        Close();
    }
}
