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

```
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

    // Initialize LibVLC.
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

    _timer.Tick += async (_, _) =>
    {
        await PollAsync();
    };

    _timer.Start();

    Loaded += async (_, _) =>
    {
        await StartVideoAsync();
    };

    Closed += (_, _) =>
    {
        Cleanup();
    };
}

private async Task StartVideoAsync()
{
    try
    {
        var media = new Media(
            _libVlc,
            new Uri(_station.RtspUrl));

        // LibVLCSharp 3.10.x uses Play(Media),
        // not MediaPlayer.PlayAsync().
        var started = _mediaPlayer.Play(media);

        if (started)
        {
            StatusText.Text = "RTSP LIVE";

            FooterText.Text =
                "Hikvision RTSP live stream started.";
        }
        else
        {
            StatusText.Text = "RTSP START FAILED";

            FooterText.Text =
                "LibVLC could not start the RTSP stream.";

            media.Dispose();
        }
    }
    catch (Exception ex)
    {
        StatusText.Text =
            "RTSP CONNECTION FAILED";

        FooterText.Text =
            ex.Message;
    }

    await Task.CompletedTask;
}

private async Task PollAsync()
{
    try
    {
        var raw =
            await _hikvision.GetCallStatusAsync();

        var active =
            IsCallActive(raw);

        if (active && !_callActive)
        {
            _callActive = true;

            CallStateText.Text =
                "INCOMING CALL";

            StatusText.Text =
                "Incoming Hikvision call";

            FooterText.Text =
                "Incoming call detected.";
        }
        else if (!active && _callActive)
        {
            _callActive = false;

            CallStateText.Text =
                "NO ACTIVE CALL";

            StatusText.Text =
                "READY";

            FooterText.Text =
                "Waiting for Hikvision call.";
        }
    }
    catch
    {
        StatusText.Text =
            "HIKVISION API UNAVAILABLE";
    }
}

private static bool IsCallActive(string raw)
{
    if (string.IsNullOrWhiteSpace(raw))
    {
        return false;
    }

    var text =
        raw.ToLowerInvariant();

    // Inactive states.
    if (text.Contains("idle") ||
        text.Contains("inactive") ||
        text.Contains("\"status\":0") ||
        text.Contains("\"status\":\"0\""))
    {
        return false;
    }

    // Active call states.
    return text.Contains("ring") ||
           text.Contains("calling") ||
           text.Contains("incoming") ||
           text.Contains("talking") ||
           text.Contains("connected");
}

private async void AnswerButton_Click(
    object sender,
    RoutedEventArgs e)
{
    try
    {
        FooterText.Text =
            "ANSWER command sending...";

        var result =
            await _hikvision.AnswerAsync();

        FooterText.Text =
            $"ANSWER response: {result}";

        CallStateText.Text =
            "CALL ANSWERED";

        _callActive = true;
    }
    catch (Exception ex)
    {
        FooterText.Text =
            $"ANSWER failed: {ex.Message}";
    }
}

private async void RejectButton_Click(
    object sender,
    RoutedEventArgs e)
{
    try
    {
        FooterText.Text =
            "REJECT command sending...";

        var result =
            await _hikvision.RejectAsync();

        FooterText.Text =
            $"REJECT response: {result}";

        CallStateText.Text =
            "CALL REJECTED";

        _callActive = false;
    }
    catch (Exception ex)
    {
        FooterText.Text =
            $"REJECT failed: {ex.Message}";
    }
}

private async void HangupButton_Click(
    object sender,
    RoutedEventArgs e)
{
    try
    {
        FooterText.Text =
            "HANG UP command sending...";

        var result =
            await _hikvision.HangUpAsync();

        FooterText.Text =
            $"HANG UP response: {result}";

        CallStateText.Text =
            "CALL ENDED";

        _callActive = false;
    }
    catch (Exception ex)
    {
        FooterText.Text =
            $"HANG UP failed: {ex.Message}";
    }
}

private async void UnlockButton_Click(
    object sender,
    RoutedEventArgs e)
{
    try
    {
        FooterText.Text =
            "UNLOCK command sending...";

        var result =
            await _hikvision.UnlockDoorAsync();

        FooterText.Text =
            $"DOOR UNLOCK RESPONSE: {result}";
    }
    catch (Exception ex)
    {
        FooterText.Text =
            $"DOOR UNLOCK FAILED: {ex.Message}";
    }
}

private void Cleanup()
{
    try
    {
        _timer.Stop();
    }
    catch
    {
    }

    try
    {
        _mediaPlayer.Stop();
    }
    catch
    {
    }

    try
    {
        _mediaPlayer.Dispose();
    }
    catch
    {
    }

    try
    {
        _libVlc.Dispose();
    }
    catch
    {
    }

    try
    {
        _hikvision.Dispose();
    }
    catch
    {
    }
}
```

}
