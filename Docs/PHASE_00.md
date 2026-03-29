# Phase 00 — Backend Skeleton

## Goal
Create the clean backend service skeleton with transport, state, debug, and lifecycle hosted service stubs.

No ability authority logic yet.

---

## Build
- ASP.NET Core service
- WebSocket endpoint stub
- debug routes
- thread-safe entity store
- lifecycle hosted service stub
- DI registration
- xUnit test project setup

---

## Ownership
- Transport owns socket/session lifetime
- State owns in-memory entity registry
- Debug owns health/snapshot routes
- Hosted service owns future authority clock loop stub

---

## State
### Reads
- entity registry snapshot

### Writes
- entity registration only
- no lifecycle mutation yet

---

## Out of scope
- ability activation
- command contracts
- cooldowns
- resources
- commit timing
- event logs