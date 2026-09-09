using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using SNAPPY.Models;
using SNAPPY.Services;

namespace SNAPPY;

public partial class MainWindow : Window
{
    private readonly OutdoorStation _station;
    private readonly HikvisionIsapiService _hikvision;
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;
    private readonly DispatcherTimer _timer;
    private bool _callActive;

    public MainWindow()
    {
        InitializeComponent();

        _station = ConfigurationService.Load();

        _hikvision = new HikvisionIsapiService(
            _station.IpAddress,
            _station.HttpPort,
            _station.Username,
            _station.Password);

        Core.Initialize();

        _libVlc = new LibVLC(
            "--network-caching=250",
            "--rtsp-tcp");

        _mediaPlayer = new MediaPlayer(_libVlc);
        VideoView.MediaPlayer = _mediaPlayer;

        DeviceText.Text =
            $"{_station.Name} • {_station.IpAddress}";

        MappingText.Text =
            $"Outdoor: {_station.Name}\n" +
            $"Main Indoor: {_station.MainIndoorName}\n" +
            $"Room: {_station.RoomNumber}\n" +
            $"Extension: {_station.ExtensionName} " +
            $"({_station.ExtensionNumber})";

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _timer.Tick += async (_, _) => await PollAsync();
        _timer.Start();

        Loaded += async (_, _) => await StartVideoAsync();
        Closed += (_, _) => Cleanup();
    }

    private async Task StartVideoAsync()
    {
        try
        {
            using var media =
                new Media(_libVlc, new Uri(_station.RtspUrl));

            await _mediaPlayer.PlayAsync(media);

            StatusText.Text = "RTSP live started";
            FooterText.Text =
                "Monitoring Hikvision call status.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "RTSP connection failed";
            FooterText.Text = ex.Message;
        }
    }

    private async Task PollAsync()
    {
        try
        {
            var raw = await _hikvision.GetCallStatusAsync();
            var active = IsActive(raw);

            if (active && !_callActive)
            {
                _callActive = true;
                CallStateText.Text = "INCOMING CALL";
                StatusText.Text = "Incoming Hikvision call";
                FooterText.Text =
                    "Incoming call detected.";
            }
            else if (!active && _callActive)
            {
                _callActive = false;
                CallStateText.Text = "No active call";
                StatusText.Text = "Ready";
            }
        }
        catch
        {
            StatusText.Text =
                "Hikvision API unavailable";
        }
    }

    private static bool IsActive(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var t = raw.ToLowerInvariant();

        if (t.Contains("idle") ||
            t.Contains("inactive") ||
            t.Contains(""status":0") ||
            t.Contains(""status":"0""))
            return false;

        return t.Contains("ring") ||
               t.Contains("calling") ||
               t.Contains("incoming") ||
               t.Contains("talking") ||
               t.Contains("connected");
    }

    private async void AnswerButton_Click(
        object sender, RoutedEventArgs e)
    {
        await RunCommandAsync(
            "ANSWER", _hikvision.AnswerAsync);
    }

    private async void RejectButton_Click(
        object sender, RoutedEventArgs e)
    {
        await RunCommandAsync(
            "REJECT", _hikvision.RejectAsync);
    }

    private async void HangupButton_Click(
        object sender, RoutedEventArgs e)
    {
        await RunCommandAsync(
            "HANG UP", _hikvision.HangUpAsync);
    }

    private async Task RunCommandAsync(
        string name, Func<Task<string>> command)
    {
        try
        {
            FooterText.Text =
                $"{name} command sent...";

            var result = await command();

            FooterText.Text =
                $"{name} response: {result}";
        }
        catch (Exception ex)
        {
            FooterText.Text =
                $"{name} failed: {ex.Message}";
        }
    }

    private async void UnlockButton_Click(
        object sender, RoutedEventArgs e)
    {
        try
        {
            FooterText.Text =
                "Unlock command sent...";

            var result =
                await _hikvision.UnlockDoorAsync();

            FooterText.Text =
                $"Door unlock response: {result}";
        }
        catch (Exception ex)
        {
            FooterText.Text =
                $"Door unlock failed: {ex.Message}";
        }
    }

    private void Cleanup()
    {
        _timer.Stop();
        _mediaPlayer.Stop();
        _mediaPlayer.Dispose();
        _libVlc.Dispose();
        _hikvision.Dispose();
    }
}
