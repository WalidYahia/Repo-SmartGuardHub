# Sensor Config Cycle — Hub ↔ Cloud

## 1. Database Table: `DeviceConfigs`

The hub stores sensor configuration in a single SQLite table called `DeviceConfigs`.

### Design principle

**One row per config type.** All sensor configs are serialised as a single JSON array and stored in the `Config` column of that row. There is no individual row per sensor.

### Schema

| Column | Type | Description |
|---|---|---|
| `Id` | INTEGER (PK) | Auto-increment primary key |
| `ConfigType` | INTEGER | Enum: `0 = Sensor`, `1 = Network` |
| `UpdateTime` | DATETIME | UTC timestamp of the last write |
| `Config` | TEXT | JSON array of all `SensorConfig` objects for this `ConfigType` |
| `UpdatedFrom` | INTEGER | Enum: `0 = Local` (written by hub), `1 = Cloud` (received from cloud) |
| `ConfigVersion` | GUID | Changes on every local update; set to the cloud-provided value on cloud updates |
| `SyncedToCloud` | BOOLEAN | `false` when written locally (pending cloud sync); `true` after publish or when received from cloud |
| `TimeToSyncedToCloud` | DATETIME (nullable) | UTC timestamp of when `SyncedToCloud` became `true` |

### Unique constraint

`ConfigType` has a unique index — there is always at most **one row** for sensor config.

### `Config` column — JSON structure

The column stores only the sensors array. The `configVersion` and `updateTime` are separate columns and are added as an envelope when publishing over MQTT (see Section 2).

```json
[
  {
    "id": "deviceId_SonOffMiniR3_unitId_Switch1_address_port",
    "deviceId": "string",
    "sensorId": "guid",
    "switchNo": 1,
    "unitId": "string",
    "address": 502,
    "port": null,
    "displayName": "Living Room Light",
    "url": "http://192.168.1.x",
    "sensorType": 0,
    "protocol": 0,
    "dataPath": "/zeroconf/switch",
    "infoPath": "/zeroconf/info",
    "inchingPath": "/zeroconf/pulse",
    "syncPeriodicity": 30,
    "eventChangeSync": true,
    "eventChangeDelta": null,
    "isInInchingMode": false,
    "inchingModeWidthInMs": 0,
    "installedAt": "2025-01-01T00:00:00Z",
    "isActive": true,
    "notes": null
  }
]
```

---

## 2. Sensor Config Cycle

```
Hub (Local write)          MQTT Broker              Cloud API
       │                       │                        │
       │── local save ─────────────────────────────────►│  (not yet)
       │   SyncedToCloud=false │                        │
       │   ConfigVersion=NEW   │                        │
       │                       │                        │
       │  [ConfigSyncService]  │                        │
       │  every 1 minute       │                        │
       │── Publish ──────────► │ Syncro/{deviceId}/     │
       │   retainFlag=true     │   DeviceSensorConfig   │
       │   SyncedToCloud=true  │ ──────────────────────►│ subscribes
       │                       │                        │ receives full list + ConfigVersion
       │                       │                        │ stores/updates its own record
       │                       │                        │
       │◄── Publish ───────────│◄───────────────────────│ cloud pushes updated config
       │  Syncro/{deviceId}/   │                        │   (with ConfigVersion)
       │    CloudSensorConfig  │                        │
       │  [UserCommandHandler] │                        │
       │  SaveAllAsync(        │                        │
       │    configs,           │                        │
       │    Cloud,             │                        │
       │    configVersion)     │                        │
       │  SyncedToCloud=true   │                        │
       │  UpdatedFrom=Cloud    │                        │
       │  ConfigVersion=SAME   │                        │
       │  RefreshDevices(      │                        │
       │    publishToCloud=    │                        │
       │    false)             │                        │
```

### MQTT Payload — `SensorConfigEnvelope`

Both directions use the same envelope shape:

```json
{
  "configVersion": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "updateTime": "2025-06-01T10:30:00Z",
  "sensors": [
    { ... }
  ]
}
```

