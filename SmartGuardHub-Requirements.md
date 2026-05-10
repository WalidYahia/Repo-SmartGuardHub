# SmartGuardHub — Requirements Specification

**Project:** SmartGuardHub  
**Platform:** Raspberry Pi (Linux primary) / Windows (fallback)  
**Date Extracted:** 2026-05-03

---

## Table of Contents

1. [System Architecture](#1-system-architecture)
2. [Sensor & Device Management](#2-sensor--device-management)
3. [User Commands & Control Operations](#3-user-commands--control-operations)
4. [User Scenario Automation](#4-user-scenario-automation)
5. [MQTT Integration & Cloud Communication](#5-mqtt-integration--cloud-communication)
6. [REST Device Protocol (Sonoff)](#6-rest-device-protocol-sonoff)
7. [Device Discovery & Scanning](#7-device-discovery--scanning)
8. [Logging & Monitoring](#8-logging--monitoring)
9. [Data Persistence](#9-data-persistence)
10. [Network Configuration (Raspberry Pi)](#10-network-configuration-raspberry-pi)
11. [API Endpoints](#11-api-endpoints)
12. [Background Services](#12-background-services)
13. [Key Business Rules & Constraints](#13-key-business-rules--constraints)

---

## 1. System Architecture

### 1.1 Application Stack

| Aspect              | Detail                                                   |
|---------------------|----------------------------------------------------------|
| Framework           | ASP.NET Core 8.0                                         |
| API Style           | REST + OpenAPI/Swagger                                   |
| Listen Address      | `http://0.0.0.0:5000`                                   |
| Database            | SQLite (system logs only)                                |
| Messaging           | MQTT over TLS (HiveMQ cloud)                            |
| Device Protocol     | REST (HTTP POST to Sonoff LAN API)                       |
| Target Platforms    | Raspberry Pi (Linux) and Windows                         |
| CORS Policy         | `AllowMobileApp` — any origin, method, and header        |

### 1.2 Device Identity

- **DeviceId** = `"SmartGuard-"` + CPU serial number
  - On Linux: CPU serial read from `/proc/cpuinfo`
  - On Windows: `Environment.MachineName` is used as fallback
- On Linux startup, the system hostname is set to the DeviceId

### 1.3 Startup Sequence

1. Detect OS platform; build DeviceId
2. Set hostname (Linux only)
3. Create and migrate SQLite log database
4. Start MQTT service and message listener
5. Call `DeviceService.InitializeAsync()` → loads `SensorConfig.json` into memory and publishes to MQTT
6. Enable CORS
7. Map controllers

---

## 2. Sensor & Device Management

### 2.1 Sensor Types

| Value | Name                |
|-------|---------------------|
| 0     | Unknown             |
| 1     | SonOffMiniR3Switch  |
| 2     | Temperature         |
| 3     | Humidity            |
| 4     | Pressure            |
| 5     | Motion              |
| 6     | Gas                 |
| 7     | Light               |
| 8     | Vibration           |
| 9     | Current             |
| 10    | Voltage             |

### 2.2 Switch Configuration Enums

**SwitchOutlet** (physical outlet index):

| Value | Name    |
|-------|---------|
| -1    | Unknown |
| 0     | First   |
| 1     | Second  |
| 2     | Third   |
| 3     | Fourth  |

**SwitchNo** (logical switch number, matches cloud):

| Value | Name    |
|-------|---------|
| -1    | Non     |
| 0     | Switch1 |
| 1     | Switch2 |
| 2     | Switch3 |
| 3     | Switch4 |
| 4     | Switch5 |
| 5     | Switch6 |
| 6     | Switch7 |
| 7     | Switch8 |

**SwitchOutletStatus**: Off = 0, On = 1

### 2.3 SensorConfig — Full Data Model

| Field                  | Type      | Persisted | Description                                                                 |
|------------------------|-----------|-----------|-----------------------------------------------------------------------------|
| `Id`                   | string    | Yes       | Deterministic key — see computation rule below                              |
| `DeviceId`             | string    | Yes       | Parent device identifier (`SmartGuard-{serial}`)                           |
| `SensorId`             | Guid      | Yes       | Unique sensor GUID                                                          |
| `SwitchNo`             | int       | Yes       | Logical switch number (SwitchNo enum, 0–7)                                  |
| `UnitId`               | string    | Yes       | Physical unit ID (e.g. Sonoff eWeLink device ID)                            |
| `Address`              | int?      | Yes       | Optional hardware address                                                   |
| `Port`                 | int?      | Yes       | Optional communication port override                                        |
| `DisplayName`          | string    | Yes       | User-facing label for the sensor                                            |
| `Url`                  | string    | Yes       | Base URL for REST (`http://eWeLink_{unitId}:8081`)                          |
| `SensorType`           | int       | Yes       | SensorType enum value                                                       |
| `Protocol`             | int       | Yes       | 0 = REST, 1 = Zigbee, 2 = MQTT, 3 = Other                                  |
| `DataPath`             | string    | Yes       | REST path for switch control (e.g. `/zeroconf/switches`)                    |
| `InfoPath`             | string    | Yes       | REST path for device status (e.g. `/zeroconf/info`)                         |
| `InchingPath`          | string    | Yes       | REST path for pulse/inching control (e.g. `/zeroconf/pulses`)               |
| `SyncPeriodicity`      | int?      | Yes       | Polling interval in seconds (e.g. 10)                                       |
| `EventChangeSync`      | bool      | Yes       | Publish to cloud on value change                                            |
| `EventChangeDelta`     | double?   | Yes       | Minimum delta required to trigger publish                                   |
| `IsInInchingMode`      | bool      | Yes       | Whether the switch is currently in pulse mode                               |
| `InchingModeWidthInMs` | int       | Yes       | Current pulse width in milliseconds                                         |
| `InstalledAt`          | DateTime  | Yes       | UTC timestamp of when the sensor was registered                             |
| `IsActive`             | bool      | Yes       | Active / inactive toggle                                                    |
| `Notes`                | string?   | Yes       | Optional free-text notes                                                    |
| `LastReading`          | string?   | Yes       | Most recent sensor value as a string (e.g. "1" = On, "0" = Off)            |
| `IsOnline`             | bool      | Yes*      | Live connectivity flag — updated by scanner, saved to file                  |
| `LastSeen`             | DateTime  | Yes*      | Last successful contact timestamp — updated by scanner, saved to file       |
| `LastTimeValueSet`     | DateTime  | Yes*      | When `LastReading` last changed — updated by scanner, saved to file         |

> \* These fields are updated by the background `DevicesScanner` and persisted to `SensorConfig.json`, but are **not** re-published to MQTT after scanner updates (the `RefreshDevices()` call is commented out in `UpdateListDeviceAsync`).

### 2.4 SensorConfig ID Computation Rule

```
Id = "{deviceId}_{sensorType}_{unitIdPart}_{switchPart}_{addressPart}_{portPart}"

Where:
  unitIdPart  = unitId            (or "unitId"    if null/empty)
  switchPart  = SwitchNo.ToString() (or "switch"  if SwitchNo == Non)
  addressPart = address.Value     (or "address"   if null)
  portPart    = port.Value        (or "port"       if null)
```

**Example:**  
`SmartGuard-WALID_SonOffMiniR3Swich_10016ca843_Switch1_address_port`

### 2.5 Supported Device Units (`sensorTypes.json`)

| Unit Name      | Protocol | Base URL Pattern                        | Port | Data Path            | Info Path        | Inching Path         |
|----------------|----------|-----------------------------------------|------|----------------------|------------------|----------------------|
| SonoffMiniR3   | REST     | `http://eWeLink_{unitId}[.local]:8081`  | 8081 | `/zeroconf/switches` | `/zeroconf/info` | `/zeroconf/pulses`   |
| SonoffMiniR4M  | REST     | `http://eWeLink_{unitId}[.local]:8081`  | 8081 | `/zeroconf/switches` | `/zeroconf/info` | `/zeroconf/pulses`   |

> `.local` suffix is appended in Production (`appsettings.Production.json`) for mDNS resolution on the local network.

---

## 3. User Commands & Control Operations

### 3.1 Command Types (`JsonCommandType`)

| Value | Name               | Source     |
|-------|--------------------|------------|
| -1    | Ping               | API        |
| 0     | TurnOn             | API / MQTT |
| 1     | TurnOff            | API / MQTT |
| 2     | InchingOn          | API / MQTT |
| 3     | InchingOff         | API / MQTT |
| 4     | GetInfo            | API / MQTT |
| 5     | CreateDevice       | API / MQTT |
| 6     | RenameDevice       | API / MQTT |
| 7     | LoadAllUnits       | API / MQTT |
| 10    | SaveUserScenario   | MQTT only  |
| 11    | DeleteUserScenario | MQTT only  |

### 3.2 `JsonCommand` Structure

```json
{
  "RequestId": "string (optional)",
  "JsonCommandType": 0,
  "CommandPayload": {
    "UnitId": "string",
    "SwitchNo": 0,
    "Address": null,
    "Port": null,
    "InstalledSensorId": "string",
    "SensorType": 1,
    "Name": "string",
    "InchingTimeInMs": 1000,
    "UserScenario": { ... }
  }
}
```

### 3.3 Command Behavior Details

#### TurnOn / TurnOff
1. Load `SensorConfig` from `SystemManager.InstalledSensors` by `InstalledSensorId`
2. POST to `{Url + DataPath}` with `switch: "on"` or `switch: "off"` payload
3. On success: set `LastReading = "1"` (TurnOn) or `"0"` (TurnOff), update `LastSeen` and `LastTimeValueSet`
4. Persist to `SensorConfig.json` via `DeviceService.UpdateDeviceAsync()`
5. Re-publish full sensor list to MQTT `DeviceSensorConfig` topic
6. Publish numeric state (`1` or `0`) to `Syncro/{DeviceId}/sensors/{SensorId}/data`

#### InchingOn
1. **Validate**: `InchingTimeInMs >= 1000 ms` — returns `InchingIntervalValidationError` if less
2. Call `GetInfo` first to fetch current pulse state from the device
3. POST inching payload to `{Url + InchingPath}` with `pulse: "on"`, `switch: "on"`, `width: InchingTimeInMs`
4. Update `IsInInchingMode = true`, `InchingModeWidthInMs = value`
5. Persist and publish

#### InchingOff
1. POST inching payload with `pulse: "off"`, `width: 0`
2. Update `IsInInchingMode = false`, `InchingModeWidthInMs = 0`
3. Persist and publish

#### CreateDevice
- **Required fields**: `UnitId`, `SensorType`, `Name`
- **Validations**:
  - `SensorType` must exist in `sensorTypes.json`
  - Duplicate `(UnitId + SwitchNo)` → error `DeviceAlreadyRegistered`
  - Duplicate `DisplayName` → error `DeviceNameAlreadyRegistered`
- **Operation**:
  1. Compute deterministic `Id` via `SensorConfig.ComputeId()`
  2. Build `SensorConfig` from the matching `SensorUnitDefinition` template
  3. Set `Url = BaseUrl + UnitId + ":" + PortNo`
  4. Add to `SystemManager.InstalledSensors`
  5. Persist to `SensorConfig.json`
  6. Publish full sensor list to MQTT `DeviceSensorConfig` topic

#### RenameDevice
- **Required**: `InstalledSensorId`, non-empty `Name`
- Updates `DisplayName`, persists, re-publishes to MQTT

#### GetInfo
- POST to `{Url + InfoPath}` with empty data body
- Returns raw `SonoffMiniRResponsePayload`

#### LoadAllUnits
- Returns the entire in-memory `SystemManager.InstalledSensors` list

#### Ping
- Returns `200 OK` immediately without any device communication

### 3.4 Response State Codes (`DeviceResponseState`)

| Code | Name                         | HTTP Status |
|------|------------------------------|-------------|
| 0    | OK                           | 200         |
| 1    | Error                        | 200         |
| 2    | NotFound                     | 200         |
| 3    | Timeout                      | 200         |
| 4    | BadRequest                   | 400         |
| 5    | DeviceDataIsRequired         | 400         |
| 6    | DeviceAlreadyRegistered      | 400         |
| 7    | DeviceNameAlreadyRegistered  | 400         |
| 8    | Conflict                     | 409         |
| 9    | InchingIntervalValidationError | 400       |
| 10   | EmptyPayload                 | 400         |
| 11   | NoContent                    | 400         |

---

## 4. User Scenario Automation

### 4.1 `UserScenario` Model

| Field                | Type                       | Description                                      |
|----------------------|----------------------------|--------------------------------------------------|
| `Id`                 | string                     | Auto-generated GUID if not provided              |
| `Name`               | string                     | Required, human-readable label                   |
| `IsEnabled`          | bool                       | Enable/disable toggle                            |
| `TargetSensorId`     | string                     | `SensorConfig.Id` of the sensor to actuate       |
| `Action`             | SwitchOutletStatus         | On or Off                                        |
| `LogicOfConditions`  | ScenarioLogic              | AND or OR for combining conditions               |
| `Conditions`         | List\<UserScenarioCondition\> | Trigger conditions list                       |

### 4.2 Condition Types (`ScenarioCondition`)

| Value | Name                 | Trigger Rule                                                                 |
|-------|----------------------|------------------------------------------------------------------------------|
| 0     | Duration             | Sensor has been ON (`LastReading = "1"`) for ≥ `DurationInSeconds`           |
| 1     | OnTime               | Current time is within ±5 seconds of `Time` AND sensor state ≠ Action value  |
| 2     | OnOtherSensorValue   | Every listed sensor in `SensorsDependency` matches its expected value         |

### 4.3 `UserScenarioCondition` Structure

```json
{
  "Condition": 0,
  "DurationInSeconds": 300,
  "Time": "08:30:00",
  "SensorsDependency": [
    {
      "SensorId": "string",
      "Value": "string",
      "Operator": 0
    }
  ]
}
```

### 4.4 Scenario Operators (`ScenarioOperator`)

Equals, NotEquals, GreaterThan, LessThan, GreaterOrEqual, LessOrEqual

- For `SensorType = 1` (Switch): compares as boolean (`"1"` = true, `"0"` = false)
- For all other types: compares as `double`

### 4.5 Scenario Logic (`ScenarioLogic`)

- **And**: ALL conditions must evaluate to true
- **Or**: ANY condition must evaluate to true

### 4.6 Scenario Execution Engine (`UserScenarioWorker`)

- Runs as a background service every **2.5 seconds**
- Only processes scenarios where `IsEnabled = true`
- For each scenario, evaluates all conditions using the configured logic (AND / OR)
- **Anti-rapid-trigger guard (OnTime)**: scenario will not re-execute if triggered within 5 seconds of its last execution
- **Duration condition**: uses `LastTimeValueSet` to measure how long sensor has been ON
- On match: dispatches TurnOn or TurnOff via `UserCommandHandler`
- Exceptions are logged per-scenario; the worker continues with remaining scenarios

---

## 5. MQTT Integration & Cloud Communication

### 5.1 Broker Configuration (`MqttConfig.json`)

| Setting         | Value                                                        |
|-----------------|--------------------------------------------------------------|
| Broker          | `5cb35f5ee0c643b58bc4c341167c1687.s1.eu.hivemq.cloud`       |
| Port            | 8883                                                         |
| TLS             | Enabled (certificate validation disabled)                    |
| Username        | `smartGuard`                                                 |
| Password        | (configured in `MqttConfig.json`)                            |
| ClientId        | DeviceId (`SmartGuard-{serial}`)                             |
| Keep-alive      | 5 seconds                                                    |
| CleanSession    | false (retains subscriptions across reconnects)              |
| Reconnect Policy | Infinite retries on disconnection, 3-second delay between attempts |

### 5.2 MQTT Topic Structure

Format: `Syncro/{DeviceId}/{TopicName}`

| Direction    | Topic Name          | Content                               | Retain |
|--------------|---------------------|---------------------------------------|--------|
| → Cloud      | `DeviceSensorConfig` | Full `SensorConfig[]` JSON array     | true   |
| → Cloud      | `UserScenario`      | Full `UserScenario[]` JSON array      | true   |
| → Cloud      | `DeviceData`        | Numeric sensor state (1 or 0)         | true   |
| → Cloud      | `RemoteAction_Ack`  | `GeneralResponse` with `RequestId`    | false  |
| ← Cloud      | `CloudSensorConfig` | `SensorConfig[]` to overwrite locally | —      |
| ← Cloud      | `CloudUserScenario` | `UserScenario[]` to overwrite locally | —      |
| ← Cloud      | `RemoteAction`      | `JsonCommand` for remote execution    | —      |
| ← Cloud      | `RemoteUpdate`      | Firmware/system update trigger        | —      |

**DeviceData topic (per-sensor):** `Syncro/{DeviceId}/sensors/{SensorId}/data`  
Published only on successful TurnOn or TurnOff commands.

### 5.3 Inbound Message Handling

| Topic               | Behavior                                                                                   |
|---------------------|--------------------------------------------------------------------------------------------|
| `CloudSensorConfig` | Deserialize list → save to `SensorConfig.json`. Does **not** update in-memory list.        |
| `CloudUserScenario` | Deserialize list → save to `UserScenarios.json`. Does **not** update in-memory list.       |
| `RemoteAction`      | Deserialize `JsonCommand` → execute via `UserCommandHandler` → publish `RemoteAction_Ack` |
| `RemoteUpdate`      | Received but not yet implemented                                                           |

### 5.4 Outbound Publishing Triggers

| Event                                  | Topic Published         |
|----------------------------------------|-------------------------|
| Startup / `RefreshDevices()`           | `DeviceSensorConfig`    |
| CreateDevice / UpdateDevice            | `DeviceSensorConfig`    |
| TurnOn / TurnOff (via API or MQTT)     | `DeviceData`            |
| SaveUserScenario / DeleteUserScenario  | `UserScenario`          |
| Any RemoteAction completion            | `RemoteAction_Ack`      |

---

## 6. REST Device Protocol (Sonoff)

### 6.1 Protocol Settings

| Setting         | Value                          |
|-----------------|--------------------------------|
| HTTP Method     | POST                           |
| Content-Type    | `application/json`             |
| Timeout         | 10 seconds                     |
| Serialization   | CamelCase JSON                 |

### 6.2 Request Payload (`DeviceRequest`)

```json
{
  "deviceid": "string",
  "data": {
    "switches": [
      { "switch": "on|off", "outlet": 0 }
    ],
    "pulses": [
      { "outlet": 0, "switch": "on|off", "pulse": "on|off", "width": 1000 }
    ]
  }
}
```

### 6.3 Response Payload (`SonoffMiniRResponsePayload`)

```json
{
  "seq": 1,
  "error": 0,
  "data": {
    "signalStrength": -60,
    "switches": [{ "switch": "on", "outlet": 0 }],
    "configure": [{ "startup": "stay", "outlet": 0 }],
    "pulses": [{ "pulse": "off", "switch": "on", "outlet": 0, "width": 1000 }],
    "sledOnline": "string",
    "fwVersion": "string",
    "staMac": "string",
    "rssi": -60,
    "bssid": "string"
  }
}
```

- `error = 0` indicates success; any non-zero value is an error
- HTTP error mapping: 400 → BadRequest, 404 → NotFound, 408 → Timeout, others → Error

### 6.4 Supported REST Operations

| Operation   | Endpoint             | Description                                  |
|-------------|----------------------|----------------------------------------------|
| TurnOn      | `/zeroconf/switches` | Set outlet to `on`                           |
| TurnOff     | `/zeroconf/switches` | Set outlet to `off`                          |
| GetInfo     | `/zeroconf/info`     | Retrieve full device state                   |
| InchingOn   | `/zeroconf/pulses`   | Configure and activate pulse mode            |
| InchingOff  | `/zeroconf/pulses`   | Deactivate pulse mode (`pulse: "off"`)        |

---

## 7. Device Discovery & Scanning

### 7.1 `DevicesScanner` (Background Service)

- Runs every **20 seconds**
- Groups `InstalledSensors` by `UnitId`
- For each group: calls `GetInfo` on the first sensor's URL
- Parses response via `MapRawInfoResponseToSensorConfig()`
- Change detection: compares JSON serialization of current vs. scanned state
- If states differ: calls `DeviceService.UpdateListDeviceAsync(scanned)`
- Per-unit failures are caught, logged, and do not stop the scan of other units

### 7.2 Fields Updated by Scanner

| Field                | Updated If         |
|----------------------|--------------------|
| `IsOnline`           | Device responded   |
| `LastSeen`           | Device responded   |
| `LastReading`        | Always from device |
| `IsInInchingMode`    | From pulse config  |
| `InchingModeWidthInMs` | From pulse config |
| `LastTimeValueSet`   | Value changed      |

> These fields are persisted to `SensorConfig.json` but are **not** re-published to MQTT after scanner updates.

---

## 8. Logging & Monitoring

### 8.1 `SystemLog` Schema (SQLite)

| Column      | Type     | Max Length | Description            |
|-------------|----------|------------|------------------------|
| `Id`        | int      | PK         | Auto-increment         |
| `LogTime`   | DateTime | —          | UTC timestamp          |
| `Level`     | string   | 20         | INFO / TRACE / ERROR   |
| `MessageKey`| string   | 50         | Category key           |
| `Message`   | string   | 200        | Log message            |
| `Exception` | string?  | 4000       | Stack trace (nullable) |

### 8.2 Log Categories (`LogMessageKey`)

| Value | Name                  |
|-------|-----------------------|
| 0     | DevicesController     |
| 1     | DevicesConflict       |
| 2     | RestProtocol          |
| 4     | LogsCleanupCycle      |
| 4     | LogsCleanupError      |
| 5     | MissingConfig         |
| 6     | LoadConfig            |
| 7     | MqttNotConnected      |
| 8     | UserCommandHandler    |
| 9     | ScanDevicesError      |
| 10    | UserScenario          |

### 8.3 `LogCleanupService` (Background Service)

- Runs every **15 minutes**
- If total log count > 1000: deletes the oldest `(total − 1000)` rows
- Otherwise: deletes all logs older than **7 days**
- Logs an INFO entry on each cleanup run

---

## 9. Data Persistence

### 9.1 File-Based Repositories

| Repository              | Dev Path                                   | Prod Path                                     |
|-------------------------|--------------------------------------------|-----------------------------------------------|
| `SensorConfig.json`     | `SensorConfig/SensorConfig.json`           | `./SensorConfig/SensorConfig.json`            |
| `UserScenarios.json`    | `UserScenarios/UserScenarios.json`         | `./UserScenarios/UserScenarios.json`          |

- **Write strategy**: Atomic — write to temp file, then rename to final path
- **Concurrency**: `SemaphoreSlim(1,1)` per repository for thread-safe access
- **File not found**: Treated as an empty list (no crash)

### 9.2 SQLite Database

| Environment | Connection String                                   |
|-------------|-----------------------------------------------------|
| Development | `Data Source=./Database/systemlogs.sqlite`          |
| Production  | `Data Source=./Database/Production/systemlogs.sqlite` |

- Automatically migrated on startup via EF Core
- Only used for system logs

### 9.3 In-Memory State

- `SystemManager.InstalledSensors` — `List<SensorConfig>`
  - Loaded on startup via `DeviceService.InitializeAsync()`
  - Updated on every Create / Update / Delete command
  - Updated by `DevicesScanner` for runtime fields
  - **Note**: Receiving `CloudSensorConfig` via MQTT saves to file but does NOT update this in-memory list

---

## 10. Network Configuration (Raspberry Pi)

### 10.1 Setup Mode (AP + LAN)

Activates a local access point so a mobile app can connect and configure the device.

| Interface | Mode        | IP / Config                                  |
|-----------|-------------|----------------------------------------------|
| `eth0`    | LAN static  | IP: `10.0.0.1/24`, Gateway: `10.0.0.1`       |
| `wlan0`   | WiFi AP     | SSID: `SmartHomeHub-Setup`, Password: `12345678`, IP: `20.0.0.1/24`, Band: 2.4GHz, Channel: 6, Security: WPA-PSK |

### 10.2 Normal Network Modes

| Mode         | Interface | Configuration                                         |
|--------------|-----------|-------------------------------------------------------|
| DHCP         | eth0      | `nmcli` auto IP                                       |
| Static LAN   | eth0      | User-provided IP, gateway; DNS: `8.8.8.8` / `8.8.4.4` |
| DHCP WiFi    | wlan0     | Connect to SSID with password                         |
| Static WiFi  | wlan0     | User-provided IP, gateway, DNS                        |

Configuration is applied via `nmcli` commands.

---

## 11. API Endpoints

### 11.1 Devices — `/api/devices`

| Method | Path                          | Description                                  |
|--------|-------------------------------|----------------------------------------------|
| POST   | `/api/devices/handleUserCommand` | Execute any command (all `JsonCommandType` values) |

**HTTP Status Mapping:**

| Response State                  | HTTP Status |
|---------------------------------|-------------|
| OK, NotFound, Timeout, Error    | 200         |
| DeviceDataIsRequired, DeviceAlreadyRegistered, DeviceNameAlreadyRegistered, InchingIntervalValidationError, EmptyPayload, NoContent | 400 |
| Conflict                        | 409         |
| Ping (any)                      | 200 (immediate) |

### 11.2 User Scenarios — `/api/userscenario`

| Method | Path                                        | Description                         |
|--------|---------------------------------------------|-------------------------------------|
| POST   | `/api/userscenario/saveUserScenario`        | Create or update a scenario         |
| GET    | `/api/userscenario/loadUserScenarios`       | Load all scenarios                  |
| DELETE | `/api/userscenario/{id}`                    | Delete a scenario by Id             |

- `saveUserScenario`: requires `Name` and `TargetSensorId`; auto-generates `Id` (GUID) if omitted
- After save or delete: publishes full scenario list to MQTT `UserScenario` topic

### 11.3 Network — `/api/network`

| Method | Path                              | Description                              |
|--------|-----------------------------------|------------------------------------------|
| POST   | `/api/network/setup-mode`         | Enable AP + LAN setup mode (Pi only)     |
| GET    | `/api/network/info`               | Get current network interface details    |
| POST   | `/api/network/configure`          | Apply a network configuration            |
| GET    | `/api/network/test-connectivity`  | Ping test (default target: `8.8.8.8`)   |

---

## 12. Background Services

| Service                | Type              | Interval     | Responsibility                                          |
|------------------------|-------------------|--------------|---------------------------------------------------------|
| `LogCleanupService`    | `BackgroundService` | 15 minutes | Prune old log entries from SQLite                       |
| `DevicesScanner`       | `BackgroundService` | 20 seconds | Poll all configured Sonoff units; update runtime state  |
| `UserScenarioWorker`   | `BackgroundService` | 2.5 seconds | Evaluate all enabled scenarios; dispatch commands        |

---

## 13. Key Business Rules & Constraints

1. **Unique device identity**: `SensorConfig.Id` is deterministic — same (DeviceId, SensorType, UnitId, SwitchNo, Address, Port) always produces the same Id.

2. **No duplicate registrations**: Creating a device with a `(UnitId + SwitchNo)` combination that already exists returns `DeviceAlreadyRegistered`.

3. **No duplicate display names**: Creating a device with an existing `DisplayName` returns `DeviceNameAlreadyRegistered`.

4. **Inching minimum pulse width**: `InchingTimeInMs` must be ≥ 1000 ms. Values below this return `InchingIntervalValidationError`.

5. **InchingOn always fetches device state first**: A `GetInfo` call is made before sending the pulse command, to preserve the current state of other outlets.

6. **Scenario anti-flood guard**: The `OnTime` condition will not re-trigger a scenario that already executed within the last 5 seconds.

7. **Duration condition requires continuous ON state**: The sensor must have `LastReading = "1"` and `(Now − LastTimeValueSet) >= DurationInSeconds`.

8. **Cloud sync writes to file only**: Receiving `CloudSensorConfig` or `CloudUserScenario` via MQTT saves the list to disk but does **not** update `SystemManager.InstalledSensors` in memory — the in-memory state is only updated by direct device commands or the scanner.

9. **MQTT publishes are always full lists**: All MQTT publications send the complete current list (not incremental deltas).

10. **Scanner updates are not re-published to MQTT**: After `DevicesScanner` updates runtime fields (IsOnline, LastSeen, etc.) and persists them, it does not re-publish to the `DeviceSensorConfig` MQTT topic.

11. **Communication timeouts**:
    - Sonoff REST: **10 seconds**
    - Syncro Cloud HTTP client: **30 seconds**
    - MQTT keep-alive: **5 seconds**

12. **Log retention limits**:
    - Hard cap: 1000 log entries
    - Time-based: entries older than 7 days are deleted
    - Evaluated every 15 minutes
