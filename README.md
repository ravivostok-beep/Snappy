# SNAPPY - Hikvision Indoor Intercom Client

Windows WPF project for a SNAPPY-branded indoor intercom client targeting Hikvision DS-KH8350-WTE1.

## Included
- Main Windows UI
- 10 configurable outdoor-station slots
- Answer / Reject controls
- Unlock 1 / Unlock 2 controls
- RTSP live-video placeholder
- Device Settings entry point
- .NET 10 WPF project

## Build
1. Install Visual Studio 2026 (or a compatible .NET 10 SDK).
2. Open `VostokIntercom.csproj`.
3. Restore/build:
   `dotnet restore`
   `dotnet build -c Release`
4. EXE output:
   `bin\Release\net10.0-windows\SNAPPY.exe`

## Hikvision integration
The current source intentionally leaves RTSP, SIP/G.711 and door-control integration as adapters/placeholders. Add the licensed Hikvision SDK/API components and device-specific configuration before production use.

Do not commit Hikvision SDK DLLs unless their license permits redistribution.
