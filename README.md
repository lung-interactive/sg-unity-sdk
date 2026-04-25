# SG Unity SDK

## Purpose

SG Unity SDK connects your Unity game to SG launcher/runtime services.

Main capabilities:

- launcher initialization and authentication handshake
- crowd action ingestion from launcher events
- lightweight event bus for gameplay integration
- HTTP request/response helpers with typed payload reading

## Runtime API Surface

Primary namespaces:

- `SGUnitySDK`
- `SGUnitySDK.Events`
- `SGUnitySDK.Http`

Core runtime entry points:

- `LauncherInitializer` (auto-bootstraps launcher/auth flow)
- `CrowdActionsHandler` (listens to `crowd.action` launcher event)
- `EventBus<T>` and `EventBinding<T>` (in-game event wiring)
- `SGHttpRequest` and `SGHttpResponse` (HTTP utility layer)

## Init Settings

For launcher configuration (`launcher.config`) loading and typed value
consumption, see:

- [Init Settings Service Guide](./Docs/init-settings-service.md)

## Quick Start

### 1. Ensure launcher initialization runs

`LauncherInitializer` runs before first scene load through
`RuntimeInitializeOnLoadMethod` and sets up launcher interop and auth.

If initialization succeeds:

- auth data is fetched and registered
- `sg-client.ready` is emitted

If it fails:

- status/error is logged with `SGLogger`

### 2. Handle crowd actions in gameplay code

Register an event binding for `CrowdActionTriggered`:

```csharp
using SGUnitySDK;
using SGUnitySDK.Events;
using UnityEngine;

public sealed class CrowdActionListener : MonoBehaviour
{
	private EventBinding<CrowdActionTriggered> _binding;

	private void OnEnable()
	{
		_binding = new EventBinding<CrowdActionTriggered>(OnCrowdAction);
		EventBus<CrowdActionTriggered>.Register(_binding);
	}

	private void OnDisable()
	{
		EventBus<CrowdActionTriggered>.Deregister(_binding);
	}

	private void OnCrowdAction(CrowdActionTriggered evt)
	{
		Debug.Log($"Action: {evt.Action.identifier}");
	}
}
```

### 3. Read action arguments safely

`ProcessedArgument` provides typed extraction through `TryGetValue<T>`.

```csharp
foreach (var arg in evt.Action.processed_arguments)
{
	if (arg.key == "amount" && arg.TryGetValue<int>(out var amount))
	{
		Debug.Log($"Amount: {amount}");
	}
}
```

## Crowd Action Data Contracts

### `CrowdAction`

- `identifier`: stable action id (routing key)
- `name`: display/action name
- `processed_arguments`: argument list
- `metadata`: contextual metadata (agent/platform/additional_data)

### `CrowdActionMetadata`

- `agent`: source agent name
- `platform`: source platform
- `additional_data`: key/value bag supporting string/int/float/bool

## Event Model

`CrowdActionsHandler` listens to launcher socket event `crowd.action`.
Each incoming payload is converted to `CrowdAction` and raised as:

- `EventBus<CrowdActionTriggered>.Raise(...)`

Your game should subscribe to `CrowdActionTriggered` and translate
action identifiers into gameplay operations.

## HTTP Utility Usage

`SGHttpRequest` supports fluent setup:

```csharp
using SGUnitySDK.Http;

var response = await SGHttpRequest
	.To("https://example.com/api/resource", HttpMethod.Get)
	.SetBearerAuth(token)
	.AddQueryEntry("page", "1")
	.SendAsync();

if (response.Success)
{
	var data = response.ReadBodyData<MyDto>();
}
```

Error handling pattern:

- inspect `response.Success` and `response.ResponseCode`
- use `ReadErrorBody()` when available
- handle `RequestFailedException` in service-level flows

## Logging

Use `SGLogger` instead of `Debug.Log` for SDK-related logs.
It prefixes output with `-SGUnitySDK |` for filtering and diagnostics.

## Editor Runtime Notes

When running in Editor runtime mode, initializer can use a dummy interop
socket for local iteration, including mocked auth payload handling.

This allows gameplay-side crowd action and auth flow development without
requiring full launcher runtime each time.

## Integration Checklist

Before shipping integration:

1. confirm launcher initialization completes without exceptions
2. verify `CrowdActionTriggered` subscription lifecycle is correct
3. verify action identifier mapping to gameplay handlers
4. validate typed argument parsing for all supported action inputs
5. validate HTTP failure paths and user feedback/logging
