using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text.Json;
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
    private readonly IndoorConfiguration _indoor;
    private readonly Dictionary<int, DeviceIsapiService> _services = new();
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;
    private Media? _currentMedia;
    private readonly DispatcherTimer _timer;
    private readonly SemaphoreSlim _pollLock = new(1, 1);
    private OutdoorStation? _selectedStation;
    private bool _incomingCall;
    private bool _ringState;
    private bool _audioOpen;

    public MainWindow()
    {
        InitializeComponent();

        _stations = ConfigurationService.Load();
        _indoor = ConfigurationService.LoadIndoor();
        ApplyIndoorConfiguration();
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

    private void ApplyIndoorConfiguration()
    {
        Title = $"SNAPPY - {_indoor.RoomNumber} Indoor Call Station";
        IndoorHeaderText.Text = $"ROOM {_indoor.RoomNumber} • INDOOR CALL STATION";
        RoomHeaderText.Text = _indoor.RoomNumber;
        StationMappingHint.Text = $"Each configured station monitors Room {_indoor.RoomNumber}";
        CallSequenceText.Text = $"{_indoor.RoomNumber}  →  CALL";
        FooterMappingText.Text = $"MAX 9 OUTDOOR STATIONS • ROOM {_indoor.RoomNumber}";
        FooterText.Text = $"Waiting for: {_indoor.RoomNumber} → CALL on any configured Outdoor Station...";
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
            $"Indoor: {_indoor.IndoorName}\n" +
            $"Room: {_indoor.RoomNumber}\n" +
            $"Extension: {_indoor.ExtensionName} ({_indoor.ExtensionNumber})\n" +
            $"Outdoor Mapping Room: {station.RoomNumber}\n" +
            $"2-Way Audio: {(station.TwoWayAudioEnabled ? $"ENABLED (CH {station.TwoWayAudioChannel})" : "DISABLED")}";
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
                StatusText.Text = _incomingCall ? "RINGING • CAMERA LIVE" : "CAMERA LIVE";
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
                    var status = ParseCallStatus(raw);
                    return new StationCallResult(station, status.IsActive, status.Status, raw);
                }
                catch (Exception ex)
                {
                    return new StationCallResult(station, false, "offline", ex.Message);
                }
            });

            var results = await Task.WhenAll(checks);
            var caller = results.FirstOrDefault(x => x.IsActive);

            if (caller is not null)
            {
                await HandleIncomingCallAsync(caller);
            }
            else if (_incomingCall)
            {
                await ClearIncomingCallAsync();
            }
        }
        finally
        {
            _pollLock.Release();
        }
    }

    private async Task HandleIncomingCallAsync(StationCallResult caller)
    {
        if (_indoor.AutoSelectCallingOutdoor && _selectedStation?.Id != caller.Station.Id)
            OutdoorStationsList.SelectedItem = caller.Station;

        if (!_incomingCall)
        {
            _incomingCall = true;
            CallStateText.Text = "INCOMING CALL";
            CallStateText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            IncomingBannerText.Text = $"INCOMING CALL • {caller.Station.Name} • ROOM {caller.Station.RoomNumber}";
            IncomingBanner.Visibility = Visibility.Visible;
            StatusText.Text = "RINGING";
            StatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            FooterText.Text =
                $"Physical call detected: {_indoor.RoomNumber} → CALL from {caller.Station.Name}. " +
                $"Device status: {caller.Status}.";
            RingOperator();
        }
        else
        {
            RingOperator();
        }

        await Task.CompletedTask;
    }

    private async Task ClearIncomingCallAsync()
    {
        _incomingCall = false;
        _ringState = false;

        if (_audioOpen && _selectedStation is not null && _services.TryGetValue(_selectedStation.Id, out var service))
        {
            try
            {
                await service.CloseTwoWayAudioAsync(_selectedStation.TwoWayAudioChannel);
            }
            catch
            {
            }
        }

        _audioOpen = false;
        CallStateText.Text = "NO ACTIVE CALL";
        CallStateText.Foreground = System.Windows.Media.Brushes.White;
        IncomingBanner.Visibility = Visibility.Collapsed;
        StatusText.Text = "READY";
        StatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
        FooterText.Text = $"Waiting for: {_indoor.RoomNumber} → CALL on any configured Outdoor Station...";
    }

    private void RingOperator()
    {
        _ringState = !_ringState;
        if (_ringState)
            SystemSounds.Exclamation.Play();
    }

    private static CallStatusInfo ParseCallStatus(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new CallStatusInfo(false, "empty");

        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            var statusElement = FindProperty(root, "status");
            var status = statusElement?.GetString() ?? string.Empty;

            if (status.Equals("ring", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("ringing", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("onCall", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("calling", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("incoming", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("dialing", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("talking", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("connected", StringComparison.OrdinalIgnoreCase))
            {
                return new CallStatusInfo(true, status);
            }

            return new CallStatusInfo(false, string.IsNullOrWhiteSpace(status) ? "idle" : status);
        }
        catch (JsonException)
        {
            var text = raw.ToLowerInvariant();
            if (text.Contains("\"status\":\"ring\"") ||
                text.Contains("\"status\":\"ringing\"") ||
                text.Contains("\"status\":\"oncall\"") ||
                text.Contains("\"status\":\"calling\"") ||
                text.Contains("\"status\":\"incoming\""))
            {
                return new CallStatusInfo(true, "ring");
            }

            return new CallStatusInfo(false, "unknown");
        }
    }

    private static JsonElement? FindProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    return property.Value;

                var nested = FindProperty(property.Value, propertyName);
                if (nested.HasValue)
                    return nested.Value;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindProperty(item, propertyName);
                if (nested.HasValue)
                    return nested.Value;
            }
        }

        return null;
    }

    private DeviceIsapiService? CurrentService =>
        _selectedStation is not null && _services.TryGetValue(_selectedStation.Id, out var service)
            ? service
            : null;

    private async void AnswerButton_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentService is null || _selectedStation is null)
            return;

        try
        {
            FooterText.Text = $"Answering {_selectedStation.Name} for Room {_indoor.RoomNumber}...";
            var result = await CurrentService.AnswerAsync();

            if (result.Contains("statusCode", StringComparison.OrdinalIgnoreCase) &&
                result.Contains("1", StringComparison.OrdinalIgnoreCase))
            {
                CallStateText.Text = "CALL ANSWERED";
                CallStateText.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                CallStateText.Text = "ANSWER COMMAND SENT";
            }

            FooterText.Text = result;

            if (_indoor.EnableTwoWayAudioCommandOnAnswer && _selectedStation.TwoWayAudioEnabled)
            {
                try
                {
                    var audioResult = await CurrentService.OpenTwoWayAudioAsync(_selectedStation.TwoWayAudioChannel);
                    _audioOpen = true;
                    FooterText.Text = $"CALL ANSWERED • TWO-WAY AUDIO CHANNEL {_selectedStation.TwoWayAudioChannel} OPEN COMMAND SENT\n{audioResult}";
                }
                catch (Exception audioEx)
                {
                    FooterText.Text = $"CALL ANSWERED. Two-way audio open was not accepted: {audioEx.Message}";
                }
            }
            else
            {
                FooterText.Text = "CALL ANSWERED • TWO-WAY AUDIO COMMAND DISABLED IN CONFIGURATION";
            }
        }
        catch (Exception ex)
        {
            FooterText.Text =
                "Answer failed. The device firmware may require the call command to be sent through the indoor station/SDK.\n" +
                ex.Message;
        }
    }

    private async void RejectButton_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentService is null)
            return;

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
        if (CurrentService is null)
            return;

        try
        {
            FooterText.Text = "Hang-up command sending...";
            var result = await CurrentService.HangUpAsync();

            try
            {
                await CurrentService.CloseTwoWayAudioAsync(_selectedStation.TwoWayAudioChannel);
            }
            catch
            {
            }

            _audioOpen = false;
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
        if (CurrentService is null)
            return;

        try
        {
            FooterText.Text = "Unlock command sending...";
            if (!_indoor.EnableDoorUnlock)
            {
                FooterText.Text = "Door unlock is disabled in Indoor Configuration.";
                return;
            }

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
            MessageBox.Show(
                "Configuration saved. Restart SNAPPY to load the new station connections.",
                "SNAPPY",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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

    private sealed record CallStatusInfo(bool IsActive, string Status);

    private sealed record StationCallResult(
        OutdoorStation Station,
        bool IsActive,
        string Status,
        string RawResponse);
}
