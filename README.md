# MMOAS Authority Service

## Project purpose
This repository is a **narrow backend authority validation service** for MMOAS.

Its purpose is to validate:
- true external authority boundaries
- command/event contracts
- backend-owned ability lifecycle timing
- clean separation between Unreal client prediction and backend truth

This is **not** a full MMO backend.

The service should remain intentionally narrow and finishable.

---

## Core architecture rules

### Layer boundaries
The service is divided into:

- **Transport**
    - WebSocket + HTTP endpoints
    - serialization
    - message routing
    - session lifetime
    - no domain logic

- **Application**
    - command handlers
    - orchestration
    - state loading/saving
    - invokes domain services

- **Domain**
    - authority validation rules
    - lifecycle phase legality
    - timing decisions
    - pure business logic

- **State**
    - thread-safe in-memory stores
    - replaceable abstractions
    - no transport concerns

- **Debug**
    - health routes
    - snapshots
    - event logs
    - latency/failure toggles later

---

## Ownership rules
- Transport never mutates gameplay state directly
- Domain never depends on transport types
- Hosted services mutate lifecycle state only through application/state abstractions
- Backend-generated IDs are authoritative
- Backend clock is authoritative

---
