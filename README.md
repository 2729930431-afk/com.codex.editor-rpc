# Editor RPC

Editor RPC is a reusable Unity editor plugin for controlling the currently open Unity Editor from a local terminal or an external AI agent.

It runs a TCP server inside the editor on loopback only (`127.0.0.1`) and executes requests on the Unity editor main thread. The default port is `47841`.

## Install

Use Unity Package Manager:

1. Open `Window > Package Manager`.
2. Choose `+ > Add package from disk...`.
3. Select `D:\unity\unity插件\com.codex.editor-rpc\package.json`.

After the package imports, open `AI Tools > Editor RPC` to inspect status, change the port, or copy a client command.

## Client

The bundled PowerShell client is here:

```powershell
D:\unity\unity插件\com.codex.editor-rpc\Tools\EditorRpc\Invoke-EditorRpc.ps1
```

Example:

```powershell
powershell -ExecutionPolicy Bypass -File "D:\unity\unity插件\com.codex.editor-rpc\Tools\EditorRpc\Invoke-EditorRpc.ps1" -Method list_methods
```

When called from an installed package, the Editor RPC window resolves the package path and copies a command pointing at the installed client script.

## Generic RPC Surface

This package includes only project-agnostic editor automation:

- system state, play mode, menu execution, and static method invocation
- asset search, selection, refresh, reimport, prefab edits, and serialized property edits
- scene open/close/save, hierarchy search, GameObject creation/deletion, transform edits, component edits, and scene-object method invocation
- Unity console read and clear
- AnimatorController inspection and basic graph edits
- UGUI discovery, inspection, creation, RectTransform edits, event binding inspection, and batch UI operations
- type search, method search, menu search, batch execution, workspace validation, and generic scene rendering statistics
- material batch assignment across prefab assets or scene hierarchies

Project-specific database tools and Scavenger rendering pipeline controls were intentionally excluded.

## Protocol

- Transport: TCP over `127.0.0.1`
- Framing: one JSON line per request and one JSON line per response
- Request fields: `request_id`, `method`, `args`
- Response fields: `request_id`, `success`, `message`, `payload_json`, `processed_at_utc`

Arguments are passed as JSON through `-ArgumentsJson`:

```powershell
powershell -ExecutionPolicy Bypass -File "D:\unity\unity插件\com.codex.editor-rpc\Tools\EditorRpc\Invoke-EditorRpc.ps1" `
  -Method find_assets `
  -ArgumentsJson '{"filter":"t:Scene","limit":20}'
```

For multiple open Unity projects, set different ports in each project's `AI Tools > Editor RPC` window, then pass `-Port` to the client.

## Notes

The server is enabled by default through `EditorPrefs`. It listens only on loopback and is meant for trusted local automation.
