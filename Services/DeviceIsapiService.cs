using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SNAPPY.Services;

public sealed class DeviceIsapiService : IDisposable
{
    private readonly HttpClient _http;

    public DeviceIsapiService(string ip, int port, string username, string password)
    {
        var handler = new HttpClientHandler
        {
            Credentials = new NetworkCredential(username, password),
            PreAuthenticate = false,
            AllowAutoRedirect = false
        };

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri($"http://{ip}:{port}"),
            Timeout = TimeSpan.FromSeconds(3)
        };
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    public async Task<string> GetCallStatusAsync(CancellationToken token = default)
    {
        using var response = await _http.GetAsync("/ISAPI/VideoIntercom/callStatus?format=json", token);
        return await ReadResponseAsync(response, token);
    }

    public async Task<string> GetCallSignalCapabilitiesAsync(CancellationToken token = default)
    {
        using var response = await _http.GetAsync("/ISAPI/VideoIntercom/callSignal/capabilities?format=json", token);
        return await ReadResponseAsync(response, token);
    }

    public Task<string> AnswerAsync(CancellationToken token = default) => SendCallSignalAsync("answer", token);

    public Task<string> RejectAsync(CancellationToken token = default) => SendCallSignalAsync("reject", token);

    public Task<string> HangUpAsync(CancellationToken token = default) => SendCallSignalAsync("hangUp", token);

    private async Task<string> SendCallSignalAsync(string command, CancellationToken token)
    {
        var json = $"{{\"CallSignal\":{{\"cmdType\":\"{command}\"}}}}";
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _http.PutAsync("/ISAPI/VideoIntercom/callSignal?format=json", content, token);
        return await ReadResponseAsync(response, token);
    }

    public async Task<string> UnlockDoorAsync(CancellationToken token = default)
    {
        const string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                           "<RemoteControlDoor version=\"2.0\" xmlns=\"http://www.isapi.org/ver20/XMLSchema\">" +
                           "<cmd>open</cmd></RemoteControlDoor>";

        using var content = new StringContent(xml, Encoding.UTF8, "application/xml");
        using var response = await _http.PutAsync("/ISAPI/AccessControl/RemoteControl/door/1", content, token);
        return await ReadResponseAsync(response, token);
    }

    public async Task<string> OpenTwoWayAudioAsync(int channel = 1, CancellationToken token = default)
    {
        channel = channel < 1 ? 1 : channel;
        using var response = await _http.PutAsync($"/ISAPI/System/TwoWayAudio/channels/{channel}/open", null, token);
        return await ReadResponseAsync(response, token);
    }

    public async Task<string> CloseTwoWayAudioAsync(int channel = 1, CancellationToken token = default)
    {
        channel = channel < 1 ? 1 : channel;
        using var response = await _http.PutAsync($"/ISAPI/System/TwoWayAudio/channels/{channel}/close", null, token);
        return await ReadResponseAsync(response, token);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken token = default)
    {
        try
        {
            using var response = await _http.GetAsync("/ISAPI/System/deviceInfo", token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string> ReadResponseAsync(HttpResponseMessage response, CancellationToken token)
    {
        var body = await response.Content.ReadAsStringAsync(token);
        if (response.IsSuccessStatusCode)
            return body;

        return $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}\n{body}".Trim();
    }

    public void Dispose() => _http.Dispose();
}
