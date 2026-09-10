using System;

namespace SNAPPY.Models;

public sealed class OutdoorStation
{
    public int Id { get; set; }
    public string Name { get; set; } = "OUTDOOR 01";
    public string IpAddress { get; set; } = "192.168.0.65";
    public int HttpPort { get; set; } = 80;
    public int RtspPort { get; set; } = 554;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = "DS-K1T502DBFWX-C";
    public string MainIndoorName { get; set; } = "MAIN INDOOR";
    public string RoomNumber { get; set; } = "101";
    public string ExtensionName { get; set; } = "INDOOR EXTENSION 01";
    public string ExtensionNumber { get; set; } = "1";

    public string RtspUrl
    {
        get
        {
            var user = Uri.EscapeDataString(Username ?? string.Empty);
            var pass = Uri.EscapeDataString(Password ?? string.Empty);
            return $"rtsp://{user}:{pass}@{IpAddress}:{RtspPort}/Streaming/Channels/101";
        }
    }

    public override string ToString() => $"{Name}  •  {IpAddress}";
}
