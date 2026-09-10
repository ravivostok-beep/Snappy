# SNAPPY Indoor Call Station

SNAPPY is a .NET 10 WPF Windows application for an indoor call-station workflow with up to 9 configured outdoor stations.

## Main workflow

1. A caller selects room **101** on an outdoor station.
2. The caller presses the physical **CALL** button.
3. SNAPPY polls the configured outdoor stations for an active intercom call.
4. SNAPPY identifies the outdoor station that is calling.
5. SNAPPY automatically selects that station.
6. The live camera from the calling station is displayed.
7. SNAPPY shows an incoming-call banner and audible ring notification.
8. The operator can use ANSWER, REJECT, HANG UP and UNLOCK DOOR.

## Supported station count

Maximum: **9 Outdoor Stations**.

Default addresses:

- OUTDOOR 01 - 192.168.0.65
- OUTDOOR 02 - 192.168.0.66
- OUTDOOR 03 - 192.168.0.67
- OUTDOOR 04 - 192.168.0.68
- OUTDOOR 05 - 192.168.0.69
- OUTDOOR 06 - 192.168.0.70
- OUTDOOR 07 - 192.168.0.71
- OUTDOOR 08 - 192.168.0.72
- OUTDOOR 09 - 192.168.0.73

Default mapping:

- Device model: DS-K1T502DBFWX-C
- Main indoor: MAIN INDOOR
- Room: 101
- Extension: INDOOR EXTENSION 01
- Extension number: 1
- HTTP/ISAPI: 80
- RTSP: 554

## Configuration

Open **CONFIGURATION** from the main window. Settings are saved to:

`%AppData%\\SNAPPY\\outdoor-stations.json`

After saving a changed IP, username or password, restart SNAPPY so the network service connections are recreated.

## Build

Target framework: `.NET 10 WPF`

Minimum Windows target: `10.0.17763.0`

Architecture: `x64`

Runtime: `win-x64`

Self-contained publish is enabled.

## Important

The exact call-control behavior depends on the firmware capabilities of the installed devices. SNAPPY uses the device VideoIntercom ISAPI endpoints for call status, call signaling and door control. The application does not expose a separate manual SIP-account configuration screen.
