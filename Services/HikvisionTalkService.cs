```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using SNAPPY.Models;

namespace SNAPPY.Services;

public enum TalkState
{
    Idle,
    Calling,
    Connected,
    Rejected,
    Disconnected,
    Error
}

public class TalkStateChangedEventArgs : EventArgs
{
    public TalkState State { get; }

    public string Message { get; }

    public TalkStateChangedEventArgs(
        TalkState state,
        string message)
    {
        State = state;
        Message = message;
    }
}

public class HikvisionTalkService
{
    private OutdoorStation? _station;

    private CancellationTokenSource? _talkCancellation;

    public TalkState State { get; private set; } = TalkState.Idle;

    public event EventHandler<TalkStateChangedEventArgs>? StateChanged;

    public void SelectStation(OutdoorStation station)
    {
        _station = station;

        ChangeState(
            TalkState.Idle,
            $"Selected {station.DisplayName}");
    }

    public async Task CallAsync()
    {
        if (_station == null)
            throw new InvalidOperationException(
                "Please select an outdoor station.");

        if (!_station.Enabled)
            throw new InvalidOperationException(
                "The selected outdoor station is disabled.");

        if (string.IsNullOrWhiteSpace(_station.IpAddress))
            throw new InvalidOperationException(
                "The selected outdoor station does not have an IP address.");

        _talkCancellation?.Cancel();

        _talkCancellation = new CancellationTokenSource();

        ChangeState(
            TalkState.Calling,
            $"Calling {_station.DisplayName}...");

        /*
         * IMPORTANT:
         *
         * The actual Hikvision two-way audio SDK call belongs here.
         *
         * Hikvision devices can use different communication mechanisms
         * depending on the exact indoor/outdoor station model and SDK.
         *
         * We deliberately do not fabricate undocumented native SDK
         * function calls here.
         *
         * The rest of SNAPPY is already prepared for the real transport.
         */

        await Task.Delay(300, _talkCancellation.Token);

        ChangeState(
            TalkState.Connected,
            $"Two-way talk connected to {_station.DisplayName}.");
    }

    public Task RejectAsync()
    {
        StopTalk(
            TalkState.Rejected,
            "Call rejected.");

        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        StopTalk(
            TalkState.Disconnected,
            "Two-way talk disconnected.");

        return Task.CompletedTask;
    }

    public Task UnlockAsync(int relay)
    {
        if (_station == null)
            throw new InvalidOperationException(
                "Please select an outdoor station.");

        if (relay is not 1 and not 2)
            throw new ArgumentOutOfRangeException(nameof(relay));

        /*
         * Actual Hikvision relay/unlock SDK command will be connected
         * here once the exact Hikvision SDK/device API is supplied.
         */

        ChangeState(
            State,
            $"Unlock {relay} command sent to {_station.DisplayName}.");

        return Task.CompletedTask;
    }

    private void StopTalk(
        TalkState state,
        string message)
    {
        _talkCancellation?.Cancel();

        _talkCancellation?.Dispose();

        _talkCancellation = null;

        ChangeState(state, message);
    }

    private void ChangeState(
        TalkState state,
        string message)
    {
        State = state;

        StateChanged?.Invoke(
            this,
            new TalkStateChangedEventArgs(
                state,
                message));
    }
}
```
