# Phase 05 — Interrupted Activations and Integration-Facing Lifecycle Control

## Goal
Introduce backend-owned interruption for activation instances and emit interruption events when an activation is canceled before commit.

Phase 05 expands the lifecycle model from:
- Accepted
- Committed

to:
- Accepted
- Interrupted
- Committed

This phase focuses on non-happy-path lifecycle control so MMOAS integration can validate authoritative state changes before effects and animation layers are added.

---

## Why this phase exists

Phase 04 proved:
- accepted activations are stored in backend state
- backend-owned time advances eligible activations to committed
- committed events are delivered to connected sessions
- lifecycle state is inspectable through debug routes

However:
- all lifecycle progression is still happy-path
- there is no authoritative way to stop an activation before commit
- MMOAS integration needs at least one backend-driven negative lifecycle path before presentation systems are layered on top

Phase 05 introduces:
- interrupted activation state
- interruption reason codes/messages
- interruption event delivery
- prevention of commit after interruption

This phase improves lifecycle realism while keeping scope narrow.

---

## Build

### Domain
Domain remains intentionally narrow.

Do NOT add:
- cooldowns
- resources
- targeting
- effect resolution
- recovery/completion systems beyond what is needed for interruption

If helpful, add a tiny domain helper for interruption eligibility, but only if it improves clarity.

---

### Application
Add a lightweight activation interruption service or extend the activation application layer to support interrupting an existing activation instance.

Application should:
- validate that the activation instance exists
- validate that it is still interruptible
- write interrupted state to backend-owned activation storage
- return enough data for transport to send an interruption event

Application must NOT:
- deliver socket messages directly
- decide hosted timing advancement

---

### State
Extend activation state to support interruption.

Add fields needed for interruption, such as:
- InterruptionCode (nullable)
- InterruptedAtUtc (nullable)

Update activation phase model to include:
- Interrupted

Update the activation store to support:
- marking an activation interrupted
- ensuring interrupted activations do not later become committed
- one-shot transition behavior using thread-safe compare-and-swap updates

Keep the state shape minimal and inspectable.

Do not add history/persistence/pruning yet.

---

### Hosting
Hosted lifecycle advancement must respect interruption state.

When scanning activations:
- accepted activations whose commit time has arrived may commit
- interrupted activations must never commit
- committed activations must not change again in this phase

Hosting does NOT own deciding why an interruption happened.
It only respects lifecycle state already written by application/state.

---

### Transport
Add support for interruption event delivery.

Add:
- `transport.ability-interrupted`

Suggested payload:
- sessionId
- entityId
- abilityId
- activationInstanceId
- interruptionCode
- interruptedAtUtc

Also add a narrow inbound command to trigger interruption for local validation.

Suggested command:
- `transport.interrupt-ability`

Suggested payload:
- activationInstanceId
- interruptionCode

Keep it intentionally simple.
This is a validation/control path, not a production admin system.

Transport still must NOT mutate lifecycle directly.

---

### Debug
Extend `/debug/activations` to include interruption visibility.

Add:
- interruption code
- interrupted timestamp

Debug output should make it obvious whether an activation is:
- Accepted
- Committed
- Interrupted

Keep routes read-only and lightweight.

---

### Test Client
Update the test client to support an interruption scenario.

Suggested options:
- activate normally and wait for commit
- or activate and then immediately interrupt before commit

The interruption mode should:
1. hello
2. register entity
3. activate ability
4. capture activation instance id
5. send interrupt command
6. receive interrupted event
7. verify no commit event arrives for that activation

Keep this simple and repeatable.

---

## Ownership

### Transport owns
- parsing interruption command
- sending interruption event
- routing messages to connected sessions

### Application owns
- interrupt orchestration
- validation that an activation may still be interrupted
- writing interrupted state

### State owns
- interruption persistence
- one-shot thread-safe transition rules

### Hosting owns
- ensuring interrupted activations are never committed later

### Debug owns
- interruption visibility in activation snapshots

### Test Client owns
- exercising commit and interrupt scenarios from outside the service

---

## State

### Reads
- activation records
- backend time
- session linkage as needed for delivery

### Writes
- interrupted activation state
- interruption timestamps and codes

---

## Threading / timing concerns

- interruption may race with hosted lifecycle commit advancement
- activation transitions must remain one-shot and thread-safe
- an activation must end in exactly one terminal direction for this phase:
  - committed
  - interrupted
- backend state remains authoritative even if delivery fails
- transport delivery and lifecycle truth remain separate concerns

This race is the most important technical concern in this phase.

---

## Required lifecycle model

Update the lifecycle model to include:
- Accepted
- Committed
- Interrupted

Rules:
- Accepted may transition to Committed
- Accepted may transition to Interrupted
- Interrupted may not transition to Committed
- Committed may not transition to Interrupted in this phase

Keep it minimal.

---

## Required contracts

### Inbound
Add:
- `transport.interrupt-ability`

Suggested payload:
- activationInstanceId
- interruptionCode

### Outbound
Add:
- `transport.ability-interrupted`

Suggested payload:
- sessionId
- entityId
- abilityId
- activationInstanceId
- interruptionCode
- interruptedAtUtc

Keep all contracts explicit and boring.

Do not build a generic event framework.

---

## Suggested validation rules

An interruption should be accepted only if:
- activation instance exists
- activation is still in Accepted state
- interruption code is non-empty

Otherwise return a narrow rejection/error response.

Keep these rules simple.

---

## Rules
- use moderate meaningful comments
- explain interruption vs commit race concerns
- explain why backend state owns truth even if delivery fails
- do not narrate obvious code
- keep layers clean
- compile-ready only
- do not add cooldowns/resources yet
- do not add targeting yet
- do not add persistence yet
- do not add pruning/history yet

---

## Output expectations

Before generating code, explain:
1. what you are about to build
2. which layer owns it
3. what state it reads and writes
4. any threading/timing concerns

Then generate the Phase 05 code.

---

## Expected result

- activations can be interrupted before commit
- interrupted activations never commit later
- interruption event is delivered to connected session
- `/debug/activations` shows interrupted state clearly
- test client can validate both commit and interrupt paths
- system is ready for first serious MMOAS remote adapter integration