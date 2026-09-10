# SNAPPY - Hikvision Indoor Station

.NET 10 WPF x64 project.

Default setup:
- Device: DS-K1T502DBFWX-C
- Outdoor IP: 192.168.0.65
- Main Indoor: MAIN INDOOR
- Room: 101
- Extension: INDOOR EXTENSION 01
- Extension number: 1
- HTTP/ISAPI: 80
- RTSP: 554

Features:
- RTSP live video
- Hikvision call-status polling
- Answer / Reject / Hang Up through ISAPI
- Real door unlock through ISAPI
- Existing Hikvision outdoor -> main indoor -> extension mapping

Important:
Hikvision intercom two-way media uses its internal SIP 2.0 media path.
This project does not expose a separate SIP-server configuration screen.
Exact two-way audio behavior depends on device firmware/media path.

Do not commit a real Hikvision password to GitHub.
