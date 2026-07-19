# Device Scanning Cycle — Flow Documentation

**Component:** `DevicesScanner` (BackgroundService)  
**File:** `Features/DeviceManagement/DevicesScanner.cs`

---

## Overview

The scanner runs on a **5-second base tick**. On each tick it decides which units to poll (based on `SyncPeriodicity`), communicates with each device, syncs changed readings to the cloud via MQTT, and persists any state changes to `SensorConfig.json`.

---

## Full Flow Chart

```mermaid
flowchart TD
    A([Every 5 seconds]) --> B[ScanForConfiguredUnits]

    B --> C{Group InstalledSensors\nby UnitId}

    C --> D[For each unit group]

    D --> E{SyncPeriodicity\nset?}

    E -- No --> G[Poll device]
    E -- Yes --> F{Elapsed time >=\nSyncPeriodicity?}
    F -- No --> SKIP[Skip unit\nthis tick]
    F -- Yes --> G

    G --> H[GetSystemSensor\nby SensorType]
    H --> I{systemSensor\nfound?}
    I -- No --> SKIP
    I -- Yes --> J[HTTP POST\nGetInfo to device]

    J --> K{Exception\nthrown?}

    K -- Yes --> L[Log error\nAdd offline entry\nto scanned\nDo NOT update lastScannedAt]
    L --> D

    K -- No --> M{response.State\n== OK AND\nPayload != null?}

    M -- No\nerror response --> N[Add offline entry\nto scanned\nDo NOT update lastScannedAt]
    N --> D

    M -- Yes --> O[MapRawInfoResponseToSensorConfig\nfor each sensor on this unit]

    O --> P[Set IsOnline = true\nLastReading from device\nLastSeen = now\nLastTimeValueSet if value changed\nIsInInchingMode, InchingModeWidthInMs]

    P --> Q[Add to scanned\nUpdate lastScannedAt = now]
    Q --> D

    D -- all units processed --> R{scanned.Count\n== 0?}

    R -- Yes\nall units within periodicity --> WAIT([Wait 5 seconds])
    R -- No --> S[SyncSensorsToCloud]

    S --> T[For each sensor in scanned]

    T --> U{EventChangeSync\n== true\nAND IsOnline == true?}
    U -- No --> T2[skip]
    U -- Yes --> V{LastReading\n!= null?}
    V -- No --> T2
    V -- Yes --> W[Get previousReading\nfrom InstalledSensors]
    W --> X{ShouldPublish?}

    X --> X1{previousReading\n== null?}
    X1 -- Yes → first reading --> PUB
    X1 -- No --> X2{EventChangeDelta\nset?}

    X2 -- No --> X3{newReading\n!= previousReading?}
    X3 -- Yes --> PUB[Publish to MQTT\nSyncro/DeviceId/Sensors/SensorConfig.Id\nretain = true]
    X3 -- No --> T2

    X2 -- Yes --> X4{Both values\nparseable as double?}
    X4 -- Yes --> X5{|new - old|\n>= delta?}
    X5 -- Yes --> PUB
    X5 -- No --> T2
    X4 -- No\nnon-numeric --> X3

    PUB --> T
    T2 --> T
    T -- all sensors processed --> Y[ScannedSensorsChanged]

    Y --> Z[For each sensor in scanned\nfind in InstalledSensors by Id]
    Z --> AA{Any difference in:\nIsOnline\nLastReading\nIsInInchingMode\nInchingModeWidthInMs?}

    AA -- No change --> WAIT
    AA -- Changed --> AB[UpdateListDeviceAsync]

    AB --> AC[Update in-memory InstalledSensors\nfor each scanned sensor:\nIsOnline, LastSeen, LastReading\nIsInInchingMode, InchingModeWidthInMs\nLastTimeValueSet]

    AC --> AD[SaveAllAsync\nWrite SensorConfig.json\natomic temp+rename]

    AD --> WAIT
```

---

## Scenario Matrix

### Scenario 1 — Normal poll, value unchanged

| Step | Result |
|---|---|
| Periodicity elapsed | Yes → poll |
| HTTP response | OK |
| `LastReading` | Same as previous |
| `ShouldPublish` | `false` (no change) |
| `ScannedSensorsChanged` | `false` |
| File write | No |
| MQTT publish | No |

---

### Scenario 2 — Normal poll, value changed (no delta)

| Step | Result |
|---|---|
| Periodicity elapsed | Yes → poll |
| HTTP response | OK |
| `EventChangeSync` | `true`, `EventChangeDelta` = null |
| `LastReading` | Changed |
| `ShouldPublish` | `true` (any change) |
| `ScannedSensorsChanged` | `true` |
| File write | Yes |
| MQTT publish | Yes → `Syncro/{DeviceId}/Sensors/{SensorConfig.Id}` retain=true |

