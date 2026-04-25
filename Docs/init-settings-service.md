# Init Settings Service Guide

## Goal

This document explains how SG Unity SDK loads launcher configuration and
how Unity app developers can safely read launcher-provided values at runtime.

## What The Service Does

`InitSettingsService` loads `launcher.config`, converts each root JSON object
into a named page, and exposes typed getters through `InitSettingsPage`.

Main points:

- Service type: `SGUnitySDK.Initialization.InitSettingsService`
- Contract: `IHMSService`
- Build roles: `Client` and `LaunchedClient`
- Locator access: `HMSLocator.Get<InitSettingsService>()`
- Page and field lookup: case-insensitive

## Where `launcher.config` Is Read

The service resolves the file path based on runtime mode.

- Editor runtime:
  - `<ProjectRoot>/SGUnitySDK/launcher.config`
  - Implemented by combining `Application.dataPath`, `..`, `SGUnitySDK`,
    and `launcher.config`
- Player runtime (standalone/launched):
  - `<ExecutableBaseDirectory>/launcher.config`
  - Implemented through `AppDomain.CurrentDomain.BaseDirectory`

## Expected JSON Shape

The file must be a JSON object where each root property is a page.

```json
{
  "window": {
    "window_mode": "fullscreen",
    "background": true,
    "select_monitor": 1,
    "background_obs_discoverable": true
  },
  "obs": {
    "host": "127.0.0.1",
    "port": "4455",
    "live_sync": true,
    "sync_interval": "0.2"
  },
  "diagnostics": {
    "debug": false
  }
}
```

Important behavior:

- Only root properties that are JSON objects are loaded as pages.
- Non-object root values are ignored.

## API Surface

### Service-level methods

- `HasPage(pageName)`
- `GetPageNames()`
- `GetPage(pageName)`

`GetPage` never returns `null`; missing pages return an empty wrapper.

### Page-level methods

- `HasField(key)`
- `GetFieldKeys()`
- `GetText(key, defaultValue)`
- `GetBool(key, defaultValue)`
- `GetInt(key, defaultValue)`
- `GetFloat(key, defaultValue)`

## Type Conversion Rules

The typed getters apply tolerant parsing and fallback defaults.

- `GetText`
  - Strings are returned as-is.
  - Primitive values are converted with invariant culture.
  - Complex JSON tokens are returned compacted (`Formatting.None`).
- `GetBool`
  - Accepts `bool` directly.
  - Integer: `0` is `false`, non-zero is `true`.
  - Float: epsilon-based non-zero check.
  - String: parses `true/false` or integer-like text.
  - Invalid values return `defaultValue`.
- `GetInt`
  - Accepts integer directly.
  - Float is rounded via `Math.Round`.
  - String is parsed with invariant culture.
  - Invalid values return `defaultValue`.
- `GetFloat`
  - Accepts float or integer directly.
  - String is parsed with invariant culture.
  - Invalid values return `defaultValue`.

## Recommended Usage Pattern In Unity

Use the project convention:

- Cache service references in `Awake`
- Apply configuration in `Start`

```csharp
using System;
using HMSUnitySDK;
using SGUnitySDK.Initialization;
using UnityEngine;

public sealed class LauncherConfigConsumer : MonoBehaviour
{
    private InitSettingsService _initSettings;

    private void Awake()
    {
        try
        {
            _initSettings = HMSLocator.Get<InitSettingsService>();
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                $"InitSettingsService is unavailable: {ex.Message}");
        }
    }

    private void Start()
    {
        if (_initSettings == null)
        {
            return;
        }

        var obsPage = _initSettings.GetPage("obs");
        var host = obsPage.GetText("host", "127.0.0.1");
        var port = obsPage.GetInt("port", 4455);
        var syncInterval = obsPage.GetFloat("sync_interval", 0.2f);

        var diagnosticsPage = _initSettings.GetPage("diagnostics");
        var debugMode = diagnosticsPage.GetBool("debug", false);

        ApplyLauncherConfig(host, port, syncInterval, debugMode);
    }

    private static void ApplyLauncherConfig(
        string host,
        int port,
        float syncInterval,
        bool debugMode)
    {
        Debug.Log(
            $"Launcher config -> host={host}, port={port}, " +
            $"sync={syncInterval}, debug={debugMode}");
    }
}
```

## Failure Model And Defaults

`InitSettingsService` is fail-safe by design:

- Missing path, file not found, empty file, or parse failure only logs warnings.
- The game continues with empty settings.
- Missing page/field never throws through this API.
- Always provide explicit defaults in typed getters.

## Practical Checklist

Before depending on launcher config values in gameplay systems:

1. Confirm the correct `launcher.config` exists in the expected runtime path.
2. Confirm page names and field keys match exactly.
3. Use typed getters with safe defaults for all reads.
4. Add logs around critical values during integration testing.
5. Keep fallback behavior deterministic when launcher config is unavailable.
