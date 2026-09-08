# SNAPPY

SNAPPY is the Windows WPF client foundation for a Hikvision indoor station.

Target device:
- Hikvision DS-KH8350-WTE1

Current build:
- .NET 10
- WPF
- Windows x64
- Self-contained publish
- Responsive startup
- 10 outdoor-station placeholders
- UI-only call/unlock controls

Important:
The Hikvision proprietary SDK DLLs are intentionally not included. The next integration stage should add the official Hikvision Win64 SDK after the exact SDK package/API documentation is supplied.

GitHub Actions builds `SNAPPY/SNAPPY.csproj` and publishes a `SNAPPY-Windows-x64` artifact.
