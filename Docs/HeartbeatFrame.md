# Heartbeat Frame — Hub → Cloud

## Overview

The hub broadcasts a lightweight heartbeat frame every **60 seconds** so the cloud can track device presence and clock drift without relying on sensor readings or config updates.

---

## MQTT Topic

| Topic pattern | Direction | Retain | QoS |
|---|---|---|---|
| `Syncro/{deviceId}/Heartbeat` | Hub → Cloud | **No** | At Most Once (0) |

`{deviceId}` is the hub's unique identifier — `SmartGuard-{cpuSerial}` on Raspberry Pi, `SmartGuard-{MachineName}` in development.

---

## Payload

```json
{
  "deviceId": "SmartGuard-1000000012345678",
  "localTime": "2026-07-15T10:30:00.000Z"
}
```

| Field | Type | Description |
|---|---|---|
| `deviceId` | string | The hub's unique device identifier. Matches the `{deviceId}` segment in the MQTT topic. |
| `localTime` | ISO-8601 UTC | The hub's current UTC clock at the moment the frame was sent. |

---

## Behaviour

- Sent every **60 seconds** by `HeartbeatService` (a .NET `BackgroundService`).
- The publish is **fire-and-forget** — the hub does not wait for broker acknowledgement before continuing.
- The frame is not retained. If the cloud is offline when a frame is sent, that frame is lost; the next heartbeat will arrive within 60 seconds once the connection is restored.
- No hub-side storage — heartbeats are not persisted to the database.

---

## Cloud API — Required Implementation

### Subscribe

- Subscribe to `Syncro/+/Heartbeat` (wildcard) or per-device `Syncro/{deviceId}/Heartbeat`.
- On receive:
  - Extract `deviceId` and `localTime` from the payload.
  - Update the device's **last-seen timestamp** in the cloud database.
  - Optionally compute clock drift as `(cloudReceivedAt - localTime)` and store it for diagnostics.

### Presence detection

A device should be considered **offline** if no heartbeat has been received for more than **2 minutes** (2× the send interval, to allow for one missed frame).

### No reply required

The cloud must **not** publish a response to the heartbeat topic. The hub does not subscribe to it and does not expect an acknowledgement.

---

## Flow

```
Hub                         MQTT Broker              Cloud API
 │                               │                       │
 │  [HeartbeatService]           │                       │
 │  every 60 seconds             │                       │
 │── Publish ───────────────────►│ Syncro/{deviceId}/    │
 │   retainFlag=false            │   Heartbeat ─────────►│ subscribes
 │   QoS=AtMostOnce             │                       │ updates last-seen
 │   (fire-and-forget)          │                       │ stores localTime
 │                               │                       │
 │  (no response expected)       │                       │
```
