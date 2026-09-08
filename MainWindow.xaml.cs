using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SNAPPY.Models;
using SNAPPY.Services;

namespace SNAPPY;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<OutdoorStation> _stations = new();
    private readonly ConfigurationService _configurationService;
    private readonly HikvisionTalkService _talkService;

    private OutdoorStation? _selectedStation;

    public MainWindow()
    {
        InitializeComponent();

        _configurationService = new ConfigurationService();
        _talkService = new HikvisionTalkService();

        OutdoorList.ItemsSource = _stations;

        _talkService.StateChanged += TalkService_StateChanged;

        SetButtonsEnabled(false);
    }


    private async void Window_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "LOADING";
            FooterText.Text =
                "Loading outdoor station configuration...";

            await LoadStationsAsync();

            StatusText.Text = "READY";

            FooterText.Text =
                $"SNAPPY started successfully. {_stations.Count} outdoor stations loaded.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "ERROR";

            FooterText.Text =
                "Unable to load configuration.";

            MessageBox.Show(
                ex.Message,
                "SNAPPY",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


    private async Task LoadStationsAsync()
    {
        var stations =
            await _configurationService.LoadStationsAsync();

        _stations.Clear();

        foreach (OutdoorStation station in stations)
        {
            _stations.Add(station);
        }
    }


    private async void OutdoorConfigButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            var configWindow =
                new OutdoorStationListWindow(
                    _stations,
                    _configurationService)
                {
                    Owner = this
                };

            bool? result =
                configWindow.ShowDialog();

            if (result == true)
            {
                await LoadStationsAsync();

                _selectedStation = null;

                SelectedStationText.Text =
                    "No station selected";

                SelectedStationInfoText.Text =
                    "No device selected";

                LiveVideoText.Text =
                    "LIVE VIDEO";

                SetButtonsEnabled(false);

                FooterText.Text =
                    "Outdoor station configuration updated.";
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }


    private void OutdoorList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        _selectedStation =
            OutdoorList.SelectedItem as OutdoorStation;

        if (_selectedStation == null)
        {
            SelectedStationText.Text =
                "No station selected";

            SelectedStationInfoText.Text =
                "No device selected";

            LiveVideoText.Text =
                "LIVE VIDEO";

            SetButtonsEnabled(false);

            return;
        }


        SelectedStationText.Text =
            $"{_selectedStation.DisplayName}  |  {_selectedStation.IpAddress}";


        SelectedStationInfoText.Text =
            $"{_selectedStation.DisplayName}\n" +
            $"IP: {_selectedStation.IpAddress}\n" +
            $"ISAPI Port: {_selectedStation.Port}\n" +
            $"User: {_selectedStation.Username}";


        _talkService.SelectStation(
            _selectedStation);


        SetButtonsEnabled(
            _selectedStation.Enabled);


        LiveVideoText.Text =
            $"LIVE VIDEO\n{_selectedStation.DisplayName}";


        FooterText.Text =
            $"Selected {_selectedStation.DisplayName}.";
    }


    private async void AnswerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!EnsureStationSelected())
            return;

        try
        {
            await _talkService.CallAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }


    private async void RejectButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await _talkService.RejectAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }


    private async void TalkButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!EnsureStationSelected())
            return;

        try
        {
            if (_talkService.State == TalkState.Connected)
            {
                await _talkService.DisconnectAsync();
            }
            else
            {
                await _talkService.CallAsync();
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }


    private async void UnlockDoorButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!EnsureStationSelected())
            return;

        try
        {
            UnlockDoorButton.IsEnabled = false;

            StatusText.Text =
                "UNLOCKING";

            FooterText.Text =
                $"Sending door unlock command to {_selectedStation!.DisplayName}...";


            await _talkService.UnlockDoorAsync();


            StatusText.Text =
                "UNLOCKED";

            FooterText.Text =
                $"Door unlocked successfully - {_selectedStation.DisplayName}";


            MessageBox.Show(
                "Door unlocked successfully.",
                "SNAPPY",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            UnlockDoorButton.IsEnabled =
                _selectedStation?.Enabled == true;
        }
    }


    private void TalkService_StateChanged(
        object? sender,
        TalkStateChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            FooterText.Text =
                e.Message;

            switch (e.State)
            {
                case TalkState.Idle:

                    StatusText.Text =
                        "READY";

                    TalkButton.Content =
                        "START TWO-WAY TALK";

                    break;


                case TalkState.Calling:

                    StatusText.Text =
                        "CALLING";

                    TalkButton.Content =
                        "CONNECTING...";

                    break;


                case TalkState.Connected:

                    StatusText.Text =
                        "TALKING";

                    TalkButton.Content =
                        "END TWO-WAY TALK";

                    break;


                case TalkState.Rejected:

                    StatusText.Text =
                        "REJECTED";

                    TalkButton.Content =
                        "START TWO-WAY TALK";

                    break;


                case TalkState.Disconnected:

                    StatusText.Text =
                        "READY";

                    TalkButton.Content =
                        "START TWO-WAY TALK";

                    break;


                case TalkState.Error:

                    StatusText.Text =
                        "ERROR";

                    TalkButton.Content =
                        "START TWO-WAY TALK";

                    break;
            }
        });
    }


    private bool EnsureStationSelected()
    {
        if (_selectedStation != null)
            return true;


        MessageBox.Show(
            "Please select an outdoor station first.",
            "SNAPPY",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        return false;
    }


    private void SetButtonsEnabled(
        bool enabled)
    {
        AnswerButton.IsEnabled =
            enabled;

        RejectButton.IsEnabled =
            enabled;

        TalkButton.IsEnabled =
            enabled;

        UnlockDoorButton.IsEnabled =
            enabled;
    }


    private void ShowError(
        Exception ex)
    {
        StatusText.Text =
            "ERROR";

        FooterText.Text =
            ex.Message;


        MessageBox.Show(
            ex.Message,
            "SNAPPY",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }


    private void Window_Closing(
        object? sender,
        System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            _talkService
                .DisconnectAsync()
                .GetAwaiter()
                .GetResult();
        }
        catch
        {
            // Do not prevent application shutdown.
        }
    }
}
