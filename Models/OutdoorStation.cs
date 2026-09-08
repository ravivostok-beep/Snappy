using System;
using System.Text.Json.Serialization;

namespace SNAPPY.Models;

public class OutdoorStation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;

    public int Port { get; set; } = 8000;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    [JsonIgnore]
    public string DisplayName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Name))
                return IpAddress;

            return Name;
        }
    }

    [JsonIgnore]
    public string ListDisplay
    {
        get
        {
            string state =
                Enabled ? "Enabled" : "Disabled";

            return $"{DisplayName} | {IpAddress}:{Port} | {state}";
        }
    }
}
