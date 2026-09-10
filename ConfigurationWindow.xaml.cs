using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SNAPPY.Models;
using SNAPPY.Services;

namespace SNAPPY;

public partial class ConfigurationWindow : Window
{
    private readonly List<OutdoorStation> _stations;
    private readonly IndoorConfiguration _indoor;

    public bool ConfigurationSaved { get; private set; }

    public ConfigurationWindow()
    {
        InitializeComponent();

        _stations = ConfigurationService.Load();
        _indoor = ConfigurationService.LoadIndoor();

        IndoorNameBox.Text = _indoor.IndoorName;
        RoomNumberBox.Text = _indoor.RoomNumber;
        ExtensionNameBox.Text = _indoor.ExtensionName;
        ExtensionNumberBox.Text = _indoor.ExtensionNumber;
        DefaultAudioChannelBox.Text = _indoor.DefaultTwoWayAudioChannel.ToString();
        AutoSelectCheckBox.IsChecked = _indoor.AutoSelectCallingOutdoor;
        AutoAudioCheckBox.IsChecked = _indoor.EnableTwoWayAudioCommandOnAnswer;
        DoorUnlockCheckBox.IsChecked = _indoor.EnableDoorUnlock;

        StationsItems.ItemsSource = _stations;
        FilePathText.Text = $"Outdoor config: {ConfigurationService.GetFilePath()}";
        IndoorFilePathText.Text = $"Indoor config: {ConfigurationService.GetIndoorFilePath()}";
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox box && box.Tag is OutdoorStation station)
            station.Password = box.Password;
    }

    private async void TestAllButton_Click(object sender, RoutedEventArgs e)
    {
        var success = 0;

        foreach (var station in _stations)
        {
            using var service = new DeviceIsapiService(
                station.IpAddress,
                station.HttpPort,
                station.Username,
                station.Password);

            if (await service.TestConnectionAsync())
                success++;
        }

        MessageBox.Show(
            $"Connection test completed. Online: {success}/{_stations.Count}.\n\n" +
            "Room mapping and two-way-audio settings are local SNAPPY configuration values.",
            "SNAPPY",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(DefaultAudioChannelBox.Text, out var defaultAudioChannel) || defaultAudioChannel < 1)
            defaultAudioChannel = 1;

        _indoor.IndoorName = string.IsNullOrWhiteSpace(IndoorNameBox.Text) ? "MAIN INDOOR" : IndoorNameBox.Text.Trim();
        _indoor.RoomNumber = string.IsNullOrWhiteSpace(RoomNumberBox.Text) ? "101" : RoomNumberBox.Text.Trim();
        _indoor.ExtensionName = string.IsNullOrWhiteSpace(ExtensionNameBox.Text) ? "INDOOR EXTENSION 01" : ExtensionNameBox.Text.Trim();
        _indoor.ExtensionNumber = string.IsNullOrWhiteSpace(ExtensionNumberBox.Text) ? "1" : ExtensionNumberBox.Text.Trim();
        _indoor.DefaultTwoWayAudioChannel = defaultAudioChannel;
        _indoor.AutoSelectCallingOutdoor = AutoSelectCheckBox.IsChecked == true;
        _indoor.EnableTwoWayAudioCommandOnAnswer = AutoAudioCheckBox.IsChecked == true;
        _indoor.EnableDoorUnlock = DoorUnlockCheckBox.IsChecked == true;

        foreach (var station in _stations)
        {
            if (string.IsNullOrWhiteSpace(station.RoomNumber))
                station.RoomNumber = _indoor.RoomNumber;

            if (string.IsNullOrWhiteSpace(station.MainIndoorName))
                station.MainIndoorName = _indoor.IndoorName;

            if (string.IsNullOrWhiteSpace(station.ExtensionNumber))
                station.ExtensionNumber = _indoor.ExtensionNumber;

            if (station.TwoWayAudioChannel < 1)
                station.TwoWayAudioChannel = _indoor.DefaultTwoWayAudioChannel;
        }

        ConfigurationService.Save(_stations);
        ConfigurationService.SaveIndoor(_indoor);

        ConfigurationSaved = true;
        DialogResult = true;
        Close();
    }
}
