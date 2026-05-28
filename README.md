# MMOAS Authority Service

Backend authority validation service for the Massive Multiplayer Online Ability System (MMOAS).

This repository is a narrow ASP.NET Core service used to prove MMOAS can send ability intent to an external authority, receive backend-owned lifecycle events, and keep Unreal runtime state synchronized with server truth.

It is not a full MMO backend. It intentionally avoids accounts, persistence, inventory, world simulation, deployment infrastructure, and broad gameplay systems.

## How It Relates To MMOAS

MMOAS is the Unreal-side ability runtime. It owns local ability instances, prediction-facing phase changes, targeting, follow-up input, cancel/trim behavior, and gameplay-side debug logs.

This service owns the remote authority slice:

- WebSocket session handshake.
- Backend entity registration.
- Ability activation acceptance or rejection.
- Backend-generated activation instance IDs.
- Backend-owned activation timing.
- Commit events.
- Interrupt-before-commit events.
- Read-only debug snapshots for sessions, entities, and activations.

The current MMOAS remote integration path is `UMMOASRemoteAuthorityAdapter` in the Unreal project. That adapter connects to this service at:

```text
ws://localhost:5274/transport/ws
```

The normal happy path is:

1. MMOAS creates a local runtime ability instance.
2. MMOAS enters `WaitingForAuthority`.
3. MMOAS sends `transport.activate-ability`.
4. This service validates the session and registered entity.
5. This service responds with `transport.ability-accepted` and a backend activation ID.
6. MMOAS maps that backend activation ID to the local runtime instance and enters `Startup`.
7. This service advances backend time and sends `transport.ability-committed`.
8. MMOAS treats that commit as authoritative and enters `Committed`.

The interruption path is:

1. MMOAS sends `transport.interrupt-ability` for a known backend activation ID.
2. This service marks the accepted activation as `Interrupted` if it has not already committed.
3. This service sends `transport.ability-interrupted`.
4. MMOAS treats the interrupted event as authoritative and does not apply a later commit for that activation.

## Current Scope

Implemented:

- ASP.NET Core WebSocket transport.
- Explicit protocol envelopes.
- Session hello/ready flow.
- Entity registration.
- Ability activation request flow.
- Accepted, rejected, committed, and interrupted authority messages.
- In-memory session, entity, and activation stores.
- Hosted lifecycle advancer with backend-owned timing.
- Debug HTTP endpoints.
- xUnit tests.
- Standalone console test client.

Out of scope:

- Authentication.
- Databases or persistence.
- Cooldowns, resources, targeting validation, damage, effects, inventory, or world simulation.
- Production reconnect/retry behavior.
- Deployment automation.

## Requirements

- .NET 9 SDK.
- For MMOAS integration testing: Unreal Engine with the `MassiveMultiplayerOnlineAbilitySystem` project.

Check your SDK:

```powershell
dotnet --info
```

## Quick Start

From this repository root:

```powershell
dotnet restore .\MMOAS.AuthorityService.sln
dotnet build .\MMOAS.AuthorityService.sln
dotnet test .\MMOAS.AuthorityService.sln
dotnet run --project .\MMOAS.AuthorityService.csproj --launch-profile http
```

The HTTP launch profile binds the service to:

```text
http://localhost:5274
```

The WebSocket transport endpoint is:

```text
ws://localhost:5274/transport/ws
```

## Local Smoke Test Without Unreal

Start the service in one terminal:

```powershell
dotnet run --project .\MMOAS.AuthorityService.csproj --launch-profile http
```

In a second terminal, run the console test client:

```powershell
dotnet run --project .\MMOAS.AuthorityService.TestClient\MMOAS.AuthorityService.TestClient.csproj
```

The default client flow is:

1. Connect to `ws://localhost:5274/transport/ws`.
2. Send `transport.hello`.
3. Send `transport.register-entity`.
4. Send `transport.activate-ability`.
5. Receive `transport.ability-accepted`.
6. Wait for `transport.ability-committed`.

