# Phase 02 — Ability Activation Skeleton, Session Debugging, and Test Client

## Goal
Add the first narrow ability activation authority slice and create a simple external test client that can manually drive the service protocol without Unreal.

Phase 02 should:
- extend the protocol beyond hello/register
- establish the first ability activation command/event flow
- improve debugging visibility for active sessions
- add a lightweight standalone test client for repeatable manual testing

This phase remains intentionally narrow.

We are still NOT implementing:
- full combat resolution
- effect application
- cooldown groups
- persistence
- auth/login
- databases
- deployment complexity
- world simulation

---

## Why this phase exists
Phase 01 created a usable transport/session foundation, but manual validation is still awkward without a dedicated client.

Phase 02 adds:
- a simple repeatable transport harness
- the first ability activation authority contract
- better local debugging support

The test client is part of the architecture workflow, not a throwaway extra.

It should help validate:
- hello flow
- entity registration
- activation request flow
- error handling
- request/response correlation

---

## Build

### Transport
Extend WebSocket message handling to support a narrow activation request flow.

Add:
- activate-ability command
- ability-accepted event
- ability-rejected event

Transport still owns:
- message parsing
- request routing
- response serialization
- socket lifetime

Transport must NOT:
- directly decide authority outcomes
- directly mutate gameplay state

---

### Application
Add a lightweight application service or handler for ability activation orchestration.

This service should:
- validate session prerequisites
- validate entity/session relationship as needed
- invoke narrow authority validation
- produce accepted or rejected results

Keep it intentionally small and compile-ready.

---

### Domain
Phase 02 introduces the first narrow domain validation slice.

Add minimal activation validation for:
- session exists
- hello completed
- session has a registered entity
- ability id is non-empty

Optional:
- maintain a tiny in-memory set of allowed ability ids if it stays simple
- only do this if it cleanly improves validation

Do NOT implement:
- cooldowns
- resources
- targeting rules
- effect resolution
- commit timing

This phase is about the activation request boundary, not full authority logic.

---

### State
State should remain narrow.

Continue using:
- entity store
- session store

Add minimal activation state only if needed.

If activation state is added, keep it to a very small inspectable record such as:
- request id
- entity id
- ability id
- accepted/rejected result
- server timestamp

Only introduce activation state if it clearly helps debugging or future phases.

Do not overbuild this.

---

### Debug
Add better visibility for manual testing.

Required:
- session snapshot debug route
- existing entity snapshot continues to work

Optional:
- activation snapshot route if activation state is introduced and it remains simple

Keep debug routes read-only and lightweight.

---

### Test Client
Add a separate lightweight console client project to the solution.

The test client should:
- connect to the backend WebSocket endpoint
- send hello
- send register-entity
- send activate-ability
- print outbound requests and inbound responses clearly
- support simple manual configuration for URL and ability id

The test client should remain intentionally small.
It is a manual validation harness, not a framework.

Preferred shape:
- .NET console app in the same solution
- small command sequence runner
- no heavy UI
- no dependency sprawl

---

## Ownership

### Transport owns
- activation command envelope parsing
- activation response envelope sending
- WebSocket routing for the new message type

### Application owns
- session/registration prerequisite checks for activation
- orchestration of activation request evaluation

### Domain owns
- narrow activation acceptance/rejection rules
- no transport dependencies

### State owns
- session/entity persistence
- optional tiny activation records only if truly needed

### Debug owns
- session snapshot visibility
- optional activation snapshot if introduced

### Test Client owns
- manual local protocol driving
- human-readable request/response logging
- repeatable smoke test flow outside Unreal

---

## State

### Reads
- session state
- entity association for session
- backend time
- optional activation records if introduced

### Writes
- optional activation records
- no lifecycle timing yet
- no cooldown/resource state yet

### Threading / timing concerns
- multiple WebSocket clients may connect concurrently
- session and entity stores must remain thread-safe
- test client is external and should not rely on in-process access
- backend time remains authoritative through `TimeProvider`
- hosted lifecycle service still exists but does not yet own activation progression

---

## Required contracts

### Inbound
Add:
- activate-ability command

Suggested fields:
- ability id
- optional client request metadata only if useful
- keep payload simple

### Outbound
Add:
- ability-accepted event
- ability-rejected event

Suggested accepted payload:
- session id
- entity id
- ability id
- server UTC timestamp

Suggested rejected payload:
- code
- message
- ability id if helpful

Keep contracts explicit and boring.

Use:
- message type
- version
- request id
- server UTC timestamp in outbound envelope

Do not overdesign generic transport infrastructure.

---

## Test client requirements
Create a separate console project in the solution.

Suggested responsibilities:
- configurable backend URL
- connects to `/transport/ws`
- sends:
  1. hello
  2. register-entity
  3. activate-ability
- prints received envelopes
- exits cleanly

Optional:
- a mode that intentionally sends activate-ability before registration to verify failure paths
- only add this if it stays simple

The client should be easy to run from Rider.

---

## Rules
- use moderate meaningful comments
- explain backend-specific concepts where helpful
- do not narrate obvious code
- keep boundaries clean
- compile-ready only
- no pseudocode
- do not add cooldowns/resources yet
- do not add commit timing yet
- do not add persistence yet
- do not add auth/login yet

---

## Output expectations
Before generating code, explain:
1. what you are about to build
2. which layer owns it
3. what state it reads and writes
4. any threading/timing concerns

Then generate the Phase 02 code.

Expected result:
- session debug visibility improved
- first activation request protocol slice exists
- accepted/rejected activation responses exist
- standalone console test client can drive hello/register/activate flow
- tests cover new services/state where appropriate