# SmartGuardHub API Documentation

## Overview

SmartGuardHub is a Smart Home Device Management System API that provides endpoints for managing devices and network configuration. The API is built with ASP.NET Core and follows RESTful principles.

**Base URL (Local Development):** `http://localhost:5000`  
**Base URL (Mobile App):** `http://<SERVER_IP>:5000` (see [Mobile App Access](#mobile-app-access) section)

**Content-Type:** `application/json`

---

## Table of Contents

1. [Mobile App Access](#mobile-app-access)
2. [Devices API](#devices-api)
3. [Network API](#network-api)
4. [Data Models](#data-models)
5. [Enumerations](#enumerations)
6. [Error Handling](#error-handling)

---

## Mobile App Access

### Finding the Server IP Address

To access the API from a mobile app, you need to use the server's IP address instead of `localhost`. Here's how to find it:

#### On Windows:
```powershell
ipconfig
```
Look for "IPv4 Address" under your active network adapter (usually `192.168.x.x` or `10.x.x.x`).

#### On Linux/Raspberry Pi:
```bash
hostname -I
```
or
```bash
ip addr show
```

#### On macOS:
```bash
ifconfig | grep "inet "
```

### Base URL for Mobile Apps

Once you have the server IP address, use it in your mobile app:

```
http://<SERVER_IP>:5000
```

**Example:**
- If your server IP is `192.168.1.100`, use: `http://192.168.1.100:5000`
- If your server IP is `10.0.0.1`, use: `http://10.0.0.1:5000`

### Network Requirements

1. **Same Network**: The mobile device and server must be on the same local network (WiFi or LAN).
2. **Firewall**: Ensure port 5000 is open on the server's firewall.
3. **Server Running**: The SmartGuardHub server must be running and listening on port 5000.

### CORS Configuration

The API is configured to accept requests from any origin (CORS enabled), so mobile apps can make requests without CORS issues.

### Mobile App Integration Examples

#### Android (Kotlin) - Using Retrofit:
```kotlin
interface SmartGuardHubApi {
    @POST("api/devices/handleUserCommand")
    suspend fun handleUserCommand(@Body command: JsonCommand): Response<GeneralResponse>
    
    @GET("api/network/info")
    suspend fun getNetworkInfo(): Response<List<NetworkInfo>>
}

// Usage
val retrofit = Retrofit.Builder()
    .baseUrl("http://192.168.1.100:5000/")
    .addConverterFactory(GsonConverterFactory.create())
    .build()

val api = retrofit.create(SmartGuardHubApi::class.java)
val response = api.handleUserCommand(jsonCommand)
```

#### iOS (Swift) - Using URLSession:
```swift
let baseURL = "http://192.168.1.100:5000"

func handleUserCommand(command: JsonCommand) async throws -> GeneralResponse {
    guard let url = URL(string: "\(baseURL)/api/devices/handleUserCommand") else {
        throw URLError(.badURL)
    }
    
    var request = URLRequest(url: url)
    request.httpMethod = "POST"
    request.setValue("application/json", forHTTPHeaderField: "Content-Type")
    request.httpBody = try JSONEncoder().encode(command)
    
    let (data, _) = try await URLSession.shared.data(for: request)
    return try JSONDecoder().decode(GeneralResponse.self, from: data)
}
```

#### React Native - Using Fetch:
```javascript
const BASE_URL = 'http://192.168.1.100:5000';

const handleUserCommand = async (command) => {
  try {
    const response = await fetch(`${BASE_URL}/api/devices/handleUserCommand`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(command),
    });
    
    const data = await response.json();
    return data;
  } catch (error) {
    console.error('Error:', error);
    throw error;
  }
};
```

#### Flutter (Dart) - Using http package:
```dart
import 'package:http/http.dart' as http;
import 'dart:convert';

const String baseUrl = 'http://192.168.1.100:5000';

Future<GeneralResponse> handleUserCommand(JsonCommand command) async {
  final response = await http.post(
    Uri.parse('$baseUrl/api/devices/handleUserCommand'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode(command),
  );
  
  if (response.statusCode == 200) {
    return GeneralResponse.fromJson(jsonDecode(response.body));
  } else {
    throw Exception('Failed to handle command');
  }
}
```

### Testing from Mobile Device

1. **Find Server IP**: Use the methods above to get your server's IP address.
2. **Test Connection**: Open a browser on your mobile device and navigate to:
   ```
   http://<SERVER_IP>:5000/swagger
   ```
   You should see the Swagger UI if the connection is successful.
3. **Test API Endpoint**: Try accessing:
   ```
   http://<SERVER_IP>:5000/api/network/info
   ```
   You should receive a JSON response.

### Troubleshooting

**Cannot connect from mobile app:**
- Verify server and mobile device are on the same network
- Check that the server is running and listening on port 5000
- Ensure firewall allows connections on port 5000
- Try pinging the server IP from the mobile device

**Connection timeout:**
- Check if the server IP address is correct
- Verify the server is not using `localhost` binding (should use `0.0.0.0` or specific IP)
- Check network connectivity between devices

**CORS errors (web-based apps):**
- The API is configured to allow all origins, so CORS should not be an issue
- If you still see CORS errors, check that the CORS middleware is properly configured

---

---

## Devices API

### Handle User Command

Execute various device commands such as turning devices on/off, creating devices, renaming, and more.

**Endpoint:** `POST /api/devices/handleUserCommand`

**Request Body:**

```json
{
  "requestId": "string (optional)",
  "jsonCommandType": 0,
  "commandPayload": {
    "unitId": "string (optional)",
    "switchNo": 0,
    "installedSensorId": "string (optional)",
    "deviceType": 0,
    "name": "string (optional)",
    "inchingTimeInMs": 0
  }
}
```

**Request Parameters:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `requestId` | string | No | Unique identifier for the request (used for MQTT subscription tracking) |
| `jsonCommandType` | integer | Yes | Command type (see JsonCommandType enum) |
| `commandPayload` | object | Yes | Command-specific payload data |
| `commandPayload.unitId` | string | No | Device unit ID (e.g., Sonoff device ID) |
| `commandPayload.switchNo` | integer | No | Switch outlet number (0-3, see SwitchOutlet enum) |
| `commandPayload.installedSensorId` | string | No | ID of the installed sensor/device |
| `commandPayload.deviceType` | integer | No | Device type (see UnitType enum) |
| `commandPayload.name` | string | No | Device name |
| `commandPayload.inchingTimeInMs` | integer | No | Inching mode duration in milliseconds |

**Response:**

**Success (200 OK):**
```json
{
  "requestId": "string",
  "state": 0,
  "devicePayload": {}
}
```

**Bad Request (400):**
```json
{
  "requestId": "string",
  "state": 5,
  "devicePayload": "Device data is required"
}
```

**Conflict (409):**
```json
{
  "requestId": "string",
  "state": 8,
  "devicePayload": {}
}
```

**Internal Server Error (500):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An error occurred while processing the request."
}
```

**Example Requests:**

**Turn On Device:**
```json
{
  "requestId": "req-123",
  "jsonCommandType": 0,
  "commandPayload": {
    "installedSensorId": "sensor-001"
  }
}
```

**Turn Off Device:**
```json
{
  "requestId": "req-124",
  "jsonCommandType": 1,
  "commandPayload": {
    "installedSensorId": "sensor-001"
  }
}
```

**Create Device:**
```json
{
  "requestId": "req-125",
  "jsonCommandType": 5,
  "commandPayload": {
    "unitId": "1000123456",
    "switchNo": 0,
    "deviceType": 0,
    "name": "Living Room Light"
  }
}
```

**Rename Device:**
```json
{
  "requestId": "req-126",
  "jsonCommandType": 6,
  "commandPayload": {
    "installedSensorId": "sensor-001",
    "name": "New Device Name"
  }
}
```

**Get Device Info:**
```json
{
  "requestId": "req-127",
  "jsonCommandType": 4,
  "commandPayload": {
    "installedSensorId": "sensor-001"
  }
}
```

**Inching On:**
```json
{
  "requestId": "req-128",
  "jsonCommandType": 2,
  "commandPayload": {
    "installedSensorId": "sensor-001",
    "inchingTimeInMs": 5000
  }
}
```

**Inching Off:**
```json
{
  "requestId": "req-129",
  "jsonCommandType": 3,
  "commandPayload": {
    "installedSensorId": "sensor-001"
  }
}
```

**Load All Units:**
```json
{
  "requestId": "req-130",
  "jsonCommandType": 7,
  "commandPayload": {}
}
```

---

## Network API

### Enable Setup Mode

Resets the device to default network configuration, enabling setup mode with default access point.

**Endpoint:** `POST /api/network/setup-mode`

**Request Body:** None

**Response:**

**Success (200 OK):**
```json
{
  "message": "Setup mode enabled successfully",
  "lanIP": "10.0.0.1",
  "wifiIP": "20.0.0.1",
  "wifiSSID": "SmartHomeHub-Setup",
  "wifiPassword": "12345678"
}
```

**Error (500 Internal Server Error):**
```json
{
  "message": "Failed to enable setup mode"
}
```

**Note:** This endpoint only works on Raspberry Pi devices. On other platforms, it returns an empty 200 OK response.

---

### Get Network Information

Retrieves current network interface information including IP addresses, MAC addresses, and connection status.

**Endpoint:** `GET /api/network/info`

**Request Parameters:** None

**Response:**

**Success (200 OK):**
```json
[
  {
    "name": "eth0",
    "description": "Ethernet interface",
    "type": "Ethernet",
    "status": "Up",
    "addresses": ["192.168.1.100"],
    "macAddress": "AA:BB:CC:DD:EE:FF"
  },
  {
    "name": "wlan0",
    "description": "Wireless interface",
    "type": "Wireless80211",
    "status": "Up",
    "addresses": ["192.168.1.101"],
    "macAddress": "11:22:33:44:55:66"
  }
]
```

**Error (500 Internal Server Error):**
```json
{
  "message": "Error message details"
}
```

**Note:** This endpoint only works on Raspberry Pi devices. On other platforms, it returns an empty 200 OK response.

---

### Configure Network

Applies user's network configuration (LAN or WiFi) with static IP or DHCP settings.

**Endpoint:** `POST /api/network/configure`

**Request Body:**

```json
{
  "networkMode": 0,
  "useDHCP": true,
  "staticIP": "192.168.1.100",
  "gateway": "192.168.1.1",
  "subnetMask": "255.255.255.0",
  "wifiSSID": "MyWiFiNetwork",
  "wifiPassword": "MyPassword123"
}
```

**Request Parameters:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `networkMode` | integer | Yes | Network mode: 0 = LAN, 1 = WIFI (see NetworkMode enum) |
| `useDHCP` | boolean | Yes | Whether to use DHCP for IP assignment |
| `staticIP` | string | Conditional | Static IP address (required if useDHCP is false) |
| `gateway` | string | No | Gateway IP address |
| `subnetMask` | string | No | Subnet mask |
| `wifiSSID` | string | Conditional | WiFi SSID (required if networkMode is WIFI) |
| `wifiPassword` | string | Conditional | WiFi password (required if networkMode is WIFI) |

**Response:**

**Success (200 OK):**
```json
{
  "message": "Network configuration applied successfully. Device will reconnect to your network.",
  "reconnectIn": "30 seconds"
}
```

**Bad Request (400):**
```json
{
  "message": "Static IP is required when DHCP is disabled for LAN"
}
```

or

```json
{
  "message": "WiFi data is missing"
}
```

**Error (500 Internal Server Error):**
```json
{
  "message": "Failed to apply network configuration"
}
```

**Note:** This endpoint only works on Raspberry Pi devices. On other platforms, it returns an empty 200 OK response.

**Example Requests:**

**LAN with DHCP:**
```json
{
  "networkMode": 0,
  "useDHCP": true
}
```

**LAN with Static IP:**
```json
{
  "networkMode": 0,
  "useDHCP": false,
  "staticIP": "192.168.1.100",
  "gateway": "192.168.1.1"
}
```

**WiFi with DHCP:**
```json
{
  "networkMode": 1,
  "useDHCP": true,
  "wifiSSID": "MyNetwork",
  "wifiPassword": "MyPassword"
}
```

**WiFi with Static IP:**
```json
{
  "networkMode": 1,
  "useDHCP": false,
  "staticIP": "192.168.1.150",
  "gateway": "192.168.1.1",
  "wifiSSID": "MyNetwork",
  "wifiPassword": "MyPassword"
}
```

---

### Test Network Connectivity

Tests network connectivity by pinging a specified host.

**Endpoint:** `GET /api/network/test-connectivity`

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `host` | string | No | "8.8.8.8" | Host to ping (IP address or hostname) |

**Response:**

**Success (200 OK):**
```json
{
  "success": true,
  "status": "Success",
  "roundtripTime": 15,
  "address": "8.8.8.8"
}
```

**Failure (200 OK with success: false):**
```json
{
  "success": false,
  "status": "TimedOut",
  "roundtripTime": 0,
  "address": null
}
```

or

```json
{
  "success": false,
  "error": "Error message details"
}
```

**Example Requests:**

```
GET /api/network/test-connectivity
GET /api/network/test-connectivity?host=8.8.8.8
GET /api/network/test-connectivity?host=google.com
```

**Note:** This endpoint only works on Raspberry Pi devices. On other platforms, it returns an empty 200 OK response.

---

## Data Models

### GeneralResponse

Standard response model for API endpoints.

```json
{
  "requestId": "string",
  "state": 0,
  "devicePayload": {}
}
```

| Field | Type | Description |
|-------|------|-------------|
| `requestId` | string | Unique identifier for the request |
| `state` | integer | Response state (see DeviceResponseState enum) |
| `devicePayload` | object | Dynamic payload containing response data |

### JsonCommand

Command request model for device operations.

```json
{
  "requestId": "string",
  "jsonCommandType": 0,
  "commandPayload": {}
}
```

| Field | Type | Description |
|-------|------|-------------|
| `requestId` | string | Unique identifier for the request |
| `jsonCommandType` | integer | Type of command (see JsonCommandType enum) |
| `commandPayload` | JsonCommandPayload | Command-specific payload |

### JsonCommandPayload

Payload data for device commands.

```json
{
  "unitId": "string",
  "switchNo": 0,
  "installedSensorId": "string",
  "deviceType": 0,
  "name": "string",
  "inchingTimeInMs": 0
}
```

| Field | Type | Description |
|-------|------|-------------|
| `unitId` | string | Device unit ID (e.g., Sonoff device ID) |
| `switchNo` | integer | Switch outlet number (see SwitchOutlet enum) |
| `installedSensorId` | string | ID of the installed sensor/device |
| `deviceType` | integer | Device type (see UnitType enum) |
| `name` | string | Device name |
| `inchingTimeInMs` | integer | Inching mode duration in milliseconds |

### NetworkConfig

Network configuration model.

```json
{
  "networkMode": 0,
  "useDHCP": true,
  "staticIP": "string",
  "gateway": "string",
  "subnetMask": "string",
  "wifiSSID": "string",
  "wifiPassword": "string"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `networkMode` | integer | Network mode (see NetworkMode enum) |
| `useDHCP` | boolean | Whether to use DHCP |
| `staticIP` | string | Static IP address |
| `gateway` | string | Gateway IP address |
| `subnetMask` | string | Subnet mask |
| `wifiSSID` | string | WiFi network SSID |
| `wifiPassword` | string | WiFi network password |

---

## Enumerations

### JsonCommandType

Command types for device operations.

| Value | Name | Description |
|-------|------|-------------|
| 0 | TurnOn | Turn device on |
| 1 | TurnOff | Turn device off |
| 2 | InchingOn | Enable inching mode |
| 3 | InchingOff | Disable inching mode |
| 4 | GetInfo | Get device information |
| 5 | CreateDevice | Create a new device |
| 6 | RenameDevice | Rename an existing device |
| 7 | LoaddAllUnits | Load all device units |

### DeviceResponseState

Response states for API operations.

| Value | Name | Description |
|-------|------|-------------|
| 0 | OK | Operation successful |
| 1 | Error | General error occurred |
| 2 | NotFound | Resource not found |
| 3 | Timeout | Operation timed out |
| 4 | BadRequest | Invalid request |
| 5 | DeviceDataIsRequired | Device data is required |
| 6 | DeviceAlreadyRegistered | Device already exists |
| 7 | DeviceNameAlreadyRegistered | Device name already in use |
| 8 | Conflict | Conflict occurred |
| 9 | InchingIntervalValidationError | Invalid inching interval |
| 10 | EmptyPayload | Empty payload provided |
| 11 | NoContent | No content available |

### UnitType

Device unit types.

| Value | Name | Description |
|-------|------|-------------|
| -1 | Unknown | Unknown device type |
| 0 | SonoffMiniR3 | Sonoff Mini R3 device |
| 1 | SonoffMiniR4M | Sonoff Mini R4M device |

### SwitchOutlet

Switch outlet numbers.

| Value | Name | Description |
|-------|------|-------------|
| -1 | Unknown | Unknown switch |
| 0 | First | First switch outlet |
| 1 | Second | Second switch outlet |
| 2 | Third | Third switch outlet |
| 3 | Fourth | Fourth switch outlet |

### SwitchOutletStatus

Switch outlet status values.

| Value | Name | Description |
|-------|------|-------------|
| 0 | Off | Switch is off |
| 1 | On | Switch is on |

### NetworkMode

Network connection modes.

| Value | Name | Description |
|-------|------|-------------|
| 0 | LAN | Ethernet/LAN connection |
| 1 | WIFI | WiFi/Wireless connection |

---

## Error Handling

### HTTP Status Codes

| Code | Description |
|------|-------------|
| 200 | OK - Request succeeded |
| 400 | Bad Request - Invalid request parameters |
| 409 | Conflict - Resource conflict occurred |
| 500 | Internal Server Error - Server error occurred |

### Error Response Format

**Standard Error:**
```json
{
  "message": "Error message description"
}
```

**Problem Details (RFC 7807):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An error occurred while processing the request."
}
```

**GeneralResponse Error:**
```json
{
  "requestId": "string",
  "state": 1,
  "devicePayload": "Error message or error details"
}
```

### Common Error Scenarios

1. **Empty Request Body**
   - Status: 400 Bad Request
   - Message: "Request body is empty."

2. **Missing Required Fields**
   - Status: 400 Bad Request
   - State: `DeviceDataIsRequired` (5)
   - Message: "Device data is required"

3. **Device Already Exists**
   - Status: 400 Bad Request
   - State: `DeviceAlreadyRegistered` (6)

4. **Device Name Conflict**
   - Status: 400 Bad Request
   - State: `DeviceNameAlreadyRegistered` (7)

5. **Invalid Inching Interval**
   - Status: 400 Bad Request
   - State: `InchingIntervalValidationError` (9)

6. **Network Configuration Errors**
   - Status: 400 Bad Request
   - Messages:
     - "Invalid configuration"
     - "Static IP is required when DHCP is disabled for LAN"
     - "WiFi data is missing"

---

## Notes

1. **Raspberry Pi Only**: Network API endpoints (`/api/network/*`) only function on Raspberry Pi devices. On other platforms, they return empty 200 OK responses.

2. **MQTT Integration**: When devices are turned on/off successfully, the system automatically publishes MQTT messages to the device data topic.

3. **Request ID**: The `requestId` field is used for MQTT subscription tracking, allowing mobile apps to receive acknowledgments for their specific actions.

4. **Base URL**: 
   - **Local Development**: `http://localhost:5000`
   - **Mobile App Access**: `http://<SERVER_IP>:5000` (replace `<SERVER_IP>` with your server's IP address)
   - See [Mobile App Access](#mobile-app-access) section for details on finding the server IP

5. **Content-Type**: All requests should use `Content-Type: application/json` header.

6. **CORS Enabled**: The API is configured to accept requests from any origin, making it accessible from mobile apps and web applications.

7. **Network Access**: The server listens on `0.0.0.0:5000`, allowing connections from any network interface. Ensure your firewall allows incoming connections on port 5000.

---

## Version

**API Version:** v1  
**Documentation Version:** 1.0  
**Last Updated:** 2024

