# SNAPPY - Indoor Room 101 + Outdoor Intercom Configuration

## Workflow

The intended call workflow is:

1. Visitor enters the configured room number on an Outdoor Station.
2. For the default configuration this is `1` → `0` → `1` → Call.
3. SNAPPY polls the configured Outdoor Stations for the intercom call status.
4. When an active call is detected, SNAPPY can automatically select the calling Outdoor Station.
5. SNAPPY rings the Windows operator and shows the caller Outdoor camera.
6. Operator presses `ANSWER`.
7. SNAPPY sends the device answer command.
8. If enabled, SNAPPY sends the configured two-way-audio channel open command.
9. Operator can `HANG UP`, `REJECT`, or `UNLOCK DOOR` when enabled.

## Indoor configuration

SNAPPY now has a dedicated Indoor Room configuration:

- Indoor Name: `MAIN INDOOR`
- Room Number: `101`
- Extension Name: `INDOOR EXTENSION 01`
- Extension Number: `1`
- Default Two-Way Audio Channel: `1`
- Auto-select calling Outdoor: enabled by default
- Open two-way-audio command after Answer: enabled by default
- Door unlock: enabled by default

Indoor settings are saved to:

`%AppData%\\SNAPPY\\indoor-room.json`

Outdoor settings are saved to:

`%AppData%\\SNAPPY\\outdoor-stations.json`

## Outdoor configuration

Up to 9 Outdoor Stations can be configured. Each station has:

- Name
- IP address
- HTTP port
- RTSP port
- Username/password
- Room mapping
- Extension number
- Two-way-audio channel
- Two-way-audio enable/disable

The default Outdoor IP addresses are `192.168.0.65` through `192.168.0.73`.

## Important two-way-audio note

The project sends the device's ISAPI call answer and two-way-audio channel commands. The actual bidirectional audio media transport is firmware/device dependent. RTSP provides the camera video stream; it does not itself provide microphone-to-speaker intercom audio. If the device requires the vendor audio SDK/media transport for the audio payload, that SDK must be integrated for full PC microphone and speaker audio.

This project deliberately does not expose or require a separate SIP account/server configuration.

## Build

GitHub Actions uses .NET 10, Windows x64, self-contained publishing, and the WPF target `net10.0-windows10.0.17763.0`.

No Markdown code fences are required inside source files.
