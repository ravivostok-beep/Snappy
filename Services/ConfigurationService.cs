using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SNAPPY.Models;

namespace SNAPPY.Services;

public static class ConfigurationService
{
    public const int MaximumOutdoorStations = 9;

    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SNAPPY");

    private static readonly string FilePath = Path.Combine(Folder, "outdoor-stations.json");
    private static readonly string IndoorFilePath = Path.Combine(Folder, "indoor-room.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static List<OutdoorStation> Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return CreateDefaults();

            var json = File.ReadAllText(FilePath);
            var stations = JsonSerializer.Deserialize<List<OutdoorStation>>(json, JsonOptions);
            return Normalize(stations ?? new List<OutdoorStation>());
        }
        catch
        {
            return CreateDefaults();
        }
    }

    public static void Save(IEnumerable<OutdoorStation> stations)
    {
        Directory.CreateDirectory(Folder);
        var normalized = Normalize(stations.ToList());
        File.WriteAllText(FilePath, JsonSerializer.Serialize(normalized, JsonOptions));
    }

    public static IndoorConfiguration LoadIndoor()
    {
        try
        {
            if (!File.Exists(IndoorFilePath))
                return new IndoorConfiguration();

            var json = File.ReadAllText(IndoorFilePath);
            return JsonSerializer.Deserialize<IndoorConfiguration>(json, JsonOptions)
                   ?? new IndoorConfiguration();
        }
        catch
        {
            return new IndoorConfiguration();
        }
    }

    public static void SaveIndoor(IndoorConfiguration configuration)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(IndoorFilePath, JsonSerializer.Serialize(configuration, JsonOptions));
    }

    public static string GetFilePath() => FilePath;
    public static string GetIndoorFilePath() => IndoorFilePath;

    public static List<OutdoorStation> CreateDefaults()
    {
        var result = new List<OutdoorStation>();
        for (var i = 1; i <= MaximumOutdoorStations; i++)
        {
            result.Add(new OutdoorStation
            {
                Id = i,
                Name = $"OUTDOOR {i:00}",
                IpAddress = $"192.168.0.{64 + i}",
                HttpPort = 80,
                RtspPort = 554,
                Username = "admin",
                Password = string.Empty,
                DeviceModel = "DS-K1T502DBFWX-C",
                MainIndoorName = "MAIN INDOOR",
                RoomNumber = "101",
                ExtensionName = "INDOOR EXTENSION 01",
                ExtensionNumber = "1",
                TwoWayAudioChannel = 1,
                TwoWayAudioEnabled = true
            });
        }
        return result;
    }

    private static List<OutdoorStation> Normalize(List<OutdoorStation> source)
    {
        var result = source.Where(x => x is not null).Take(MaximumOutdoorStations).ToList();
        var defaults = CreateDefaults();

        for (var i = 0; i < MaximumOutdoorStations; i++)
        {
            if (i >= result.Count)
            {
                result.Add(defaults[i]);
                continue;
            }

            var item = result[i];
            item.Id = i + 1;
            item.Name = string.IsNullOrWhiteSpace(item.Name) ? defaults[i].Name : item.Name.Trim();
            item.IpAddress = string.IsNullOrWhiteSpace(item.IpAddress) ? defaults[i].IpAddress : item.IpAddress.Trim();
            item.HttpPort = item.HttpPort is < 1 or > 65535 ? 80 : item.HttpPort;
            item.RtspPort = item.RtspPort is < 1 or > 65535 ? 554 : item.RtspPort;
            item.Username = string.IsNullOrWhiteSpace(item.Username) ? "admin" : item.Username.Trim();
            item.DeviceModel = string.IsNullOrWhiteSpace(item.DeviceModel) ? defaults[i].DeviceModel : item.DeviceModel.Trim();
            item.MainIndoorName = string.IsNullOrWhiteSpace(item.MainIndoorName) ? defaults[i].MainIndoorName : item.MainIndoorName.Trim();
            item.RoomNumber = string.IsNullOrWhiteSpace(item.RoomNumber) ? defaults[i].RoomNumber : item.RoomNumber.Trim();
            item.ExtensionName = string.IsNullOrWhiteSpace(item.ExtensionName) ? defaults[i].ExtensionName : item.ExtensionName.Trim();
            item.ExtensionNumber = string.IsNullOrWhiteSpace(item.ExtensionNumber) ? defaults[i].ExtensionNumber : item.ExtensionNumber.Trim();
            item.TwoWayAudioChannel = item.TwoWayAudioChannel < 1 ? 1 : item.TwoWayAudioChannel;
        }

        return result;
    }
}
