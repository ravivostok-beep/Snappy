using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using SNAPPY.Models;
using SNAPPY.Services;

namespace SNAPPY;

public partial class MainWindow : Window
{
    private readonly List<OutdoorStation> _stations;
    private readonly Dictionary<int, DeviceIsapiService> _services = new();
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;
    private Media? _currentMedia;
    private readonly DispatcherTimer _timer;
    private readonly SemaphoreSlim _pollLock = new(1, 1);
    private OutdoorStation? _selectedStation;
    private bool _incomingCall;
    private bool _ringState;

    public MainWindow()
    {
        InitializeComponent();

        _stations = ConfigurationService.Load();
        StationCountText.Text = $"{_stations.Count} configured";
        OutdoorStationsList.ItemsSource = _stations;

        Core.Initialize();
        _libVlc = new LibVLC("--network-caching=250", "--rtsp-tcp");
        _mediaPlayer = new MediaPlayer(_libVlc);
        VideoView.MediaPlayer = _mediaPlayer;

        foreach (var station in _stations)
        {
            _services[station.Id] = new DeviceIsapiService(
                station.IpAddress,
                station.HttpPort,
                station.Username,
                station.Password);
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += async (_, _) => await PollAllStationsAsync();
        _timer.Start();

        Loaded += async (_, _) =>
        {
            if (OutdoorStationsList.Items.Count > 0)
                OutdoorStationsList.SelectedIndex = 0;
            await PollAllStationsAsync();
        };

        Closed += (_, _) => Cleanup();
    }

    private async void OutdoorStationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OutdoorStationsList.SelectedItem is not OutdoorStation station)
            return;

