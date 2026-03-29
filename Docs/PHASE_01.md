# Phase 01 — Message Contracts, Hello Flow, and Session Registration

## Goal
Build the first real transport protocol slice for the authority service.

Phase 01 formalizes WebSocket communication with:
- explicit message envelopes
- a hello/ready handshake
- connection-aware session tracking
- entity registration over WebSocket

This phase also includes a few repository/process cleanup fixes discovered during Phase 00 review.

The service remains intentionally narrow.

We are still NOT implementing:
- ability activation
- cooldown validation
- resources
- commit timing
- lifecycle mutation
- auth/login
- persistence

---

## Phase 01 fixes carried forward from Phase 00 review
Include these cleanup tasks as part of this phase:

- remove `/Docs/` from `.gitignore`
- stop tracking Rider `.idea` files in git
- ensure docs and implementation agree on current endpoint names
- keep current routes unless there is a strong reason to rename:
    - `GET /debug/health`
    - `GET /debug/snapshot`
    - `GET /transport/ws`
    - `POST /transport/entities`

These are considered part of the Phase 01 patch.

---

## Build

### Transport
Add explicit WebSocket message contracts and routing for a narrow handshake flow.

Implement:
- inbound message envelope
- outbound message envelope
- hello command from client
- hello acknowledged response from backend
- register-entity command from client
- entity-registered event from backend
- unsupported-message error response

Transport still owns:
- WebSocket lifetime
- serialization/deserialization
- connection loop
- routing by message type

Transport must NOT own:
- entity registration rules
- authority decisions
- gameplay validation

---

### Application
Application continues to own entity registration orchestration.

Use the existing registration service where appropriate.

If helpful, add a dedicated application service or handler for:
- hello/session initialization
- register-entity orchestration

Keep this lightweight and compile-ready.

---

### State
Add a lightweight thread-safe session store.

The session store should track only the minimum useful Phase 01 information:
- connection/session id
- connected at UTC
- whether hello completed
- optional registered entity id if assigned during this session

Use abstractions/interfaces.

Use thread-safe in-memory implementation.

---

### Debug
Extend debug snapshot coverage if useful, but keep it narrow.

At minimum:
- existing debug routes continue to work
- snapshot remains focused on entity state
- do not build large admin tooling yet

Optional:
- add a very small session debug route if it fits cleanly
- only do this if it stays simple and helpful

---

## Ownership

### Transport owns
- `/transport/ws`
- WebSocket upgrade validation
- receive loop
- message parsing
- message dispatch
- outbound message sending
- session close behavior

### Application owns
- registration orchestration
- coordinating session state updates with entity registration

### State owns
- entity registry
- session registry

### Hosting owns
- lifecycle hosted service stub only
- no new lifecycle mutation yet

### Debug owns
- health/snapshot routes only
- optional small session snapshot if clean

---

## State

### Reads
- current session state
- current entity snapshot
- backend time

### Writes
- create/update session records
- mark hello completed
- register backend-owned entity
- associate session with registered entity where applicable

### Threading / timing concerns
- WebSocket sessions may be active concurrently
- session store must be thread-safe
- entity registration may happen from multiple concurrent sockets
- hosted service still exists but must not mutate gameplay lifecycle yet
- backend time remains authoritative through `TimeProvider`

---

## Required contracts
Add explicit DTOs/contracts for:

### Inbound
- envelope with message type and version
- hello command
- register entity command

### Outbound
- envelope with message type and version
- ready/hello acknowledged message
- entity registered message
- error message for unsupported or invalid input

Keep contracts simple and boring.

Include:
- message type
- version
- server UTC timestamp where useful
- correlation/request id if helpful

Do not overdesign generic protocol infrastructure.

---

## Rules
- use moderate meaningful comments
- explain backend-specific concepts where helpful
- do not narrate obvious code
- keep boundaries clean
- compile-ready only
- no pseudocode
- do not add ability activation yet
- do not add persistence yet
- do not add authentication yet

---

## Output expectations
Before generating code, explain:
1. what you are about to build
2. which layer owns it
3. what state it reads and writes
4. any threading/timing concerns

Then generate the Phase 01 code.

Expected result:
- repository cleanup fixes included
- explicit WebSocket protocol skeleton exists
- hello/ack works
- register-entity works over WebSocket
- session registry exists
- tests cover new state/services where appropriate