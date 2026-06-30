using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace EditorRpc
{
    [InitializeOnLoad]
    public static class EditorRpcService
    {
        private const string PackageName = "com.codex.editor-rpc";
        private const string EnabledEditorPrefKey = "EditorRpc.Enabled";
        private const string PortEditorPrefKey = "EditorRpc.Port";
        private const int DefaultPortValue = 47841;
        private const int MinPortValue = 1024;
        private const int MaxPortValue = 65535;
        private const int MaxRequestsPerEditorUpdate = 4;
        private const int RequestReadTimeoutMilliseconds = 15000;
        private const int RequestExecutionTimeoutSeconds = 180;

        private static readonly ConcurrentQueue<PendingRpcRequest> PendingRequests = new ConcurrentQueue<PendingRpcRequest>();
        private static readonly object StateLock = new object();

        private static TcpListener _listener;
        private static CancellationTokenSource _listenerCancellation;
        private static bool _startRequested;
        private static bool _isRunning;
        private static bool _isBootstrapped;
        private static int _activePort;
        private static string _lastProcessedRequestId = string.Empty;
        private static string _lastError = string.Empty;
        private static DateTime _lastStartedUtc = DateTime.MinValue;
        private static int _processedRequestCount;

        static EditorRpcService()
        {
            Bootstrap();
        }

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            Bootstrap();
        }

        private static void Bootstrap()
        {
            if (_isBootstrapped)
            {
                return;
            }

            _isBootstrapped = true;
            EditorRpcMethodExecutor.Initialize();

            if (!EditorPrefs.HasKey(EnabledEditorPrefKey))
            {
                EditorPrefs.SetBool(EnabledEditorPrefKey, true);
            }

            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload -= StopServer;
            AssemblyReloadEvents.beforeAssemblyReload += StopServer;
            EditorApplication.quitting -= StopServer;
            EditorApplication.quitting += StopServer;
            _startRequested = true;
            Debug.Log("Editor RPC bootstrap requested.");
        }

        public static string ProjectRoot
        {
            get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); }
        }

        public static string ClientScriptPath
        {
            get
            {
                var legacyClientPath = Path.Combine(ProjectRoot, "Tools", "EditorRpc", "Invoke-EditorRpc.ps1");
                if (File.Exists(legacyClientPath))
                {
                    return legacyClientPath;
                }

                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/" + PackageName + "/package.json");
                if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.resolvedPath))
                {
                    return Path.Combine(packageInfo.resolvedPath, "Tools", "EditorRpc", "Invoke-EditorRpc.ps1");
                }

                return Path.Combine(ProjectRoot, "Packages", PackageName, "Tools", "EditorRpc", "Invoke-EditorRpc.ps1");
            }
        }

        public static string ClientToolDirectory
        {
            get { return Path.GetDirectoryName(ClientScriptPath); }
        }

        public static bool IsEnabled
        {
            get { return EditorPrefs.GetBool(EnabledEditorPrefKey, true); }
            set
            {
                EditorPrefs.SetBool(EnabledEditorPrefKey, value);
                if (value)
                {
                    _startRequested = true;
                }
                else
                {
                    StopServer();
                }
            }
        }

        public static int Port
        {
            get
            {
                var savedPort = EditorPrefs.GetInt(PortEditorPrefKey, DefaultPortValue);
                return ClampPort(savedPort);
            }
            set
            {
                var clampedPort = ClampPort(value);
                if (clampedPort == Port)
                {
                    return;
                }

                EditorPrefs.SetInt(PortEditorPrefKey, clampedPort);
                if (IsRunning)
                {
                    RestartServer();
                }
                else if (IsEnabled)
                {
                    _startRequested = true;
                }
            }
        }

        public static string Endpoint
        {
            get { return "tcp://127.0.0.1:" + EffectivePort.ToString(); }
        }

        public static int EffectivePort
        {
            get { return _activePort > 0 ? _activePort : Port; }
        }

        public static bool IsRunning
        {
            get { return _isRunning; }
        }

        public static int PendingRequestCount
        {
            get { return PendingRequests.Count; }
        }

        public static int ProcessedRequestCount
        {
            get { return _processedRequestCount; }
        }

        public static string LastProcessedRequestId
        {
            get { return _lastProcessedRequestId; }
        }

        public static string LastError
        {
            get { return _lastError; }
        }

        public static string LastStartedUtc
        {
            get { return _lastStartedUtc == DateTime.MinValue ? string.Empty : _lastStartedUtc.ToString("u"); }
        }

        public static void RestartServer()
        {
            StopServer();
            if (IsEnabled)
            {
                _startRequested = true;
            }
        }

        public static void StopServer()
        {
            TcpListener listenerToStop = null;
            CancellationTokenSource cancellationToStop = null;

            lock (StateLock)
            {
                listenerToStop = _listener;
                cancellationToStop = _listenerCancellation;
                _listener = null;
                _listenerCancellation = null;
                _isRunning = false;
                _activePort = 0;
                _startRequested = false;
            }

            if (cancellationToStop != null)
            {
                try
                {
                    cancellationToStop.Cancel();
                }
                catch
                {
                }

                cancellationToStop.Dispose();
            }

            if (listenerToStop != null)
            {
                try
                {
                    listenerToStop.Stop();
                }
                catch
                {
                }
            }

            FailPendingRequests("RPC server stopped.");
        }

        private static void Update()
        {
            if (!IsEnabled)
            {
                if (IsRunning)
                {
                    StopServer();
                }

                return;
            }

            if (_startRequested && !_isRunning && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                StartServer();
            }

            if (_isRunning && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                ProcessPendingRequests();
            }
        }

        private static void StartServer()
        {
            lock (StateLock)
            {
                if (_isRunning)
                {
                    _startRequested = false;
                    return;
                }

                try
                {
                    var port = Port;
                    var listener = new TcpListener(IPAddress.Loopback, port);
                    listener.Server.NoDelay = true;
                    listener.Start();

                    var cancellation = new CancellationTokenSource();
                    _listener = listener;
                    _listenerCancellation = cancellation;
                    Task.Run(() => ListenLoop(listener, cancellation.Token));
                    _isRunning = true;
                    _activePort = port;
                    _lastError = string.Empty;
                    _lastStartedUtc = DateTime.UtcNow;
                    Debug.Log("Editor RPC listening on " + Endpoint);
                }
                catch (Exception e)
                {
                    _lastError = "Failed to start Editor RPC: " + e.Message;
                    Debug.LogError(_lastError);
                    _listener = null;
                    if (_listenerCancellation != null)
                    {
                        _listenerCancellation.Dispose();
                        _listenerCancellation = null;
                    }

                    _isRunning = false;
                    _activePort = 0;
                }
                finally
                {
                    _startRequested = false;
                }
            }
        }

        private static void ListenLoop(TcpListener listener, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client = null;
                try
                {
                    client = listener.AcceptTcpClient();
                    client.NoDelay = true;
                    var acceptedClient = client;
                    Task.Run(() => HandleClient(acceptedClient, cancellationToken), cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    _lastError = "Editor RPC listener stopped accepting connections.";
                    return;
                }
                catch (Exception e)
                {
                    _lastError = "Editor RPC listener failed: " + e.Message;
                    if (client != null)
                    {
                        client.Dispose();
                    }

                    return;
                }
            }
        }

        private static void HandleClient(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            {
                client.ReceiveTimeout = RequestReadTimeoutMilliseconds;

                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, true))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true))
                {
                    writer.AutoFlush = true;

                    try
                    {
                        var rawRequest = reader.ReadLine();
                        EditorRpcRequest request;
                        string parseError;
                        if (!TryParseTransportRequest(rawRequest, out request, out parseError))
                        {
                            writer.WriteLine(SerializeFailureResponse(string.Empty, parseError));
                            return;
                        }

                        var pendingRequest = new PendingRpcRequest(request.requestId, request.methodName, request.argumentsJson);
                        PendingRequests.Enqueue(pendingRequest);

                        if (!pendingRequest.Completion.Task.Wait(TimeSpan.FromSeconds(RequestExecutionTimeoutSeconds)))
                        {
                            writer.WriteLine(SerializeFailureResponse(request.requestId, "Timed out waiting for Unity editor execution."));
                            return;
                        }

                        writer.WriteLine(pendingRequest.Completion.Task.Result);
                    }
                    catch (OperationCanceledException)
                    {
                        writer.WriteLine(SerializeFailureResponse(string.Empty, "RPC server stopped."));
                    }
                    catch (IOException e)
                    {
                        _lastError = "Editor RPC client I/O failed: " + e.Message;
                    }
                    catch (Exception e)
                    {
                        _lastError = "Editor RPC client failed: " + e.Message;
                        writer.WriteLine(SerializeFailureResponse(string.Empty, "RPC client failed: " + e.Message));
                    }
                }
            }
        }

        private static void ProcessPendingRequests()
        {
            var processedThisFrame = 0;
            while (processedThisFrame < MaxRequestsPerEditorUpdate)
            {
                PendingRpcRequest pendingRequest;
                if (!PendingRequests.TryDequeue(out pendingRequest))
                {
                    return;
                }

                processedThisFrame++;
                pendingRequest.Completion.TrySetResult(ExecutePendingRequest(pendingRequest));
            }
        }

        private static string ExecutePendingRequest(PendingRpcRequest pendingRequest)
        {
            try
            {
                var methodResult = EditorRpcMethodExecutor.Execute(pendingRequest.MethodName, pendingRequest.ArgumentsJson);
                _lastProcessedRequestId = pendingRequest.RequestId;
                _lastError = methodResult.success ? string.Empty : methodResult.message;
                _processedRequestCount++;

                return SerializeResponse(new EditorRpcResponse
                {
                    requestId = pendingRequest.RequestId,
                    success = methodResult.success,
                    message = methodResult.message,
                    payloadJson = methodResult.payloadJson,
                    processedAtUtc = DateTime.UtcNow.ToString("o")
                });
            }
            catch (Exception e)
            {
                _lastError = e.Message;
                return SerializeResponse(new EditorRpcResponse
                {
                    requestId = pendingRequest.RequestId,
                    success = false,
                    message = "RPC request failed: " + e.Message,
                    payloadJson = string.Empty,
                    processedAtUtc = DateTime.UtcNow.ToString("o")
                });
            }
        }

        private static bool TryParseTransportRequest(string rawRequest, out EditorRpcRequest request, out string error)
        {
            request = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(rawRequest))
            {
                error = "RPC request payload was empty.";
                return false;
            }

            var args = SimpleJsonParser.Parse(rawRequest);
            var methodName = GetValue(args, "method");
            if (string.IsNullOrWhiteSpace(methodName))
            {
                methodName = GetValue(args, "tool");
            }

            if (string.IsNullOrWhiteSpace(methodName))
            {
                error = "RPC request is missing method.";
                return false;
            }

            var requestId = GetValue(args, "request_id");
            if (string.IsNullOrWhiteSpace(requestId))
            {
                requestId = GetValue(args, "requestId");
            }

            if (string.IsNullOrWhiteSpace(requestId))
            {
                requestId = Guid.NewGuid().ToString("N");
            }

            var argumentsJson = GetValue(args, "args");
            if (string.IsNullOrWhiteSpace(argumentsJson))
            {
                argumentsJson = GetValue(args, "arguments");
            }

            if (string.IsNullOrWhiteSpace(argumentsJson))
            {
                argumentsJson = GetValue(args, "args_json");
            }

            if (string.IsNullOrWhiteSpace(argumentsJson))
            {
                argumentsJson = "{}";
            }

            request = new EditorRpcRequest
            {
                requestId = requestId,
                methodName = methodName,
                argumentsJson = argumentsJson
            };
            return true;
        }

        private static string GetValue(System.Collections.Generic.Dictionary<string, string> values, string key)
        {
            string value;
            return values != null && values.TryGetValue(key, out value) ? value : string.Empty;
        }

        private static string SerializeFailureResponse(string requestId, string message)
        {
            return SerializeResponse(new EditorRpcResponse
            {
                requestId = requestId ?? string.Empty,
                success = false,
                message = message,
                payloadJson = string.Empty,
                processedAtUtc = DateTime.UtcNow.ToString("o")
            });
        }

        private static string SerializeResponse(EditorRpcResponse response)
        {
            var builder = new StringBuilder(256);
            builder.Append('{');
            builder.Append("\"request_id\":\"").Append(EscapeJson(response.requestId)).Append("\",");
            builder.Append("\"success\":").Append(response.success ? "true" : "false").Append(',');
            builder.Append("\"message\":\"").Append(EscapeJson(response.message)).Append("\",");
            builder.Append("\"payload_json\":\"").Append(EscapeJson(response.payloadJson)).Append("\",");
            builder.Append("\"processed_at_utc\":\"").Append(EscapeJson(response.processedAtUtc)).Append("\"");
            builder.Append('}');
            return builder.ToString();
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        private static void FailPendingRequests(string message)
        {
            PendingRpcRequest pendingRequest;
            while (PendingRequests.TryDequeue(out pendingRequest))
            {
                pendingRequest.Completion.TrySetResult(SerializeFailureResponse(pendingRequest.RequestId, message));
            }
        }

        private static int ClampPort(int port)
        {
            if (port < MinPortValue)
            {
                return DefaultPortValue;
            }

            if (port > MaxPortValue)
            {
                return MaxPortValue;
            }

            return port;
        }

        private sealed class PendingRpcRequest
        {
            public PendingRpcRequest(string requestId, string methodName, string argumentsJson)
            {
                RequestId = requestId;
                MethodName = methodName;
                ArgumentsJson = string.IsNullOrEmpty(argumentsJson) ? "{}" : argumentsJson;
                Completion = new TaskCompletionSource<string>();
            }

            public string RequestId { get; private set; }

            public string MethodName { get; private set; }

            public string ArgumentsJson { get; private set; }

            public TaskCompletionSource<string> Completion { get; private set; }
        }
    }

    [Serializable]
    internal sealed class EditorRpcRequest
    {
        public string requestId;
        public string methodName;
        public string argumentsJson;
    }

    [Serializable]
    internal sealed class EditorRpcResponse
    {
        public string requestId;
        public bool success;
        public string message;
        public string payloadJson;
        public string processedAtUtc;
    }
}
