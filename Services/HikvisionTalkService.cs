using System;
using System.Net;
using System.Net.Http;
using System.Text;
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


public sealed class TalkStateChangedEventArgs : EventArgs
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


    public TalkState State { get; private set; } =
        TalkState.Idle;


    public event EventHandler<TalkStateChangedEventArgs>?
        StateChanged;


    public void SelectStation(
        OutdoorStation station)
    {
        _station = station;

        SetState(
            TalkState.Idle,
            $"Selected outdoor station: {station.DisplayName}");
    }


    public async Task CallAsync()
    {
        EnsureStation();

        SetState(
            TalkState.Calling,
            $"Calling {_station!.DisplayName}...");

        /*
         * The current SNAPPY project does not yet contain
         * a SIP media stack.
         *
         * Keep this method as the UI state transition.
         * Real SIP two-way audio/video can be connected
         * separately.
         */
        await Task.Delay(300);

        SetState(
            TalkState.Connected,
            $"Connected to {_station.DisplayName}.");
    }


    public Task RejectAsync()
    {
        SetState(
            TalkState.Rejected,
            "Call rejected.");

        return Task.CompletedTask;
    }


    public Task DisconnectAsync()
    {
        if (State == TalkState.Connected ||
            State == TalkState.Calling)
        {
            SetState(
                TalkState.Disconnected,
                "Disconnected.");
        }
        else
        {
            SetState(
                TalkState.Idle,
                "Ready.");
        }

        return Task.CompletedTask;
    }


    /// <summary>
    /// Sends a REAL Hikvision ISAPI command to open
    /// the physical lock connected to Door/Relay 1.
    ///
    /// DS-K1T502DBFWX-C has one lock-control output.
    /// </summary>
    public async Task UnlockDoorAsync()
    {
        EnsureStation();


        OutdoorStation station =
            _station!;


        if (!station.Enabled)
        {
            throw new InvalidOperationException(
                "The selected outdoor station is disabled.");
        }


        if (string.IsNullOrWhiteSpace(
                station.IpAddress))
        {
            throw new InvalidOperationException(
                "The outdoor station IP address is empty.");
        }


        if (string.IsNullOrWhiteSpace(
                station.Username))
        {
            throw new InvalidOperationException(
                "The Hikvision username is empty.");
        }


        if (string.IsNullOrWhiteSpace(
                station.Password))
        {
            throw new InvalidOperationException(
                "The Hikvision password is empty.");
        }


        int port =
            station.Port > 0
                ? station.Port
                : 80;


        string protocol =
            port == 443
                ? "https"
                : "http";


        string url =
            $"{protocol}://{station.IpAddress}:{port}" +
            "/ISAPI/AccessControl/RemoteControl/door/1";


        const string xml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<RemoteControlDoor " +
            "version=\"2.0\" " +
            "xmlns=\"http://www.isapi.org/ver20/XMLSchema\">" +
            "<cmd>open</cmd>" +
            "</RemoteControlDoor>";


        SetState(
            TalkState.Idle,
            $"Sending door unlock command to {station.DisplayName}...");


        using var handler =
            new HttpClientHandler
            {
                /*
                 * Hikvision devices commonly use Digest
                 * authentication for ISAPI.
                 *
                 * HttpClientHandler handles the 401
                 * challenge and calculates the Digest
                 * response using these credentials.
                 */
                Credentials =
                    new NetworkCredential(
                        station.Username,
                        station.Password),

                PreAuthenticate = false
            };


        /*
         * Many Hikvision devices use a self-signed
         * certificate when HTTPS is enabled.
         *
         * This allows local HTTPS communication.
         *
         * For production environments, this should
         * ideally be replaced with proper certificate
         * validation.
         */
        if (protocol == "https")
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler
                    .DangerousAcceptAnyServerCertificateValidator;
        }


        using var client =
            new HttpClient(handler)
            {
                Timeout =
                    TimeSpan.FromSeconds(10)
            };


        client.DefaultRequestHeaders.Accept.Clear();

        client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(
                "application/xml"));


        using var content =
            new StringContent(
                xml,
                Encoding.UTF8,
                "application/xml");


        HttpResponseMessage response;

        try
        {
            response =
                await client.PutAsync(
                    url,
                    content);
        }
        catch (TaskCanceledException)
        {
            throw new TimeoutException(
                $"No response from Hikvision device " +
                $"{station.IpAddress}:{port} within 10 seconds.");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Unable to connect to Hikvision device " +
                $"{station.IpAddress}:{port}.\n\n" +
                $"Check the IP address, ISAPI port, network connection " +
                $"and Windows Firewall.\n\n" +
                $"Details: {ex.Message}",
                ex);
        }


        string responseBody =
            await response.Content.ReadAsStringAsync();


        if (!response.IsSuccessStatusCode)
        {
            string status =
                $"{(int)response.StatusCode} " +
                response.ReasonPhrase;


            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException(
                    "Hikvision authentication failed.\n\n" +
                    "Check the Hikvision username and password.\n\n" +
                    $"Device: {station.IpAddress}:{port}");
            }


            if (response.StatusCode ==
                HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException(
                    "Hikvision ISAPI door-control endpoint was not found.\n\n" +
                    "Check that ISAPI is enabled on the device " +
                    "and that the selected port is 80 or 443.\n\n" +
                    $"URL: {url}");
            }


            throw new InvalidOperationException(
                $"Hikvision door unlock failed.\n\n" +
                $"HTTP Status: {status}\n\n" +
                $"Device: {station.IpAddress}:{port}\n\n" +
                $"Response:\n{responseBody}");
        }


        /*
         * A successful HTTP response means the Hikvision
         * device accepted the ISAPI door-control command.
         */
        SetState(
            TalkState.Idle,
            $"Door unlocked successfully - {station.DisplayName}");


        response.Dispose();
    }


    private void EnsureStation()
    {
        if (_station == null)
        {
            throw new InvalidOperationException(
                "No outdoor station has been selected.");
        }
    }


    private void SetState(
        TalkState state,
        string message)
    {
        State =
            state;


        StateChanged?.Invoke(
            this,
            new TalkStateChangedEventArgs(
                state,
                message));
    }
}
