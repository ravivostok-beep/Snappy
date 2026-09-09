using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SNAPPY.Services;

public sealed class HikvisionIsapiService : IDisposable
{
private readonly HttpClient _http;

```
public HikvisionIsapiService(
    string ip,
    int port,
    string username,
    string password)
{
    var handler = new HttpClientHandler
    {
        Credentials = new NetworkCredential(username, password),
        PreAuthenticate = false
    };

    _http = new HttpClient(handler)
    {
        BaseAddress = new Uri($"http://{ip}:{port}"),
        Timeout = TimeSpan.FromSeconds(5)
    };
}

public async Task<string> GetCallStatusAsync(
    CancellationToken token = default)
{
    using var response = await _http.GetAsync(
        "/ISAPI/VideoIntercom/callStatus?format=json",
        token);

    return await response.Content.ReadAsStringAsync(token);
}

public Task<string> AnswerAsync(
    CancellationToken token = default)
{
    return SendCallSignalAsync("answer", token);
}

public Task<string> RejectAsync(
    CancellationToken token = default)
{
    return SendCallSignalAsync("reject", token);
}

public Task<string> HangUpAsync(
    CancellationToken token = default)
{
    return SendCallSignalAsync("hangUp", token);
}

private async Task<string> SendCallSignalAsync(
    string command,
    CancellationToken token)
{
    var json = JsonSerializer.Serialize(new
    {
        CallSignal = new
        {
            cmdType = command
        }
    });

    using var content = new StringContent(
        json,
        Encoding.UTF8,
        "application/json");

    using var response = await _http.PutAsync(
        "/ISAPI/VideoIntercom/callSignal?format=json",
        content,
        token);

    return await response.Content.ReadAsStringAsync(token);
}

public async Task<string> UnlockDoorAsync(
    CancellationToken token = default)
{
    const string xml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<RemoteControlDoor version=\"2.0\" " +
        "xmlns=\"http://www.isapi.org/ver20/XMLSchema\">" +
        "<cmd>open</cmd>" +
        "</RemoteControlDoor>";

    using var content = new StringContent(
        xml,
        Encoding.UTF8,
        "application/xml");

    using var response = await _http.PutAsync(
        "/ISAPI/AccessControl/RemoteControl/door/1",
        content,
        token);

    return await response.Content.ReadAsStringAsync(token);
}

public void Dispose()
{
    _http.Dispose();
}
```

}
