using System;

namespace SNAPPY.Models;

public class OutdoorStation
{
    public string Name { get; set; } = "MAIN OUTDOOR";
    public string Model { get; set; } = "DS-K1T502DBFWX-C";
    public string IpAddress { get; set; } = "192.168.0.65";
    public int HttpPort { get; set; } = 80;
    public int RtspPort { get; set; } = 554;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "";
    public string MainIndoorName { get; set; } = "MAIN INDOOR";
    public string RoomNumber { get; set; } = "101";
    public string ExtensionName { get; set; } = "INDOOR EXTENSION 01";
    public string ExtensionNumber { get; set; } = "1";

    public string RtspUrl =>
        $"rtsp://{Uri.EscapeDataString(Username)}:{Uri.EscapeDataString(Password)}@" +
        $"{IpAddress}:{RtspPort}/Streaming/Channels/101";
}
