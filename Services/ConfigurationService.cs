```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SNAPPY.Models;

namespace SNAPPY.Services;

public class ConfigurationService
{
    private readonly string _configurationDirectory;
    private readonly string _configurationFile;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public ConfigurationService()
    {
        _configurationDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SNAPPY");

        _configurationFile = Path.Combine(
            _configurationDirectory,
            "outdoor-stations.json");
    }

    public async Task<List<OutdoorStation>> LoadStationsAsync()
    {
        try
        {
            if (!File.Exists(_configurationFile))
            {
                return CreateDefaultStations();
            }

            string json = await File.ReadAllTextAsync(_configurationFile);

            if (string.IsNullOrWhiteSpace(json))
                return CreateDefaultStations();

            List<OutdoorStation>? stations =
                JsonSerializer.Deserialize<List<OutdoorStation>>(
                    json,
                    JsonOptions);

            return stations ?? CreateDefaultStations();
        }
        catch
        {
            return CreateDefaultStations();
        }
    }

    public async Task SaveStationsAsync(
        IEnumerable<OutdoorStation> stations)
    {
        Directory.CreateDirectory(_configurationDirectory);

        string json = JsonSerializer.Serialize(
            stations,
            JsonOptions);

        await File.WriteAllTextAsync(
            _configurationFile,
            json);
    }

    private static List<OutdoorStation> CreateDefaultStations()
    {
        var result = new List<OutdoorStation>();

        for (int i = 1; i <= 10; i++)
        {
            result.Add(new OutdoorStation
            {
                Name = $"Outdoor {i:00}",
                Enabled = false
            });
        }

        return result;
    }
}
```