| Field | Type | Description |
|---|---|---|
| `configVersion` | GUID | Unique version of this config snapshot. Hub generates a new GUID on every local change. Cloud must echo its own GUID when pushing. |
| `updateTime` | ISO-8601 UTC | Timestamp of when this version was written. Used by the hub to resolve conflicts. |
| `sensors` | Array | Full list of `SensorConfig` objects (see DB schema above). |

### MQTT Topics

| Topic pattern | Direction | Retain | Payload |
|---|---|---|---|
| `Syncro/{deviceId}/DeviceSensorConfig` | Hub → Cloud | **Yes** | `SensorConfigEnvelope` (see below) |
| `Syncro/{deviceId}/CloudSensorConfig` | Cloud → Hub | No | `SensorConfigEnvelope` (see below) |

`{deviceId}` is the hub's unique device identifier.

### Flow description

#### Hub → Cloud (outbound)

1. Any local change (create / update / delete a sensor, or inching mode auto-update) calls `SaveAsync` or `SaveAllAsync` with `ConfigSource.Local`.
2. The `DeviceConfigs` row is updated: `SyncedToCloud = false`, `ConfigVersion = new GUID`.
3. `ConfigSyncService` runs every **60 seconds**, queries all rows where `SyncedToCloud = false`, deserialises the stored JSON, and publishes it to `Syncro/{deviceId}/DeviceSensorConfig` with `retainFlag = true`.
4. After a successful publish the row is marked `SyncedToCloud = true`.
5. On hub startup, `DeviceService.RefreshDevices` also publishes immediately and marks the row synced.

> The retained flag means the cloud always receives the latest config on (re)connect, even if no local change happened during the downtime.

#### Cloud → Hub (inbound)

1. The cloud publishes a `SensorConfigEnvelope` to `Syncro/{deviceId}/CloudSensorConfig`.
2. The hub deserialises the envelope and reads the current local `configVersion` and `updateTime` from the DB.
3. **Conflict guard** — the hub skips the update entirely if either condition is true:
   - `envelope.configVersion == current.configVersion` — same version, nothing changed.
   - `envelope.updateTime <= current.updateTime` — device config is equal or more recent; cloud message is stale.
4. If the guard passes, `SaveAllAsync(sensors, Cloud, configVersion)` is called:
   - `SyncedToCloud = true` immediately (already in sync — no re-publish needed).
   - `UpdatedFrom = Cloud`.
   - `ConfigVersion` stored exactly as provided by the cloud.
5. `DeviceService.RefreshDevices(publishToCloud: false)` reloads the in-memory sensor list. **No MQTT publish is triggered.**

---

## 3. Cloud API — Required Implementation

To complete the cycle the cloud API must:

### Subscribe

- Subscribe to `Syncro/{deviceId}/DeviceSensorConfig` for every registered hub.
- On receive: deserialise the `SensorConfigEnvelope`.
- Compare `configVersion` against the stored cloud-side version:
  - If the same → already up to date, skip.
  - If different → update the cloud database with the new sensors list and store the new `configVersion` and `updateTime`.
- **Do not publish back to MQTT** after receiving from the hub. The hub is the source of truth for this message.

### Publish

- When a config change originates in the cloud (e.g. mobile app edit), build a `SensorConfigEnvelope`:
  - Generate a new `configVersion` UUID.
  - Set `updateTime` to the current UTC timestamp.
  - Set `sensors` to the full updated list.
- Publish to `Syncro/{deviceId}/CloudSensorConfig`.
- Store the `configVersion` and `updateTime` in the cloud database so you can detect stale re-sends from the hub later.

### Conflict resolution summary

| Condition | Hub behaviour | Cloud behaviour |
|---|---|---|
| Hub receives cloud message with same `configVersion` | Skip — no DB write, no re-publish | — |
| Hub receives cloud message with older `updateTime` | Skip — device is more recent | — |
| Hub receives cloud message with newer `updateTime` and different `configVersion` | Accept — save to DB, reload in-memory, no re-publish | — |
| Cloud receives hub message with same `configVersion` | — | Skip — already in sync |
| Cloud receives hub message with different `configVersion` | — | Accept — update cloud DB, no re-publish |
