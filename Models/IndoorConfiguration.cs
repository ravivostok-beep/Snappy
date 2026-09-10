namespace SNAPPY.Models;

public sealed class IndoorConfiguration
{
    public string IndoorName { get; set; } = "MAIN INDOOR";
    public string RoomNumber { get; set; } = "101";
    public string ExtensionName { get; set; } = "INDOOR EXTENSION 01";
    public string ExtensionNumber { get; set; } = "1";
    public int DefaultTwoWayAudioChannel { get; set; } = 1;
    public bool AutoSelectCallingOutdoor { get; set; } = true;
    public bool EnableTwoWayAudioCommandOnAnswer { get; set; } = true;
    public bool EnableDoorUnlock { get; set; } = true;
}