        _selectedStation = station;
        UpdateStationInformation(station);
        await StartVideoAsync(station);
    }

    private void UpdateStationInformation(OutdoorStation station)
    {
        DeviceText.Text = $"{station.Name} • {station.IpAddress}";
        MappingText.Text =
            $"Outdoor: {station.Name}\n" +
            $"Main Indoor: {station.MainIndoorName}\n" +
            $"Room: {station.RoomNumber}\n" +
            $"Extension: {station.ExtensionName} ({station.ExtensionNumber})";
    }

    private async Task StartVideoAsync(OutdoorStation station)
    {
        try
        {
            _mediaPlayer.Stop();
            _currentMedia?.Dispose();
            _currentMedia = new Media(_libVlc, new Uri(station.RtspUrl));
            var started = _mediaPlayer.Play(_currentMedia);

            if (started)
            {
                StatusText.Text = "CAMERA LIVE";
                StatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
                FooterText.Text = $"Live camera: {station.Name}";
            }
            else
            {
                StatusText.Text = "CAMERA FAILED";
                StatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                FooterText.Text = $"Could not start camera stream from {station.Name}.";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "CAMERA ERROR";
            StatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            FooterText.Text = ex.Message;
        }

        await Task.CompletedTask;
    }

    private async Task PollAllStationsAsync()
    {
        if (!await _pollLock.WaitAsync(0))
            return;

        try
        {
            var checks = _stations.Select(async station =>
            {
                try
                {
                    var raw = await _services[station.Id].GetCallStatusAsync();
                    return (station, Active: IsCallActive(raw));
                }
                catch
                {
                    return (station, Active: false);
                }
            });

            var results = await Task.WhenAll(checks);
            var caller = results.FirstOrDefault(x => x.Active).station;
            var hasCaller = results.Any(x => x.Active);

            if (hasCaller && caller is not null)
            {
                if (_selectedStation?.Id != caller.Id)
                {
                    OutdoorStationsList.SelectedItem = caller;
                }

                if (!_incomingCall)
                {
                    _incomingCall = true;
                    CallStateText.Text = "INCOMING CALL";
                    CallStateText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                    IncomingBannerText.Text = $"INCOMING CALL • {caller.Name} • ROOM {caller.RoomNumber}";
                    IncomingBanner.Visibility = Visibility.Visible;
                    StatusText.Text = "RINGING";
                    StatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                    FooterText.Text = $"Call received from {caller.Name} for room {caller.RoomNumber}.";
                }

                RingOperator();
            }
            else if (_incomingCall)
            {
                _incomingCall = false;
                CallStateText.Text = "NO ACTIVE CALL";
                CallStateText.Foreground = System.Windows.Media.Brushes.White;
                IncomingBanner.Visibility = Visibility.Collapsed;
                StatusText.Text = "READY";
                StatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
                FooterText.Text = "Waiting for an incoming call...";
            }
        }
        finally
        {
            _pollLock.Release();
        }
    }

    private void RingOperator()
    {
        _ringState = !_ringState;
        if (_ringState)
            SystemSounds.Exclamation.Play();
    }

    private static bool IsCallActive(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var text = raw.ToLowerInvariant();
        if (text.Contains("idle") || text.Contains("inactive") ||
            text.Contains("\"status\":0") || text.Contains("\"status\":\"0\""))
            return false;

        return text.Contains("ring") ||
               text.Contains("calling") ||
               text.Contains("incoming") ||
               text.Contains("dialing") ||
               text.Contains("talking") ||
               text.Contains("connected");
    }

    private DeviceIsapiService? CurrentService =>
        _selectedStation is not null && _services.TryGetValue(_selectedStation.Id, out var service)
            ? service
            : null;

    private async void AnswerButton_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentService is null) return;
        try
        {
            FooterText.Text = "Answer command sending...";
            var result = await CurrentService.AnswerAsync();
            CallStateText.Text = "CALL ANSWERED";
            FooterText.Text = result;
        }
        catch (Exception ex)
        {
            FooterText.Text = $"Answer failed: {ex.Message}";
        }
    }

    private async void RejectButton_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentService is null) return;
        try
        {
            FooterText.Text = "Reject command sending...";
            var result = await CurrentService.RejectAsync();
            _incomingCall = false;
            IncomingBanner.Visibility = Visibility.Collapsed;
            CallStateText.Text = "CALL REJECTED";
            FooterText.Text = result;
        }
        catch (Exception ex)
        {
            FooterText.Text = $"Reject failed: {ex.Message}";
        }
    }

    private async void HangupButton_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentService is null) return;
        try
        {
            FooterText.Text = "Hang-up command sending...";
            var result = await CurrentService.HangUpAsync();
            _incomingCall = false;
            IncomingBanner.Visibility = Visibility.Collapsed;
            CallStateText.Text = "CALL ENDED";
            FooterText.Text = result;
        }
        catch (Exception ex)
        {
            FooterText.Text = $"Hang-up failed: {ex.Message}";
        }
    }

    private async void UnlockButton_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentService is null) return;
        try
        {
            FooterText.Text = "Unlock command sending...";
            var result = await CurrentService.UnlockDoorAsync();
            FooterText.Text = $"DOOR UNLOCK RESPONSE: {result}";
        }
        catch (Exception ex)
        {
            FooterText.Text = $"Door unlock failed: {ex.Message}";
        }
    }

    private void ConfigurationButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new ConfigurationWindow
        {
            Owner = this
        };
        window.ShowDialog();
        if (window.ConfigurationSaved)
        {
            MessageBox.Show("Configuration saved. Restart SNAPPY to load the new station connections.", "SNAPPY", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Cleanup()
    {
        try { _timer.Stop(); } catch { }
        try { _mediaPlayer.Stop(); } catch { }
        try { _mediaPlayer.Dispose(); } catch { }
        try { _currentMedia?.Dispose(); } catch { }
        try { _libVlc.Dispose(); } catch { }
        foreach (var service in _services.Values)
        {
            try { service.Dispose(); } catch { }
        }
        _pollLock.Dispose();
    }
}
