using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EditorRpc
{
    public sealed class EditorRpcWindow : EditorWindow
    {
        private Vector2 _methodsScroll;
        private int _portInput;
        private bool _portInputInitialized;

        [MenuItem("AI Tools/Editor RPC")]
        [MenuItem("AI工具/Editor RPC")]
        public static void OpenWindow()
        {
            var window = GetWindow<EditorRpcWindow>("Editor RPC");
            window.minSize = new Vector2(560f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            _portInput = EditorRpcService.Port;
            _portInputInitialized = true;
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            EnsurePortInput();

            EditorGUILayout.LabelField("Editor RPC", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Local loopback RPC for terminal or external AI control of the open Unity editor.", MessageType.Info);

            DrawStatusSection();
            EditorGUILayout.Space();
            DrawActionSection();
            EditorGUILayout.Space();
            DrawMethodsSection();
        }

        private void EnsurePortInput()
        {
            if (!_portInputInitialized)
            {
                _portInput = EditorRpcService.Port;
                _portInputInitialized = true;
            }
        }

        private void DrawStatusSection()
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            var enabled = EditorGUILayout.Toggle("Enabled", EditorRpcService.IsEnabled);
            if (enabled != EditorRpcService.IsEnabled)
            {
                EditorRpcService.IsEnabled = enabled;
            }

            _portInput = EditorGUILayout.IntField("Port", _portInput);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Port"))
                {
                    EditorRpcService.Port = _portInput;
                    _portInput = EditorRpcService.Port;
                }

                if (GUILayout.Button(EditorRpcService.IsRunning ? "Restart Server" : "Start Server"))
                {
                    EditorRpcService.IsEnabled = true;
                    EditorRpcService.RestartServer();
                }

                if (GUILayout.Button("Stop Server"))
                {
                    EditorRpcService.IsEnabled = false;
                }
            }

            EditorGUILayout.SelectableLabel(EditorRpcService.Endpoint, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.LabelField("Running", EditorRpcService.IsRunning ? "Yes" : "No");
            EditorGUILayout.LabelField("Last Started UTC", string.IsNullOrEmpty(EditorRpcService.LastStartedUtc) ? "-" : EditorRpcService.LastStartedUtc);
            EditorGUILayout.LabelField("Pending Requests", EditorRpcService.PendingRequestCount.ToString());
            EditorGUILayout.LabelField("Processed Requests", EditorRpcService.ProcessedRequestCount.ToString());
            EditorGUILayout.LabelField("Last Request Id", string.IsNullOrEmpty(EditorRpcService.LastProcessedRequestId) ? "-" : EditorRpcService.LastProcessedRequestId);

            if (!string.IsNullOrEmpty(EditorRpcService.LastError))
            {
                EditorGUILayout.HelpBox(EditorRpcService.LastError, MessageType.Warning);
            }
        }

        private void DrawActionSection()
        {
            EditorGUILayout.LabelField("Client", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy list_methods Command"))
                {
                    EditorGUIUtility.systemCopyBuffer =
                        "powershell -ExecutionPolicy Bypass -File \"" +
                        EditorRpcService.ClientScriptPath +
                        "\" -Method list_methods";
                }

                if (GUILayout.Button("Reveal Client Folder"))
                {
                    EditorUtility.RevealInFinder(EditorRpcService.ClientToolDirectory);
                }
            }
        }

        private void DrawMethodsSection()
        {
            EditorGUILayout.LabelField("Methods", EditorStyles.boldLabel);
            var methods = EditorRpcMethods.GetMethods();
            _methodsScroll = EditorGUILayout.BeginScrollView(_methodsScroll);

            for (int i = 0; i < methods.Count; i++)
            {
                DrawMethod(methods[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawMethod(EditorRpcMethodDefinition method)
        {
            if (method == null)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(method.Name + " [" + method.Category + "]", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(method.Description, EditorStyles.wordWrappedLabel);

                if (method.Parameters == null || method.Parameters.Count == 0)
                {
                    EditorGUILayout.LabelField("Parameters: none");
                    return;
                }

                EditorGUILayout.LabelField("Parameters:");
                foreach (KeyValuePair<string, EditorRpcParameterDefinition> pair in method.Parameters)
                {
                    var definition = pair.Value;
                    var requiredSuffix = definition.Required ? " required" : " optional";
                    EditorGUILayout.LabelField(
                        "  " + pair.Key + " : " + definition.Type + " (" + requiredSuffix.Trim() + ")",
                        EditorStyles.miniBoldLabel);
                    EditorGUILayout.LabelField("  " + definition.Description, EditorStyles.wordWrappedMiniLabel);
                }
            }
        }
    }
}
