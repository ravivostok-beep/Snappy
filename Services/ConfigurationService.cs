using System.IO;
using System.Text.Json;
using SNAPPY.Models;

namespace SNAPPY.Services;

public static class ConfigurationService
{
    private static readonly string FilePath =
        Path.Combine(AppContext.BaseDirectory, "snappy-device.json");

    public static OutdoorStation Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var value = JsonSerializer.Deserialize<OutdoorStation>(
                    File.ReadAllText(FilePath));
                if (value is not null) return value;
            }
        }
        catch { }

        return new OutdoorStation();
    }
}
