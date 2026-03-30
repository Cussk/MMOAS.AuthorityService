# Phase 04 — Backend-Owned Startup Timing and Commit Events

## Goal
Introduce backend-owned lifecycle timing for activation instances and emit a commit event when an activation reaches its authoritative commit point.

Phase 04 transitions the system from:
- accepted activation instances existing in state

to:
- accepted activation instances progressing over backend-owned time

This is the first phase where the hosted lifecycle service performs real authority work.

---

## Why this phase exists

Phase 03 proved:
- activation requests can be accepted or rejected
- accepted activations create backend-owned activation instances
- activation instance IDs are visible to the client
- activation state is inspectable through debug routes

However:
- activation instances do not yet progress
- the backend is not yet advancing authority state over time
- the hosted service still acts only as a clock stub

Phase 04 introduces:
- explicit activation phase state
- startup timing owned by backend time
- commit event emission when startup completes

This is the first real validation that backend time, not client time, drives ability progression.

---

## Build

### Domain
Domain remains intentionally narrow.

Do NOT add:
- cooldowns
- resources
- targeting
- interruption rules
- recovery
- completion rules beyond what is strictly needed for this phase

If useful, add a very small domain helper for commit eligibility, but only if it improves clarity.

Do not overbuild lifecycle rules in Domain yet.

---

### Application
Application continues to own activation creation orchestration.

Update activation creation so that accepted activation records include:
- lifecycle phase
- created time
- startup duration or commit due time

Prefer storing an explicit commit due timestamp if that keeps the hosted service simpler.

Application must remain responsible for:
- generating backend-owned activation instance IDs
- creating new activation records
- writing accepted activations to state

Application should NOT advance lifecycle state over time.
That responsibility belongs to the hosted service in this phase.

---

### State
Extend activation state to support minimal lifecycle progression.

Add fields needed for this phase, such as:
- Phase
- CommitDueAtUtc
- CommittedAtUtc (nullable)

Use the smallest shape that cleanly supports:
- created/accepted state
- committed state
- backend timing checks

Update the activation store to support:
- adding activation records
- reading current activation snapshots
- updating activation phase for a known activation instance

Use thread-safe structures and compare-and-swap style updates where needed.

Do not add history, persistence, or cleanup systems yet.

---

### Hosting
This is the main focus of Phase 04.

`AuthorityLifecycleHostedService` now becomes responsible for:
- scanning activation state on a periodic interval
- finding activations eligible to commit
- transitioning them to committed
- publishing a commit event to connected sessions

The hosted service must use:
- backend `TimeProvider`
- state store abstractions
- transport/session notification abstractions only

Do not let the hosted service reach directly into WebSocket details.

If needed, introduce an application or transport-facing notifier abstraction so lifecycle code can emit outbound events without owning socket implementation details.

The hosted service should remain cancellation-safe.

---

### Transport
Transport must support outbound commit notifications.

Add:
- `transport.ability-committed`

This event should include:
- sessionId
- entityId
- abilityId
- activationInstanceId
- committedAtUtc

Transport may expose a lightweight notifier abstraction used by the hosted service.

Transport still must NOT:
- decide commit timing
- mutate activation lifecycle directly

It only delivers outbound messages.

---

### Debug
Extend activation debug visibility so lifecycle state is inspectable.

Update `/debug/activations` to include:
- current phase
- created time
- commit due time
- committed time if present

This route should make it easy to confirm:
- newly accepted activations are pending commit
- committed activations have transitioned correctly

Keep it read-only and lightweight.

---

### Test Client
Update the test client to:
- wait for and print the commit event after activation acceptance
- clearly distinguish accepted vs committed events
- remain easy to run from Rider

The test client should now validate this flow:

1. hello
2. register entity
3. activate ability
4. receive accepted response with activation instance ID
5. receive committed event later for that same activation instance ID

Do not add heavy client-side state management.

---

## Ownership

### Transport owns
- outbound delivery of commit notifications
- message envelope serialization
- socket session routing

### Application owns
- creation of accepted activation records with lifecycle timing metadata

### State owns
- activation lifecycle record persistence
- thread-safe lifecycle updates

### Hosting owns
- backend-timed phase advancement
- commit eligibility checks
- invoking outbound commit notification

### Debug owns
- lifecycle visibility through activation snapshot routes

### Test Client owns
- observing accepted + committed flow from outside the service

---

## State

### Reads
- activation records
- session records
- backend time

### Writes
- new accepted activation records
- commit transition updates for eligible activations
- committed timestamps

---

## Threading / timing concerns

- multiple activations may be created concurrently
- lifecycle hosted service may update activations while debug endpoints read snapshots
- activation store updates must be thread-safe
- commit should happen once per activation instance
- backend time remains authoritative
- transport delivery may fail if a session disconnects before commit; the lifecycle transition should still remain authoritative in state

This last point is important:
state transition and network delivery are related, but not the same thing.
The backend must not depend on a live client socket to own lifecycle truth.

---

## Required lifecycle model

Add the smallest explicit lifecycle model that supports this phase.

Suggested minimal states:
- Accepted
- Committed

If you want slightly more clarity, you may use:
- Created
- Committed

But keep it minimal.

The important thing is:
- new accepted activation enters initial phase
- hosted service transitions it to committed after startup time
- committed activations do not re-commit

---

## Required contracts

### Outbound
Add:
- `transport.ability-committed`

Suggested payload:
- sessionId
- entityId
- abilityId
- activationInstanceId
- committedAtUtc

### Existing accepted payload
Keep accepted payload including:
- sessionId
- entityId
- abilityId
- activationInstanceId

Do not overdesign a full event bus contract yet.

---

## Suggested timing model
Keep timing intentionally simple.

Recommended approach:
- every accepted activation gets a fixed startup delay
- when created, compute `CommitDueAtUtc = UtcNow + StartupDelay`
- hosted service checks for activations where:
    - phase is Accepted
    - current backend time >= CommitDueAtUtc
- hosted service marks activation committed and emits commit event

A fixed startup delay configured in code is fine for this phase.

Optional:
- use a small config constant such as 500ms or 1000ms
- only introduce external config if it stays simple

---

## Optional abstraction
If needed for clean boundaries, introduce something like:

- `IAuthorityEventNotifier`
  or
- `IAuthoritySessionNotifier`

Its job would be:
- send outbound events to a given session
- hide socket details from the hosted service

This is encouraged if it prevents Hosting from depending on concrete WebSocket handler internals.

---

## Rules
- use moderate meaningful comments
- explain why backend-owned timing matters
- explain why lifecycle transition and socket delivery are separate concerns
- do not narrate obvious code
- keep layers clean
- compile-ready only
- do not add cooldowns/resources yet
- do not add interruption yet
- do not add persistence yet
- do not add cleanup/pruning unless strictly necessary

---

## Output expectations

Before generating code, explain:
1. what you are about to build
2. which layer owns it
3. what state it reads and writes
4. any threading/timing concerns

Then generate the Phase 04 code.

---

## Expected result

- accepted activations include timing metadata
- hosted service advances eligible activations to committed
- committed event is delivered to connected client session
- `/debug/activations` shows lifecycle state clearly
- test client prints accepted and committed events for the same activation instance
- system remains narrow and clean