---

### Scenario 3 — Value changed, within delta threshold

| Step | Result |
|---|---|
| Periodicity elapsed | Yes → poll |
| HTTP response | OK |
| `EventChangeSync` | `true`, `EventChangeDelta` = 1.0 |
| `LastReading` changed | `"22.4"` → `"22.7"` (diff = 0.3 < 1.0) |
| `ShouldPublish` | `false` (below delta) |
| `ScannedSensorsChanged` | `true` (LastReading differs) |
| File write | Yes |
| MQTT publish | No |

---

### Scenario 4 — Value changed, beyond delta threshold

| Step | Result |
|---|---|
| Periodicity elapsed | Yes → poll |
| HTTP response | OK |
| `EventChangeSync` | `true`, `EventChangeDelta` = 1.0 |
| `LastReading` changed | `"20.0"` → `"22.5"` (diff = 2.5 >= 1.0) |
| `ShouldPublish` | `true` |
| `ScannedSensorsChanged` | `true` |
| File write | Yes |
| MQTT publish | Yes |

---

### Scenario 5 — Device goes offline (HTTP exception)

| Step | Result |
|---|---|
| Periodicity elapsed | Yes → poll |
| HTTP call | Exception thrown |
| `_lastScannedAt` | NOT updated → retried every 5s tick |
| Offline entry added | `IsOnline = false`, all other fields preserved from memory |
| `SyncSensorsToCloud` | Skipped (`IsOnline = false`) |
| `ScannedSensorsChanged` | `true` (`IsOnline` changed from `true` → `false`) |
| File write | Yes (`IsOnline = false` persisted) |
| MQTT data publish | No |
| Error logged | Yes |

---

### Scenario 6 — Device returns error response (error != 0)

| Step | Result |
|---|---|
| Periodicity elapsed | Yes → poll |
| HTTP response | OK (HTTP 200) but `error != 0` in body |
| `response.State` | `BadRequest` |
| `_lastScannedAt` | NOT updated → retried every 5s tick |
| Offline entry added | `IsOnline = false`, all other fields preserved |
| `ScannedSensorsChanged` | `true` if was previously online |
| File write | Yes |
| MQTT data publish | No |

---

### Scenario 7 — Device comes back online

| Step | Result |
|---|---|
| Previous state | `IsOnline = false` |
| HTTP call | Succeeds |
| `MapRawInfoResponseToSensorConfig` | `IsOnline = true`, fresh readings |
| `_lastScannedAt` | Updated |
| `ShouldPublish` | Depends on `EventChangeDelta` and value change |
| `ScannedSensorsChanged` | `true` (`IsOnline` changed from `false` → `true`) |
| File write | Yes |
| MQTT data publish | Yes (if `EventChangeSync` and value qualifies) |

---

### Scenario 8 — Unit within SyncPeriodicity, not yet due

| Step | Result |
|---|---|
| `_lastScannedAt` present and elapsed < `SyncPeriodicity` | Skip unit entirely |
| Unit added to scanned | No |
| `scanned.Count` (if all units skipped) | 0 → early `continue` |
| File write | No |
| MQTT publish | No |

---

### Scenario 9 — First reading ever (previousReading == null)

| Step | Result |
|---|---|
| `current.LastReading` | `null` |
| `ShouldPublish` | `true` unconditionally |
| MQTT publish | Yes |

---

### Scenario 10 — EventChangeSync = false

| Step | Result |
|---|---|
| `EventChangeSync` | `false` |
| `SyncSensorsToCloud` | Skips this sensor |
| File write | Still happens if state changed |
| MQTT data publish | No |

---

## Key Design Decisions

| Decision | Reason |
|---|---|
| `SyncSensorsToCloud` runs **before** `UpdateListDeviceAsync` | Delta is compared against the OLD in-memory reading. If update ran first, the comparison would always see no change. |
| `_lastScannedAt` is **not updated on failure** | Failed units are retried every 5s tick to detect recovery ASAP, regardless of their `SyncPeriodicity`. |
| `BuildOfflineSensorEntry` preserves all runtime fields | Prevents `UpdateListDeviceAsync` from overwriting `LastReading`, `LastSeen` etc. with zeroed values on a failed scan. |
| `ScannedSensorsChanged` compares only the **scanned subset** | Since `scanned` is a partial list (only periodicity-due units), comparing against the full `InstalledSensors` would always differ. |
| `LastSeen` and `LastTimeValueSet` excluded from `ScannedSensorsChanged` | `LastSeen` changes on every successful scan — including it would trigger a file write every tick even when nothing meaningful changed. |
| Property pattern `response is { State: OK, DevicePayload: not null }` | Allows the compiler to track non-nullness of `DevicePayload` inside the if-body, eliminating null-dereference warnings cleanly. |
