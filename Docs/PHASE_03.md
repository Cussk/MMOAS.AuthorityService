# Phase 03 — Activation Instance State and Debug Visibility

## Goal
Introduce backend-owned activation instances and make them visible through debug endpoints.

Phase 03 transitions the system from:
- stateless activation decisions

to:
- explicit, inspectable backend activation instances

This aligns the backend with MMOAS-style runtime instances.

---

## Why this phase exists

Phase 02 proved:
- transport works
- session lifecycle works
- activation requests can be validated
- accepted/rejected responses flow correctly

However:
- accepted activations are not persisted
- there is no backend concept of an "activation instance"
- nothing exists to inspect or correlate later

Phase 03 introduces:
- backend-generated activation instance IDs
- in-memory activation state tracking
- debug visibility for activations

This is the first step toward:
- lifecycle progression
- commit timing
- reconciliation with Unreal

---

## Build

### Domain
Domain continues to own validation logic.

No major changes required here.

Do NOT add:
- cooldowns
- resources
- targeting rules
- timing

---

### Application

Extend activation orchestration to:

- generate a backend-owned activation instance ID
- create an activation record when accepted
- store the activation instance in state

Update the activation result to include:
- activation instance ID (for accepted cases)

Application remains responsible for:
- orchestration
- calling domain validation
- writing to state

---

### State

Add a new activation store.

Create:

- `IAuthorityActivationStore`
- `InMemoryAuthorityActivationStore`

Activation record should include:

- ActivationInstanceId
- SessionId
- EntityId
- AbilityId
- CreatedAtUtc

Keep this intentionally minimal.

No lifecycle phase yet beyond "created".

Use thread-safe structures.

---

### Transport

Update accepted activation response to include:

- activation instance ID

Rejected response remains unchanged.

Transport must NOT:
- create activation instances
- mutate activation state directly

---

### Debug

Add:

- `GET /debug/activations`

Response should include:
- activation count
- list of activation records

This should allow you to:

- confirm activations are being created
- inspect session/entity relationships
- debug ordering issues

Keep it read-only and lightweight.

---

### Test Client

Update test client to:

- print activation instance ID from accepted response
- clearly separate activation requests and responses

Optional:
- allow multiple activations in one run if it stays simple

Do not overbuild client logic.

---

## Ownership

### Transport owns
- parsing activation command
- sending accepted/rejected messages

### Application owns
- generating activation instance IDs
- creating activation records
- orchestrating validation + state writes

### Domain owns
- validation rules only

### State owns
- activation storage
- thread-safe persistence of activation records

### Debug owns
- activation snapshot visibility

### Test Client owns
- observing and printing activation instance IDs

---

## State

### Reads
- session state
- entity state
- backend time

### Writes
- activation records (on accepted activation)

---

## Threading / timing concerns

- multiple activations may be created concurrently
- activation store must be thread-safe
- instance IDs must be unique
- backend time remains authoritative
- hosted service still does not mutate activation lifecycle yet

---

## Required contracts

### Outbound (update)

Update accepted message payload:

Add:
- activationInstanceId

Example:
- sessionId
- entityId
- abilityId
- activationInstanceId

Rejected message unchanged.

---

## Rules

- use moderate meaningful comments
- explain why activation instances exist
- do not narrate obvious code
- keep layers clean
- compile-ready only
- do not add timing/commit yet
- do not add cooldowns/resources yet
- do not add persistence yet

---

## Output expectations

Before generating code, explain:

1. what you are about to build
2. which layer owns it
3. what state it reads and writes
4. any threading/timing concerns

Then generate the Phase 03 code.

---

## Expected result

- accepted activations create backend-owned instances
- activation instance IDs are returned to client
- `/debug/activations` shows current activation records
- test client prints activation instance IDs
- system remains narrow and clean