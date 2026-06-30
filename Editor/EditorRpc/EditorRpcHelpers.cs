using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorRpc
{
    public static partial class EditorRpcMethodExecutor
    {
        private static Dictionary<string, string> ParseArgs(string argumentsJson)
        {
            return SimpleJsonParser.Parse(argumentsJson);
        }

        private static EditorRpcMethodResult Success(string message)
        {
            return new EditorRpcMethodResult
            {
                success = true,
                message = message,
                payloadJson = string.Empty
            };
        }

        private static EditorRpcMethodResult Success(string message, object payload)
        {
            return new EditorRpcMethodResult
            {
                success = true,
                message = message,
                payloadJson = payload == null ? string.Empty : JsonUtility.ToJson(payload, false)
            };
        }

        private static EditorRpcMethodResult Failure(string message)
        {
            return new EditorRpcMethodResult
            {
                success = false,
                message = message,
                payloadJson = string.Empty
            };
        }

        private static string GetRequiredString(Dictionary<string, string> args, string key)
        {
            string value;
            if (!args.TryGetValue(key, out value))
            {
                return string.Empty;
            }

            return NormalizeStringValue(value);
        }

        private static string GetString(Dictionary<string, string> args, string key, string defaultValue)
        {
            string value;
            if (!args.TryGetValue(key, out value))
            {
                return defaultValue;
            }

            value = NormalizeStringValue(value);
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        private static bool GetBool(Dictionary<string, string> args, string key, bool defaultValue)
        {
            string value;
            if (!args.TryGetValue(key, out value))
            {
                return defaultValue;
            }

            value = NormalizeStringValue(value);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1")
            {
                return true;
            }

            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) || value == "0")
            {
                return false;
            }

            return defaultValue;
        }

        private static int GetInt(Dictionary<string, string> args, string key, int defaultValue)
        {
            string value;
            int parsedValue;
            if (!args.TryGetValue(key, out value))
            {
                return defaultValue;
            }

            value = NormalizeStringValue(value);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue)
                ? parsedValue
                : defaultValue;
        }

        private static float GetFloat(Dictionary<string, string> args, string key, float defaultValue)
        {
            string value;
            float parsedValue;
            if (!args.TryGetValue(key, out value))
            {
                return defaultValue;
            }

            value = NormalizeStringValue(value);
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue)
                ? parsedValue
                : defaultValue;
        }

        private static bool TryGetVector3(Dictionary<string, string> args, string key, out Vector3 value)
        {
            string raw;
            if (!args.TryGetValue(key, out raw))
            {
                value = Vector3.zero;
                return false;
            }

            return TryParseVector3(NormalizeStringValue(raw), out value);
        }

        private static bool TryGetVector2(Dictionary<string, string> args, string key, out Vector2 value)
        {
            string raw;
            if (!args.TryGetValue(key, out raw))
            {
                value = Vector2.zero;
                return false;
            }

            return TryParseVector2(NormalizeStringValue(raw), out value);
        }

        private static bool TryParseVector3(string raw, out Vector3 value)
        {
            value = Vector3.zero;
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            raw = raw.Trim().Trim('[', ']', '(', ')');
            var parts = raw.Split(new[] { ',', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                return false;
            }

            float x;
            float y;
            float z;
            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y) ||
                !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
            {
                return false;
            }

            value = new Vector3(x, y, z);
            return true;
        }

        private static bool TryParseVector2(string raw, out Vector2 value)
        {
            value = Vector2.zero;
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            raw = raw.Trim().Trim('[', ']', '(', ')');
            var parts = raw.Split(new[] { ',', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return false;
            }

            float x;
            float y;
            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y))
            {
                return false;
            }

            value = new Vector2(x, y);
            return true;
        }

        private static bool TryParseColor(string raw, out Color value)
        {
            value = Color.white;
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            raw = raw.Trim().Trim('[', ']', '(', ')');
            var parts = raw.Split(new[] { ',', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3 && parts.Length != 4)
            {
                return false;
            }

            float r;
            float g;
            float b;
            float a = 1f;
            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out r) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out g) ||
                !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out b))
            {
                return false;
            }

            if (parts.Length == 4 && !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out a))
            {
                return false;
            }

            value = new Color(r, g, b, a);
            return true;
        }

        private static string[] ParseStringArray(string raw)
        {
            raw = NormalizeStringValue(raw);
            if (string.IsNullOrEmpty(raw))
            {
                return new string[0];
            }

            raw = raw.Trim();
            if (raw.StartsWith("[", StringComparison.Ordinal) && raw.EndsWith("]", StringComparison.Ordinal))
            {
                raw = raw.Substring(1, raw.Length - 2);
            }

            var tokens = raw.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
            var values = new List<string>();
            for (int i = 0; i < tokens.Length; i++)
            {
                var token = NormalizeStringValue(tokens[i]);
                if (!string.IsNullOrEmpty(token))
                {
                    values.Add(token);
                }
            }

            return values.ToArray();
        }

        private static string NormalizeStringValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            value = value.Trim();
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            {
                value = value.Substring(1, value.Length - 2);
            }

            return value.Replace("\\\"", "\"");
        }

        private static string ProjectRoot
        {
            get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); }
        }

        private static bool AssetExists(string path)
        {
            return AssetDatabase.LoadMainAssetAtPath(path) != null || AssetDatabase.IsValidFolder(path);
        }

        private static bool TryResolveScene(string scenePath, out Scene scene)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                scene = SceneManager.GetActiveScene();
                return scene.IsValid();
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var loadedScene = SceneManager.GetSceneAt(i);
                if (loadedScene.IsValid() && string.Equals(loadedScene.path, scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    scene = loadedScene;
                    return true;
                }
            }

            scene = default(Scene);
            return false;
        }

        private static List<Scene> GetSearchScenes(string scenePath)
        {
            var scenes = new List<Scene>();
            if (!string.IsNullOrEmpty(scenePath))
            {
                Scene resolvedScene;
                if (TryResolveScene(scenePath, out resolvedScene))
                {
                    scenes.Add(resolvedScene);
                }

                return scenes;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded)
                {
                    scenes.Add(scene);
                }
            }

            return scenes;
        }

        private static string GetGameObjectPath(GameObject go)
        {
            if (go == null)
            {
                return string.Empty;
            }

            var names = new List<string>();
            var current = go.transform;
            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        private static bool TryFindGameObject(string path, out GameObject go)
        {
            go = null;
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return false;
            }

            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                var roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    if (!string.Equals(roots[rootIndex].name, parts[0], StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var current = roots[rootIndex].transform;
                    var matched = true;
                    for (int partIndex = 1; partIndex < parts.Length; partIndex++)
                    {
                        current = current.Find(parts[partIndex]);
                        if (current == null)
                        {
                            matched = false;
                            break;
                        }
                    }

                    if (matched)
                    {
                        go = current.gameObject;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryFindPrefabGameObject(GameObject root, string objectPath, out GameObject target)
        {
            target = root;
            if (root == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(objectPath))
            {
                return true;
            }

            var parts = objectPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return true;
            }

            var current = root.transform;
            var startIndex = 0;
            if (string.Equals(parts[0], root.name, StringComparison.Ordinal))
            {
                startIndex = 1;
            }

            for (int i = startIndex; i < parts.Length; i++)
            {
                current = current.Find(parts[i]);
                if (current == null)
                {
                    target = null;
                    return false;
                }
            }

            target = current.gameObject;
            return true;
        }

        private static bool TryResolveSerializedTarget(GameObject go, string componentType, int componentIndex, out UnityEngine.Object target, out string error)
        {
            error = string.Empty;
            if (go == null)
            {
                target = null;
                error = "GameObject is null.";
                return false;
            }

            if (string.IsNullOrEmpty(componentType))
            {
                target = go;
                return true;
            }

            var resolvedType = ResolveTypeByName(componentType, typeof(Component));
            if (resolvedType == null)
            {
                target = null;
                error = "Could not resolve component type: " + componentType;
                return false;
            }

            var components = go.GetComponents<Component>();
            var matches = new List<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null && resolvedType.IsAssignableFrom(components[i].GetType()))
                {
                    matches.Add(components[i]);
                }
            }

            if (matches.Count == 0)
            {
                target = null;
                error = "Component not found on GameObject: " + componentType;
                return false;
            }

            if (componentIndex < 0 || componentIndex >= matches.Count)
            {
                target = null;
                error = "component_index is out of range for component type: " + componentType;
                return false;
            }

            target = matches[componentIndex];
            return true;
        }

        private static string[] GetComponentTypeNames(GameObject go)
        {
            var components = go != null ? go.GetComponents<Component>() : null;
            if (components == null || components.Length == 0)
            {
                return new string[0];
            }

            var names = new List<string>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                {
                    names.Add(components[i].GetType().Name);
                }
            }

            return names.ToArray();
        }

        private static ComponentSearchInfo[] BuildComponentInfos(GameObject go, int propertyLimitPerComponent)
        {
            if (go == null)
            {
                return new ComponentSearchInfo[0];
            }

            var components = go.GetComponents<Component>();
            if (components == null || components.Length == 0)
            {
                return new ComponentSearchInfo[0];
            }

            var list = new List<ComponentSearchInfo>();
            for (int index = 0; index < components.Length; index++)
            {
                var component = components[index];
                if (component == null)
                {
                    continue;
                }

                int totalPropertyCount;
                var properties = CollectSerializedPropertyInfos(component, Mathf.Max(1, propertyLimitPerComponent), out totalPropertyCount);
                list.Add(new ComponentSearchInfo
                {
                    index = index,
                    typeName = component.GetType().Name,
                    fullTypeName = component.GetType().FullName,
                    targetPath = BuildSceneTargetPath(go, component),
                    totalPropertyCount = totalPropertyCount,
                    returnedPropertyCount = properties.Length,
                    properties = properties
                });
            }

            return list.ToArray();
        }

        private static SerializedTargetPayload BuildSerializedTargetPayload(string targetPath, UnityEngine.Object target, string[] componentTypes, bool includeProperties, int propertyLimit)
        {
            var payload = new SerializedTargetPayload
            {
                targetPath = targetPath,
                targetType = target != null ? target.GetType().FullName : string.Empty,
                componentTypes = componentTypes ?? new string[0],
                totalPropertyCount = 0,
                returnedPropertyCount = 0,
                properties = new SerializedPropertyInfo[0]
            };

            if (!includeProperties || target == null)
            {
                return payload;
            }

            int totalPropertyCount;
            var properties = CollectSerializedPropertyInfos(target, Mathf.Max(1, propertyLimit), out totalPropertyCount);
            payload.totalPropertyCount = totalPropertyCount;
            payload.returnedPropertyCount = properties.Length;
            payload.properties = properties;
            return payload;
        }

        private static SerializedPropertyInfo[] CollectSerializedPropertyInfos(UnityEngine.Object target, int limit, out int totalPropertyCount)
        {
            var list = new List<SerializedPropertyInfo>();
            totalPropertyCount = 0;
            if (target == null)
            {
                return list.ToArray();
            }

            var serializedObject = new SerializedObject(target);
            var iterator = serializedObject.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                totalPropertyCount++;
                if (list.Count < limit)
                {
                    list.Add(new SerializedPropertyInfo
                    {
                        propertyPath = iterator.propertyPath,
                        displayName = iterator.displayName,
                        propertyType = iterator.propertyType.ToString(),
                        isArray = iterator.isArray && iterator.propertyType != SerializedPropertyType.String,
                        editable = iterator.editable,
                        valuePreview = GetSerializedPropertyPreview(iterator)
                    });
                }

                enterChildren = false;
            }

            return list.ToArray();
        }

        private static string GetSerializedPropertyPreview(SerializedProperty property)
        {
            if (property == null)
            {
                return string.Empty;
            }

            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                return "size=" + property.arraySize.ToString(CultureInfo.InvariantCulture);
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.Boolean:
                    return property.boolValue ? "true" : "false";
                case SerializedPropertyType.Float:
                    return property.floatValue.ToString("0.###", CultureInfo.InvariantCulture);
                case SerializedPropertyType.String:
                    return property.stringValue ?? string.Empty;
                case SerializedPropertyType.Color:
                    return property.colorValue.r.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                           property.colorValue.g.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                           property.colorValue.b.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                           property.colorValue.a.ToString("0.###", CultureInfo.InvariantCulture);
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue != null ? DescribeSelectionObject(property.objectReferenceValue) : "null";
                case SerializedPropertyType.Enum:
                    if (property.enumDisplayNames != null &&
                        property.enumValueIndex >= 0 &&
                        property.enumValueIndex < property.enumDisplayNames.Length)
                    {
                        return property.enumDisplayNames[property.enumValueIndex];
                    }

                    return property.enumValueIndex.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.Vector2:
                    return property.vector2Value.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                           property.vector2Value.y.ToString("0.###", CultureInfo.InvariantCulture);
                case SerializedPropertyType.Vector3:
                    return property.vector3Value.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                           property.vector3Value.y.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                           property.vector3Value.z.ToString("0.###", CultureInfo.InvariantCulture);
                default:
                    return property.propertyType.ToString();
            }
        }

        private static bool TrySetSerializedPropertyValue(SerializedObject serializedObject, string propertyPath, string requestedValueType, string rawValue, out PropertySetPayload payload, out string error)
        {
            payload = null;
            error = string.Empty;
            if (serializedObject == null)
            {
                error = "SerializedObject is null.";
                return false;
            }

            var property = serializedObject.FindProperty(propertyPath);
            if (property == null)
            {
                error = "SerializedProperty not found: " + propertyPath;
                return false;
            }

            var valueType = string.IsNullOrEmpty(requestedValueType) ? InferValueType(property) : requestedValueType;
            rawValue = NormalizeStringValue(rawValue);

            try
            {
                switch (valueType)
                {
                    case "bool":
                        property.boolValue = string.Equals(rawValue, "true", StringComparison.OrdinalIgnoreCase) || rawValue == "1";
                        break;
                    case "int":
                        int intValue;
                        if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
                        {
                            error = "Could not parse int value: " + rawValue;
                            return false;
                        }

                        property.intValue = intValue;
                        break;
                    case "float":
                        float floatValue;
                        if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out floatValue))
                        {
                            error = "Could not parse float value: " + rawValue;
                            return false;
                        }

                        property.floatValue = floatValue;
                        break;
                    case "string":
                        property.stringValue = rawValue;
                        break;
                    case "enum":
                        int enumIndex;
                        if (!TryParseEnumValue(property, rawValue, out enumIndex))
                        {
                            error = "Could not parse enum value: " + rawValue;
                            return false;
                        }

                        property.enumValueIndex = enumIndex;
                        break;
                    case "vector2":
                        Vector2 vector2Value;
                        if (!TryParseVector2(rawValue, out vector2Value))
                        {
                            error = "Could not parse vector2 value: " + rawValue;
                            return false;
                        }

                        property.vector2Value = vector2Value;
                        break;
                    case "vector3":
                        Vector3 vector3Value;
                        if (!TryParseVector3(rawValue, out vector3Value))
                        {
                            error = "Could not parse vector3 value: " + rawValue;
                            return false;
                        }

                        property.vector3Value = vector3Value;
                        break;
                    case "color":
                        Color colorValue;
                        if (!TryParseColor(rawValue, out colorValue))
                        {
                            error = "Could not parse color value: " + rawValue;
                            return false;
                        }

                        property.colorValue = colorValue;
                        break;
                    case "object_reference":
                        UnityEngine.Object referenceValue;
                        if (!TryResolveObjectReference(rawValue, property.serializedObject.targetObject, out referenceValue, out error))
                        {
                            return false;
                        }

                        property.objectReferenceValue = referenceValue;
                        break;
                    default:
                        error = "Unsupported value_type: " + valueType;
                        return false;
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }

            payload = new PropertySetPayload
            {
                targetPath = DescribeSelectionObject(serializedObject.targetObject),
                targetType = serializedObject.targetObject != null ? serializedObject.targetObject.GetType().FullName : string.Empty,
                propertyPath = property.propertyPath,
                propertyType = property.propertyType.ToString(),
                valuePreview = GetSerializedPropertyPreview(property)
            };
            return true;
        }

        private static string InferValueType(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return "bool";
                case SerializedPropertyType.Integer:
                    return "int";
                case SerializedPropertyType.Float:
                    return "float";
                case SerializedPropertyType.String:
                    return "string";
                case SerializedPropertyType.Enum:
                    return "enum";
                case SerializedPropertyType.Vector2:
                    return "vector2";
                case SerializedPropertyType.Vector3:
                    return "vector3";
                case SerializedPropertyType.Color:
                    return "color";
                case SerializedPropertyType.ObjectReference:
                    return "object_reference";
                default:
                    return property.propertyType.ToString().ToLowerInvariant();
            }
        }

        private static bool TryParseEnumValue(SerializedProperty property, string rawValue, out int value)
        {
            value = 0;
            if (property == null)
            {
                return false;
            }

            int parsedIndex;
            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedIndex))
            {
                if (parsedIndex >= 0 && parsedIndex < property.enumDisplayNames.Length)
                {
                    value = parsedIndex;
                    return true;
                }

                return false;
            }

            for (int i = 0; i < property.enumDisplayNames.Length; i++)
            {
                if (string.Equals(property.enumDisplayNames[i], rawValue, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(property.enumNames[i], rawValue, StringComparison.OrdinalIgnoreCase))
                {
                    value = i;
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveObjectReference(string rawValue, UnityEngine.Object contextTarget, out UnityEngine.Object reference, out string error)
        {
            reference = null;
            error = string.Empty;

            if (string.IsNullOrEmpty(rawValue) || string.Equals(rawValue, "null", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (rawValue.StartsWith("asset:", StringComparison.OrdinalIgnoreCase))
            {
                var assetReference = rawValue.Substring("asset:".Length);
                reference = LoadPreferredAssetReference(assetReference);
                if (reference == null)
                {
                    error = "Asset reference not found: " + assetReference;
                    return false;
                }

                return true;
            }

            if (rawValue.StartsWith("scene:", StringComparison.OrdinalIgnoreCase))
            {
                var sceneRef = rawValue.Substring("scene:".Length);
                var componentType = string.Empty;
                var hashIndex = sceneRef.IndexOf('#');
                if (hashIndex >= 0)
                {
                    componentType = sceneRef.Substring(hashIndex + 1);
                    sceneRef = sceneRef.Substring(0, hashIndex);
                }

                if (TryResolveContextHierarchyReference(sceneRef, componentType, contextTarget, out reference, out error))
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(error))
                {
                    return false;
                }

                GameObject go;
                if (!TryFindGameObject(sceneRef, out go))
                {
                    error = "Scene object reference not found: " + sceneRef;
                    return false;
                }

                if (string.IsNullOrEmpty(componentType))
                {
                    reference = go;
                    return true;
                }

                var resolvedType = ResolveTypeByName(componentType, typeof(Component));
                if (resolvedType == null)
                {
                    error = "Could not resolve referenced component type: " + componentType;
                    return false;
                }

                reference = go.GetComponent(resolvedType);
                if (reference == null)
                {
                    error = "Referenced component not found: " + componentType;
                    return false;
                }

                return true;
            }

            error = "Unsupported object reference format. Use asset: or scene:.";
            return false;
        }

        private static UnityEngine.Object LoadPreferredAssetReference(string assetReference)
        {
            if (string.IsNullOrEmpty(assetReference))
            {
                return null;
            }

            string assetPath;
            string subAssetTypeName;
            string subAssetName;
            ParseAssetReference(assetReference, out assetPath, out subAssetTypeName, out subAssetName);

            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            if (!string.IsNullOrEmpty(subAssetTypeName) || !string.IsNullOrEmpty(subAssetName))
            {
                return LoadSubAssetReference(assetPath, subAssetTypeName, subAssetName);
            }

            var spriteAsset = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .FirstOrDefault(asset => asset is Sprite);
            if (spriteAsset != null)
            {
                return spriteAsset;
            }

            return AssetDatabase.LoadMainAssetAtPath(assetPath);
        }

        private static void ParseAssetReference(string assetReference, out string assetPath, out string subAssetTypeName, out string subAssetName)
        {
            assetPath = assetReference;
            subAssetTypeName = string.Empty;
            subAssetName = string.Empty;

            if (string.IsNullOrEmpty(assetReference))
            {
                return;
            }

            string[] segments = assetReference.Split(new[] { '#' }, StringSplitOptions.None);
            if (segments.Length <= 1)
            {
                return;
            }

            assetPath = segments[0];
            if (segments.Length == 2)
            {
                subAssetName = segments[1];
                return;
            }

            subAssetTypeName = segments[1];
            subAssetName = segments[2];
        }

        private static UnityEngine.Object LoadSubAssetReference(string assetPath, string subAssetTypeName, string subAssetName)
        {
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (subAssets == null || subAssets.Length == 0)
            {
                return null;
            }

            Type hintedType = string.IsNullOrEmpty(subAssetTypeName) ? null : ResolveTypeByName(subAssetTypeName, typeof(UnityEngine.Object));

            for (int i = 0; i < subAssets.Length; i++)
            {
                var candidate = subAssets[i];
                if (candidate == null)
                {
                    continue;
                }

                if (hintedType != null && !hintedType.IsAssignableFrom(candidate.GetType()))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(subAssetName) &&
                    !string.Equals(candidate.name, subAssetName, StringComparison.Ordinal))
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private static bool TryResolveContextHierarchyReference(string hierarchyPath, string componentType, UnityEngine.Object contextTarget, out UnityEngine.Object reference, out string error)
        {
            reference = null;
            error = string.Empty;
            if (string.IsNullOrEmpty(hierarchyPath) || contextTarget == null)
            {
                return false;
            }

            GameObject contextGo = null;
            var contextComponent = contextTarget as Component;
            if (contextComponent != null)
            {
                contextGo = contextComponent.gameObject;
            }
            else
            {
                contextGo = contextTarget as GameObject;
            }

            if (contextGo == null)
            {
                return false;
            }

            GameObject targetGo;
            if (!TryFindPrefabGameObject(contextGo.transform.root.gameObject, hierarchyPath, out targetGo))
            {
                return false;
            }

            if (string.IsNullOrEmpty(componentType))
            {
                reference = targetGo;
                return true;
            }

            var resolvedType = ResolveTypeByName(componentType, typeof(Component));
            if (resolvedType == null)
            {
                error = "Could not resolve referenced component type: " + componentType;
                return false;
            }

            reference = targetGo.GetComponent(resolvedType);
            if (reference == null)
            {
                error = "Referenced component not found: " + componentType;
                return false;
            }

            return true;
        }

        private static Type ResolveTypeByName(string typeName, Type assignableTo)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
            {
                Type[] types;
                try
                {
                    types = assemblies[assemblyIndex].GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                if (types == null)
                {
                    continue;
                }

                for (int typeIndex = 0; typeIndex < types.Length; typeIndex++)
                {
                    var type = types[typeIndex];
                    if (type == null)
                    {
                        continue;
                    }

                    if (!string.Equals(type.Name, typeName, StringComparison.Ordinal) &&
                        !string.Equals(type.FullName, typeName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (assignableTo == null || assignableTo.IsAssignableFrom(type))
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        private static Type[] SearchTypes(string query, Type assignableTo, int limit)
        {
            var results = new List<Type>();
            query = NormalizeStringValue(query);
            var hasQuery = !string.IsNullOrEmpty(query);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int assemblyIndex = 0; assemblyIndex < assemblies.Length && results.Count < limit; assemblyIndex++)
            {
                Type[] types;
                try
                {
                    types = assemblies[assemblyIndex].GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                if (types == null)
                {
                    continue;
                }

                for (int typeIndex = 0; typeIndex < types.Length && results.Count < limit; typeIndex++)
                {
                    var type = types[typeIndex];
                    if (type == null)
                    {
                        continue;
                    }

                    if (assignableTo != null && !assignableTo.IsAssignableFrom(type))
                    {
                        continue;
                    }

                    if (hasQuery &&
                        type.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
                        type.FullName.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    results.Add(type);
                }
            }

            return results.ToArray();
        }

        private static TypeSearchInfo BuildTypeSearchInfo(Type type)
        {
            return new TypeSearchInfo
            {
                assemblyName = type.Assembly.GetName().Name,
                fullName = type.FullName,
                name = type.Name,
                isAbstract = type.IsAbstract,
                isEnum = type.IsEnum,
                isValueType = type.IsValueType,
                isUnityObject = typeof(UnityEngine.Object).IsAssignableFrom(type),
                isComponent = typeof(Component).IsAssignableFrom(type)
            };
        }

        private static MethodSearchInfo[] BuildMethodSearchInfos(Type targetType, string methodNameFilter, bool includeInherited, bool includeNonPublic, int limit)
        {
            if (targetType == null)
            {
                return new MethodSearchInfo[0];
            }

            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            if (includeNonPublic)
            {
                flags |= BindingFlags.NonPublic;
            }

            if (!includeInherited)
            {
                flags |= BindingFlags.DeclaredOnly;
            }

            var filter = NormalizeStringValue(methodNameFilter);
            var hasFilter = !string.IsNullOrEmpty(filter);
            var methods = targetType.GetMethods(flags);
            var list = new List<MethodSearchInfo>();

            for (int index = 0; index < methods.Length && list.Count < limit; index++)
            {
                var method = methods[index];
                if (method.IsSpecialName)
                {
                    continue;
                }

                if (hasFilter && method.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var parameters = method.GetParameters();
                var parameterDescriptions = new string[parameters.Length];
                int requiredParameterCount = 0;
                for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
                {
                    var parameter = parameters[parameterIndex];
                    if (!parameter.IsOptional)
                    {
                        requiredParameterCount++;
                    }

                    parameterDescriptions[parameterIndex] = parameter.ParameterType.FullName + " " + parameter.Name + (parameter.IsOptional ? " = optional" : string.Empty);
                }

                list.Add(new MethodSearchInfo
                {
                    declaringType = method.DeclaringType != null ? method.DeclaringType.FullName : targetType.FullName,
                    methodName = method.Name,
                    isStatic = method.IsStatic,
                    isPublic = method.IsPublic,
                    isGeneric = method.IsGenericMethod,
                    returnType = method.ReturnType != null ? method.ReturnType.FullName : string.Empty,
                    requiredParameterCount = requiredParameterCount,
                    parameters = parameterDescriptions
                });
            }

            return list.ToArray();
        }

        private static int CountConsoleEntriesByMask(ConsoleEntryInfo[] entries, int mask)
        {
            if (entries == null || entries.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < entries.Length; index++)
            {
                if ((entries[index].mode & mask) != 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static void EnsureAssetParentDirectoryExists(string assetPath)
        {
            var fullPath = Path.GetFullPath(Path.Combine(ProjectRoot, assetPath));
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static void MarkDirty(GameObject go)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            if (go != null && go.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(go.scene);
            }
        }

        private static void SaveSceneIfRequested(Dictionary<string, string> args, GameObject go)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            if (GetBool(args, "save_scene", false) && go != null && go.scene.IsValid())
            {
                EditorSceneManager.SaveScene(go.scene);
            }
        }

        private static EditorRpcMethodListPayload BuildMethodListPayload()
        {
            var methods = EditorRpcMethods.GetMethods();
            var items = new List<EditorRpcMethodInfo>();
            for (int i = 0; i < methods.Count; i++)
            {
                var definition = methods[i];
                var parameters = new List<EditorRpcParameterInfo>();
                foreach (var pair in definition.Parameters)
                {
                    parameters.Add(new EditorRpcParameterInfo
                    {
                        name = pair.Key,
                        type = pair.Value.Type,
                        description = pair.Value.Description,
                        required = pair.Value.Required,
                        enumValues = pair.Value.EnumValues != null ? pair.Value.EnumValues.ToArray() : new string[0]
                    });
                }

                items.Add(new EditorRpcMethodInfo
                {
                    name = definition.Name,
                    category = definition.Category,
                    description = definition.Description,
                    parameters = parameters.ToArray()
                });
            }

            return new EditorRpcMethodListPayload
            {
                returnedCount = items.Count,
                methods = items.ToArray()
            };
        }

        private static EditorStatePayload BuildEditorStatePayload()
        {
            var activeScene = SceneManager.GetActiveScene();
            var selected = Selection.objects;
            var selection = new string[selected.Length];
            for (int i = 0; i < selected.Length; i++)
            {
                selection[i] = DescribeSelectionObject(selected[i]);
            }

            return new EditorStatePayload
            {
                projectPath = ProjectRoot,
                activeScenePath = activeScene.path,
                activeSceneDirty = activeScene.IsValid() && activeScene.isDirty,
                isPlaying = EditorApplication.isPlaying,
                isPaused = EditorApplication.isPaused,
                isCompiling = EditorApplication.isCompiling,
                isUpdating = EditorApplication.isUpdating,
                selection = selection
            };
        }

        private static string DescribeSelectionObject(UnityEngine.Object obj)
        {
            var go = obj as GameObject;
            if (go != null)
            {
                return "scene:" + GetGameObjectPath(go);
            }

            var component = obj as Component;
            if (component != null)
            {
                return "scene:" + GetGameObjectPath(component.gameObject) + "#" + component.GetType().Name;
            }

            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(assetPath))
            {
                var mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (mainAsset != null && mainAsset != obj)
                {
                    return "asset:" + assetPath + "#" + obj.GetType().Name + "#" + obj.name;
                }

                return "asset:" + assetPath;
            }

            return obj != null ? obj.name : string.Empty;
        }

        private static string FormatVector3(Vector3 value)
        {
            return value.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   value.y.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   value.z.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static GameObjectPayload BuildGameObjectPayload(GameObject go)
        {
            return new GameObjectPayload
            {
                path = GetGameObjectPath(go),
                name = go.name,
                scenePath = go.scene.path,
                activeSelf = go.activeSelf,
                position = FormatVector3(go.transform.position),
                localPosition = FormatVector3(go.transform.localPosition),
                localRotationEuler = FormatVector3(go.transform.localEulerAngles),
                localScale = FormatVector3(go.transform.localScale)
            };
        }

        private static string BuildAssetTargetPath(string assetPath, GameObject targetGo, UnityEngine.Object target)
        {
            if (target == null || targetGo == null || target == targetGo)
            {
                return assetPath + "::" + GetGameObjectPath(targetGo);
            }

            return assetPath + "::" + GetGameObjectPath(targetGo) + "#" + target.GetType().Name;
        }

        private static string BuildSceneTargetPath(GameObject go, UnityEngine.Object target)
        {
            if (target == null || go == null || target == go)
            {
                return GetGameObjectPath(go);
            }

            return GetGameObjectPath(go) + "#" + target.GetType().Name;
        }

        private static void AppendHierarchyNode(Transform current, int depth, int maxDepth, bool includeComponents, bool includeInactive, List<HierarchyNodeInfo> nodes, int limit)
        {
            if (current == null || nodes.Count >= limit)
            {
                return;
            }

            if (!includeInactive && !current.gameObject.activeSelf)
            {
                return;
            }

            nodes.Add(new HierarchyNodeInfo
            {
                path = GetGameObjectPath(current.gameObject),
                name = current.name,
                depth = depth,
                activeSelf = current.gameObject.activeSelf,
                componentTypes = includeComponents ? GetComponentTypeNames(current.gameObject) : new string[0]
            });

            if (depth >= maxDepth)
            {
                return;
            }

            for (int childIndex = 0; childIndex < current.childCount && nodes.Count < limit; childIndex++)
            {
                AppendHierarchyNode(current.GetChild(childIndex), depth + 1, maxDepth, includeComponents, includeInactive, nodes, limit);
            }
        }

        private static void AppendMatchingObjects(Transform current, string needle, bool includeInactive, List<GameObjectInfo> matches, int limit)
        {
            if (current == null || matches.Count >= limit)
            {
                return;
            }

            if ((includeInactive || current.gameObject.activeSelf) &&
                current.name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                matches.Add(new GameObjectInfo
                {
                    path = GetGameObjectPath(current.gameObject),
                    name = current.name,
                    scenePath = current.gameObject.scene.path,
                    activeSelf = current.gameObject.activeSelf
                });
            }

            for (int childIndex = 0; childIndex < current.childCount && matches.Count < limit; childIndex++)
            {
                AppendMatchingObjects(current.GetChild(childIndex), needle, includeInactive, matches, limit);
            }
        }

        private static EditorRpcMethodResult ExecuteListMethods(string methodName, string argumentsJson)
        {
            return Success("RPC method list retrieved.", BuildMethodListPayload());
        }

        private static EditorRpcMethodResult ExecuteGetEditorState(string methodName, string argumentsJson)
        {
            return Success("Editor state retrieved.", BuildEditorStatePayload());
        }

        private static EditorRpcMethodResult ExecuteEnterPlayMode(string methodName, string argumentsJson)
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
            }

            return Success("Play Mode requested.", BuildEditorStatePayload());
        }

        private static EditorRpcMethodResult ExecuteExitPlayMode(string methodName, string argumentsJson)
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            return Success("Exit Play Mode requested.", BuildEditorStatePayload());
        }

        private static EditorRpcMethodResult ExecuteSetPlayModePause(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            EditorApplication.isPaused = GetBool(args, "paused", true);
            return Success("Play Mode pause state updated.", BuildEditorStatePayload());
        }

        private static EditorRpcMethodResult ExecuteMenuItem(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var menuPath = GetRequiredString(args, "menu_path");
            if (string.IsNullOrEmpty(menuPath))
            {
                return Failure("execute_menu_item requires menu_path.");
            }

            if (!EditorApplication.ExecuteMenuItem(menuPath))
            {
                return Failure("Menu item execution failed: " + menuPath);
            }

            return Success("Menu item executed.", new MenuPayload { menuPath = menuPath });
        }

        private static EditorRpcMethodResult ExecuteEditorStaticMethod(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var typeName = GetRequiredString(args, "type_name");
            var methodToCall = GetRequiredString(args, "method_name");
            if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(methodToCall))
            {
                return Failure("execute_editor_static_method requires type_name and method_name.");
            }

            var resolvedType = ResolveTypeByName(typeName, null);
            if (resolvedType == null)
            {
                return Failure("Could not resolve type: " + typeName);
            }

            var method = resolvedType.GetMethod(methodToCall, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null || method.GetParameters().Length != 0)
            {
                return Failure("Could not resolve a parameterless static method: " + typeName + "." + methodToCall);
            }

            method.Invoke(null, null);
            return Success("Static editor method executed.", new EditorMethodPayload
            {
                typeName = resolvedType.FullName,
                methodName = method.Name
            });
        }

    }
}
