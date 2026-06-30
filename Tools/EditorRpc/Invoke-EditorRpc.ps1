param(
    [Parameter(Mandatory = $true)]
    [Alias("ToolName")]
    [string]$Method,

    [Alias("Arguments")]
    [string]$ArgumentsJson = "{}",

    [string]$ServerHost = "127.0.0.1",

    [int]$Port = 47841,

    [int]$TimeoutSeconds = 30,

    [string]$RequestId = "",

    [switch]$RawResponse
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Convert-ArgumentsJson {
    param(
        [string]$Json
    )

    if ([string]::IsNullOrWhiteSpace($Json)) {
        return @{}
    }

    try {
        return $Json | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "ArgumentsJson is not valid JSON: $($_.Exception.Message)"
    }
}

function Convert-EditorRpcResponse {
    param(
        [string]$ResponseLine
    )

    try {
        $response = $ResponseLine | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "RPC response is not valid JSON: $($_.Exception.Message)"
    }

    $payload = $null
    if ($null -ne $response.payload_json -and [string]$response.payload_json -ne "") {
        try {
            $payload = ([string]$response.payload_json) | ConvertFrom-Json -ErrorAction Stop
        }
        catch {
            $payload = [string]$response.payload_json
        }
    }

    return [pscustomobject]@{
        request_id = [string]$response.request_id
        success = [bool]$response.success
        message = [string]$response.message
        payload = $payload
        processed_at_utc = [string]$response.processed_at_utc
    }
}

function New-EditorRpcRequestJson {
    param(
        [string]$RpcMethod,
        [string]$ArgsJson,
        [string]$RpcRequestId
    )

    $requestIdValue = if ([string]::IsNullOrWhiteSpace($RpcRequestId)) {
        [guid]::NewGuid().ToString("N")
    }
    else {
        $RpcRequestId
    }

    $requestObject = [ordered]@{
        request_id = $requestIdValue
        method = $RpcMethod
        args = Convert-ArgumentsJson -Json $ArgsJson
    }

    return ($requestObject | ConvertTo-Json -Depth 32 -Compress)
}

$client = $null
$stream = $null
$reader = $null
$writer = $null
$script:DismissedUnityReloadDialog = $false

function Ensure-UnityDialogInterop {
    if ("UnityDialogInterop" -as [type]) {
        return
    }

    Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public static class UnityDialogInterop
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
"@
}

function Get-NativeWindowText {
    param(
        [IntPtr]$Handle
    )

    $builder = New-Object System.Text.StringBuilder 512
    [void][UnityDialogInterop]::GetWindowText($Handle, $builder, $builder.Capacity)
    return $builder.ToString()
}

function Get-NativeWindowClassName {
    param(
        [IntPtr]$Handle
    )

    $builder = New-Object System.Text.StringBuilder 256
    [void][UnityDialogInterop]::GetClassName($Handle, $builder, $builder.Capacity)
    return $builder.ToString()
}

function Get-NativeChildWindows {
    param(
        [IntPtr]$ParentHandle
    )

    $script:EditorRpcChildWindows = New-Object System.Collections.ArrayList
    $callback = [UnityDialogInterop+EnumWindowsProc]{
        param(
            [IntPtr]$childHandle,
            [IntPtr]$lParam
        )

        $text = Get-NativeWindowText -Handle $childHandle
        $className = Get-NativeWindowClassName -Handle $childHandle
        [void]$script:EditorRpcChildWindows.Add([pscustomobject]@{
            Handle = $childHandle
            Text = $text
            ClassName = $className
        })
        return $true
    }

    [void][UnityDialogInterop]::EnumChildWindows($ParentHandle, $callback, [IntPtr]::Zero)
    $children = @($script:EditorRpcChildWindows)
    $script:EditorRpcChildWindows = $null
    return $children
}

function Test-UnityReloadSceneDialogText {
    param(
        [string]$DialogText
    )

    if ([string]::IsNullOrWhiteSpace($DialogText)) {
        return $false
    }

    $englishReloadScene =
        $DialogText -match '(?i)reload' -and
        ($DialogText -match '(?i)scene' -or $DialogText -match '(?i)modified outside' -or $DialogText -match '(?i)external')
    $chineseReloadScene =
        ($DialogText -match '重新加载' -or $DialogText -match '重新载入') -and
        ($DialogText -match '场景' -or $DialogText -match '場景')

    return $englishReloadScene -or $chineseReloadScene
}

function Try-DismissUnityReloadSceneDialog {
    try {
        Ensure-UnityDialogInterop
    }
    catch {
        return $false
    }

    $script:EditorRpcUnityDialogDismissedNow = $false
    $script:EditorRpcTopWindows = New-Object System.Collections.ArrayList
    $callback = [UnityDialogInterop+EnumWindowsProc]{
        param(
            [IntPtr]$windowHandle,
            [IntPtr]$lParam
        )

        if (-not [UnityDialogInterop]::IsWindowVisible($windowHandle)) {
            return $true
        }

        [uint32]$processId = 0
        [void][UnityDialogInterop]::GetWindowThreadProcessId($windowHandle, [ref]$processId)
        if ($processId -eq 0) {
            return $true
        }

        try {
            $process = Get-Process -Id $processId -ErrorAction Stop
        }
        catch {
            return $true
        }

        if ($process.ProcessName -notlike "Unity*") {
            return $true
        }

        $title = Get-NativeWindowText -Handle $windowHandle
        $children = Get-NativeChildWindows -ParentHandle $windowHandle
        $dialogText = (@($title) + @($children | ForEach-Object { $_.Text })) -join "`n"
        if (-not (Test-UnityReloadSceneDialogText -DialogText $dialogText)) {
            return $true
        }

        $reloadButton = $children |
            Where-Object {
                $_.ClassName -eq "Button" -and
                ($_.Text -match '^(?i)&?Reload(\s.*)?$' -or $_.Text -match '^重新(加载|载入)')
            } |
            Select-Object -First 1

        if ($null -eq $reloadButton) {
            return $true
        }

        [void][UnityDialogInterop]::SendMessage($reloadButton.Handle, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero)
        $script:EditorRpcUnityDialogDismissedNow = $true
        $script:DismissedUnityReloadDialog = $true
        return $false
    }

    [void][UnityDialogInterop]::EnumWindows($callback, [IntPtr]::Zero)
    $script:EditorRpcTopWindows = $null

    if ($script:EditorRpcUnityDialogDismissedNow) {
        Start-Sleep -Milliseconds 500
        return $true
    }

    return $false
}

function Wait-TaskWithUnityDialogPump {
    param(
        [System.Threading.Tasks.Task]$Task,
        [int]$TimeoutSeconds
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while (-not $Task.IsCompleted) {
        [void](Try-DismissUnityReloadSceneDialog)

        $remainingMilliseconds = [int]($deadline - [DateTime]::UtcNow).TotalMilliseconds
        if ($remainingMilliseconds -le 0) {
            return $false
        }

        $sliceMilliseconds = [Math]::Min(500, [Math]::Max(50, $remainingMilliseconds))
        [void]$Task.Wait($sliceMilliseconds)
    }

    return $true
}

try {
    $requestJson = New-EditorRpcRequestJson -RpcMethod $Method -ArgsJson $ArgumentsJson -RpcRequestId $RequestId

    [void](Try-DismissUnityReloadSceneDialog)

    $client = New-Object System.Net.Sockets.TcpClient
    $connectTask = $client.ConnectAsync($ServerHost, $Port)
    if (-not (Wait-TaskWithUnityDialogPump -Task $connectTask -TimeoutSeconds $TimeoutSeconds)) {
        $message = "Timed out connecting to Editor RPC at $ServerHost`:$Port."
        if ($script:DismissedUnityReloadDialog) {
            $message += " A Unity scene reload dialog was dismissed while waiting."
        }

        throw $message
    }

    $client.ReceiveTimeout = $TimeoutSeconds * 1000
    $client.SendTimeout = $TimeoutSeconds * 1000

    $stream = $client.GetStream()
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    $writer = New-Object System.IO.StreamWriter($stream, $utf8, 4096, $true)
    $reader = New-Object System.IO.StreamReader($stream, $utf8, $false, 4096, $true)
    $writer.AutoFlush = $true

    $writer.WriteLine($requestJson)

    $readTask = $reader.ReadLineAsync()
    if (-not (Wait-TaskWithUnityDialogPump -Task $readTask -TimeoutSeconds $TimeoutSeconds)) {
        $message = "Timed out waiting for Editor RPC response."
        if ($script:DismissedUnityReloadDialog) {
            $message += " A Unity scene reload dialog was dismissed while waiting."
        }

        throw $message
    }

    $responseLine = $readTask.Result
    if ([string]::IsNullOrWhiteSpace($responseLine)) {
        throw "Editor RPC returned an empty response."
    }

    if ($RawResponse) {
        $responseLine
        exit 0
    }

    $responseObject = Convert-EditorRpcResponse -ResponseLine $responseLine
    $responseObject | ConvertTo-Json -Depth 32

    if (-not $responseObject.success) {
        exit 1
    }
}
catch {
    Write-Error $_
    exit 1
}
finally {
    if ($writer -ne $null) {
        $writer.Dispose()
    }

    if ($reader -ne $null) {
        $reader.Dispose()
    }

    if ($stream -ne $null) {
        $stream.Dispose()
    }

    if ($client -ne $null) {
        $client.Dispose()
    }
}