Run an interruption smoke test:

```powershell
dotnet run --project .\MMOAS.AuthorityService.TestClient\MMOAS.AuthorityService.TestClient.csproj -- --interrupt-after-activate
```

Run a rejection smoke test by activating before registration:

```powershell
dotnet run --project .\MMOAS.AuthorityService.TestClient\MMOAS.AuthorityService.TestClient.csproj -- --activate-before-register
```

Use a custom ability ID:

```powershell
dotnet run --project .\MMOAS.AuthorityService.TestClient\MMOAS.AuthorityService.TestClient.csproj -- --ability ability.basic
```

## Testing With MMOAS

1. Start this service:

```powershell
dotnet run --project .\MMOAS.AuthorityService.csproj --launch-profile http
```

2. Confirm the service is alive:

```powershell
Invoke-RestMethod http://localhost:5274/debug/health
```

3. Open the Unreal project:

```text
MMOAbilitySystem.uproject
```

4. On the actor or pawn that owns `UMMOASAbilityComponent`, set:

```text
AuthorityAdapterClass = UMMOASRemoteAuthorityAdapter
```

5. Leave the adapter URL at the default unless you changed the backend port:

```text
ws://localhost:5274/transport/ws
```

6. Press Play in Unreal.

Expected MMOAS log flow:

- Remote adapter connects to the authority transport.
- Adapter sends hello.
- Backend returns `transport.ready`.
- Adapter sends entity registration.
- Backend returns `transport.entity-registered`.
- Ability activation enters `WaitingForAuthority`.
- Backend returns `transport.ability-accepted`.
- MMOAS enters `Startup`.
- Backend later sends `transport.ability-committed`.
- MMOAS enters `Committed`.

For cancel/trim testing, trigger a local cancel/trim before the backend commit arrives. MMOAS should send `transport.interrupt-ability`; the backend should return `transport.ability-interrupted` if the activation is still accepted.

## Debug Endpoints

With the service running:

```powershell
Invoke-RestMethod http://localhost:5274/debug/health
Invoke-RestMethod http://localhost:5274/debug/snapshot
Invoke-RestMethod http://localhost:5274/debug/sessions
Invoke-RestMethod http://localhost:5274/debug/activations
```

Useful checks while MMOAS is running:

- `/debug/sessions` should show a connected session with hello completed and a registered entity.
- `/debug/activations` should show accepted activations, backend commit due time, committed timestamps, and interruption fields.
- MMOAS logs should use the same backend activation IDs shown by `/debug/activations`.

## Protocol Summary

Inbound messages:

- `transport.hello`
- `transport.register-entity`
- `transport.activate-ability`
- `transport.interrupt-ability`

Outbound messages:

- `transport.ready`
- `transport.entity-registered`
- `transport.ability-accepted`
- `transport.ability-rejected`
- `transport.ability-committed`
- `transport.ability-interrupted`
- `transport.error`

All WebSocket messages use a JSON envelope:

```json
{
  "messageType": "transport.activate-ability",
  "version": 1,
  "requestId": "client-request-id",
  "payload": {
    "abilityId": "ability.basic"
  }
}
```

## Project Layout

```text
Application/                         Application orchestration services
Composition/                         Dependency injection registration
Debug/                               HTTP debug endpoints and contracts
Domain/                              Narrow authority validation rules
Hosting/                             Backend lifecycle hosted service
State/                               Thread-safe in-memory stores
Transport/                           WebSocket transport and wire contracts
MMOAS.AuthorityService.Tests/        xUnit tests
MMOAS.AuthorityService.TestClient/   Console smoke-test client
Docs/                                Phase notes and architecture rules
```

## Development Notes

- Backend time is authoritative.
- Backend activation instance IDs are authoritative after acceptance.
- Transport delivers messages but does not own gameplay state decisions.
- State transitions are in-memory and thread-safe.
- Accepted activations may transition once to either `Committed` or `Interrupted`.
- Interrupt and commit can race near the commit boundary; final backend state is the source of truth.

