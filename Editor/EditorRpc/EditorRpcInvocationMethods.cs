using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EditorRpc
{
    [Serializable]
    internal sealed class MethodInvocationArgumentDescriptor
    {
        public string type = string.Empty;
        public string value = string.Empty;
    }

    [Serializable]
    internal sealed class MethodInvocationArgumentWrapper
    {
        public MethodInvocationArgumentDescriptor[] items = new MethodInvocationArgumentDescriptor[0];
    }

    public static partial class EditorRpcMethodExecutor
    {
        private static EditorRpcMethodResult ExecuteInvokeEditorStaticMethod(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var typeName = GetRequiredString(args, "type_name");
            var targetMethodName = GetRequiredString(args, "method_name");
            var rawArguments = GetString(args, "arguments", string.Empty);
            var includeNonPublic = GetBool(args, "include_non_public", true);
            if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(targetMethodName))
            {
                return Failure("invoke_editor_static_method requires type_name and method_name.");
            }

            var resolvedType = ResolveTypeByName(typeName, null);
            if (resolvedType == null)
            {
                return Failure("Could not resolve type: " + typeName);
            }

            var flags = BindingFlags.Static | BindingFlags.Public;
            if (includeNonPublic)
            {
                flags |= BindingFlags.NonPublic;
            }

            MethodInvocationPayload payload;
            string error;
            if (!TryInvokeMethodOnTarget(
                    null,
                    resolvedType,
                    resolvedType.FullName,
                    targetMethodName,
                    rawArguments,
                    flags,
                    null,
                    out payload,
                    out error))
            {
                return Failure(error);
            }

            return Success("Editor static method invoked.", payload);
        }

        private static EditorRpcMethodResult ExecuteInvokeAssetObjectMethod(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var assetPath = GetRequiredString(args, "asset_path");
            var objectPath = GetString(args, "object_path", string.Empty);
            var componentType = GetString(args, "component_type", string.Empty);
            var componentIndex = GetInt(args, "component_index", 0);
            var targetMethodName = GetRequiredString(args, "method_name");
            var rawArguments = GetString(args, "arguments", string.Empty);
            var includeNonPublic = GetBool(args, "include_non_public", true);
            var saveAsset = GetBool(args, "save_asset", true);
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(targetMethodName))
            {
                return Failure("invoke_asset_object_method requires asset_path and method_name.");
            }

            var flags = BindingFlags.Instance | BindingFlags.Public;
            if (includeNonPublic)
            {
                flags |= BindingFlags.NonPublic;
            }

            if (assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                var root = PrefabUtility.LoadPrefabContents(assetPath);
                if (root == null)
                {
                    return Failure("Failed to load prefab contents: " + assetPath);
                }

                try
                {
                    GameObject targetGo;
                    if (!TryFindPrefabGameObject(root, objectPath, out targetGo))
                    {
                        return Failure("Prefab object path not found: " + objectPath);
                    }

                    UnityEngine.Object target;
                    string resolveError;
                    if (!TryResolveSerializedTarget(targetGo, componentType, componentIndex, out target, out resolveError))
                    {
                        return Failure(resolveError);
                    }

                    MethodInvocationPayload payload;
                    string invokeError;
                    if (!TryInvokeMethodOnTarget(
                            target,
                            target.GetType(),
                            BuildAssetTargetPath(assetPath, targetGo, target),
                            targetMethodName,
                            rawArguments,
                            flags,
                            target,
                            out payload,
                            out invokeError))
                    {
                        return Failure(invokeError);
                    }

                    if (saveAsset)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                        AssetDatabase.SaveAssets();
                    }

                    return Success("Asset object method invoked.", payload);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
            {
                return Failure("Asset not found: " + assetPath);
            }

            if (!string.IsNullOrEmpty(objectPath) || !string.IsNullOrEmpty(componentType))
            {
                return Failure("object_path and component_type are only supported for prefab assets.");
            }

            MethodInvocationPayload assetPayload;
            string assetInvokeError;
            if (!TryInvokeMethodOnTarget(
                    asset,
                    asset.GetType(),
                    assetPath,
                    targetMethodName,
                    rawArguments,
                    flags,
                    asset,
                    out assetPayload,
                    out assetInvokeError))
            {
                return Failure(assetInvokeError);
            }

            if (saveAsset)
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }

            return Success("Asset object method invoked.", assetPayload);
        }

        private static EditorRpcMethodResult ExecuteInvokeSceneObjectMethod(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            var componentType = GetString(args, "component_type", string.Empty);
            var componentIndex = GetInt(args, "component_index", 0);
            var targetMethodName = GetRequiredString(args, "method_name");
            var rawArguments = GetString(args, "arguments", string.Empty);
            var includeNonPublic = GetBool(args, "include_non_public", true);
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(targetMethodName))
            {
                return Failure("invoke_scene_object_method requires path and method_name.");
            }

            GameObject go;
            if (!TryFindGameObject(path, out go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            UnityEngine.Object target;
            string resolveError;
            if (!TryResolveSerializedTarget(go, componentType, componentIndex, out target, out resolveError))
            {
                return Failure(resolveError);
            }

            var flags = BindingFlags.Instance | BindingFlags.Public;
            if (includeNonPublic)
            {
                flags |= BindingFlags.NonPublic;
            }

            MethodInvocationPayload payload;
            string invokeError;
            if (!TryInvokeMethodOnTarget(
                    target,
                    target.GetType(),
                    BuildSceneTargetPath(go, target),
                    targetMethodName,
                    rawArguments,
                    flags,
                    target,
                    out payload,
                    out invokeError))
            {
                return Failure(invokeError);
            }

            MarkDirty(go);
            SaveSceneIfRequested(args, go);
            return Success("Scene object method invoked.", payload);
        }

        private static bool TryInvokeMethodOnTarget(
            object targetInstance,
            Type targetType,
            string targetPath,
            string methodName,
            string rawArguments,
            BindingFlags bindingFlags,
            UnityEngine.Object contextTarget,
            out MethodInvocationPayload payload,
            out string error)
        {
            payload = null;
            error = string.Empty;
            if (targetType == null)
            {
                error = "Target type was null.";
                return false;
            }

            MethodInvocationArgumentDescriptor[] descriptors;
            if (!TryParseInvocationArguments(rawArguments, out descriptors, out error))
            {
                return false;
            }

            var methods = targetType.GetMethods(bindingFlags);
            var foundName = false;
            var lastCandidateError = string.Empty;

            for (int methodIndex = 0; methodIndex < methods.Length; methodIndex++)
            {
                var candidate = methods[methodIndex];
                if (!string.Equals(candidate.Name, methodName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foundName = true;

                object[] invocationArguments;
                string candidateError;
                if (!TryBuildInvocationArguments(candidate, descriptors, contextTarget, out invocationArguments, out candidateError))
                {
                    if (string.IsNullOrEmpty(lastCandidateError))
                    {
                        lastCandidateError = candidateError;
                    }

                    continue;
                }

                try
                {
                    var returnValue = candidate.Invoke(targetInstance, invocationArguments);
                    payload = new MethodInvocationPayload
                    {
                        targetPath = targetPath,
                        targetType = targetType.FullName,
                        methodName = candidate.Name,
                        argumentCount = invocationArguments.Length,
                        returnType = candidate.ReturnType == typeof(void) ? string.Empty : candidate.ReturnType.FullName,
                        returnValuePreview = candidate.ReturnType == typeof(void) ? string.Empty : FormatInvocationValue(returnValue)
                    };
                    return true;
                }
                catch (TargetInvocationException e)
                {
                    var inner = e.InnerException != null ? e.InnerException : e;
                    error = "Method invocation failed: " + inner.Message;
                    return false;
                }
                catch (Exception e)
                {
                    error = "Method invocation failed: " + e.Message;
                    return false;
                }
            }

            if (!foundName)
            {
                error = "Method not found on target type: " + targetType.FullName + "." + methodName;
                return false;
            }

            error = string.IsNullOrEmpty(lastCandidateError)
                ? "No matching overload found for method: " + targetType.FullName + "." + methodName
                : lastCandidateError;
            return false;
        }

        private static bool TryParseInvocationArguments(string rawArguments, out MethodInvocationArgumentDescriptor[] descriptors, out string error)
        {
            descriptors = new MethodInvocationArgumentDescriptor[0];
            error = string.Empty;
            rawArguments = NormalizeStringValue(rawArguments);
            if (string.IsNullOrEmpty(rawArguments))
            {
                return true;
            }

            try
            {
                var wrapper = JsonUtility.FromJson<MethodInvocationArgumentWrapper>("{\"items\":" + rawArguments + "}");
                if (wrapper == null || wrapper.items == null)
                {
                    return true;
                }

                descriptors = wrapper.items;
                for (int i = 0; i < descriptors.Length; i++)
                {
                    if (descriptors[i] == null)
                    {
                        descriptors[i] = new MethodInvocationArgumentDescriptor();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                error = "Could not parse arguments array: " + e.Message;
                return false;
            }
        }

        private static bool TryBuildInvocationArguments(
            MethodInfo method,
            MethodInvocationArgumentDescriptor[] descriptors,
            UnityEngine.Object contextTarget,
            out object[] values,
            out string error)
        {
            error = string.Empty;
            var parameters = method.GetParameters();
            descriptors = descriptors ?? new MethodInvocationArgumentDescriptor[0];
            values = new object[parameters.Length];

            if (descriptors.Length > parameters.Length)
            {
                error = "Method expects at most " + parameters.Length.ToString(CultureInfo.InvariantCulture) + " arguments: " + method.Name;
                return false;
            }

            for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
            {
                var parameter = parameters[parameterIndex];
                if (parameter.ParameterType.IsByRef)
                {
                    error = "ref and out parameters are not supported: " + parameter.Name;
                    return false;
                }

                if (parameterIndex < descriptors.Length)
                {
                    object value;
                    string convertError;
                    if (!TryConvertInvocationArgument(descriptors[parameterIndex], parameter.ParameterType, contextTarget, out value, out convertError))
                    {
                        error = "Argument " + parameterIndex.ToString(CultureInfo.InvariantCulture) + " could not be converted for " + method.Name + ": " + convertError;
                        return false;
                    }

                    values[parameterIndex] = value;
                    continue;
                }

                if (!parameter.IsOptional)
                {
                    error = "Method requires " + GetRequiredParameterCount(parameters).ToString(CultureInfo.InvariantCulture) + " arguments: " + method.Name;
                    return false;
                }

                values[parameterIndex] = parameter.DefaultValue == DBNull.Value
                    ? GetDefaultValue(parameter.ParameterType)
                    : parameter.DefaultValue;
            }

            return true;
        }

        private static int GetRequiredParameterCount(ParameterInfo[] parameters)
        {
            var count = 0;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!parameters[i].IsOptional)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryConvertInvocationArgument(
            MethodInvocationArgumentDescriptor descriptor,
            Type parameterType,
            UnityEngine.Object contextTarget,
            out object value,
            out string error)
        {
            value = null;
            error = string.Empty;

            var rawTypeHint = descriptor != null ? NormalizeStringValue(descriptor.type) : string.Empty;
            var rawValue = descriptor != null ? NormalizeStringValue(descriptor.value) : string.Empty;
            var targetType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;

            if (!IsInvocationTypeHintCompatible(rawTypeHint, targetType))
            {
                error = "Type hint " + rawTypeHint + " does not match parameter type " + targetType.FullName;
                return false;
            }

            if (string.Equals(rawValue, "null", StringComparison.OrdinalIgnoreCase))
            {
                if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) == null)
                {
                    error = "null cannot be assigned to value type " + parameterType.FullName;
                    return false;
                }

                value = null;
                return true;
            }

            if (targetType == typeof(string))
            {
                value = rawValue;
                return true;
            }

            if (targetType == typeof(bool))
            {
                if (string.Equals(rawValue, "true", StringComparison.OrdinalIgnoreCase) || rawValue == "1")
                {
                    value = true;
                    return true;
                }

                if (string.Equals(rawValue, "false", StringComparison.OrdinalIgnoreCase) || rawValue == "0")
                {
                    value = false;
                    return true;
                }

                error = "Could not parse bool value: " + rawValue;
                return false;
            }

            if (targetType == typeof(int))
            {
                int intValue;
                if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
                {
                    error = "Could not parse int value: " + rawValue;
                    return false;
                }

                value = intValue;
                return true;
            }

            if (targetType == typeof(long))
            {
                long longValue;
                if (!long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
                {
                    error = "Could not parse long value: " + rawValue;
                    return false;
                }

                value = longValue;
                return true;
            }

            if (targetType == typeof(float))
            {
                float floatValue;
                if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out floatValue))
                {
                    error = "Could not parse float value: " + rawValue;
                    return false;
                }

                value = floatValue;
                return true;
            }

            if (targetType == typeof(double))
            {
                double doubleValue;
                if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue))
                {
                    error = "Could not parse double value: " + rawValue;
                    return false;
                }

                value = doubleValue;
                return true;
            }

            if (targetType == typeof(Vector2))
            {
                Vector2 vector2Value;
                if (!TryParseVector2(rawValue, out vector2Value))
                {
                    error = "Could not parse vector2 value: " + rawValue;
                    return false;
                }

                value = vector2Value;
                return true;
            }

            if (targetType == typeof(Vector3))
            {
                Vector3 vector3Value;
                if (!TryParseVector3(rawValue, out vector3Value))
                {
                    error = "Could not parse vector3 value: " + rawValue;
                    return false;
                }

                value = vector3Value;
                return true;
            }

            if (targetType == typeof(Color))
            {
                Color colorValue;
                if (!TryParseColor(rawValue, out colorValue))
                {
                    error = "Could not parse color value: " + rawValue;
                    return false;
                }

                value = colorValue;
                return true;
            }

            if (targetType.IsEnum)
            {
                try
                {
                    value = Enum.Parse(targetType, rawValue, true);
                    return true;
                }
                catch
                {
                    int enumIndex;
                    if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out enumIndex))
                    {
                        error = "Could not parse enum value: " + rawValue;
                        return false;
                    }

                    value = Enum.ToObject(targetType, enumIndex);
                    return true;
                }
            }

            if (targetType == typeof(Type))
            {
                var resolvedType = ResolveTypeByName(rawValue, null) ?? Type.GetType(rawValue);
                if (resolvedType == null)
                {
                    error = "Could not resolve Type value: " + rawValue;
                    return false;
                }

                value = resolvedType;
                return true;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
            {
                return TryResolveInvocationObjectReference(rawValue, targetType, contextTarget, out value, out error);
            }

            if (targetType == typeof(object))
            {
                value = rawValue;
                return true;
            }

            error = "Unsupported parameter type: " + targetType.FullName;
            return false;
        }

        private static bool IsInvocationTypeHintCompatible(string typeHint, Type parameterType)
        {
            if (string.IsNullOrEmpty(typeHint))
            {
                return true;
            }

            switch (typeHint.ToLowerInvariant())
            {
                case "string":
                    return parameterType == typeof(string) || parameterType == typeof(object);
                case "bool":
                case "boolean":
                    return parameterType == typeof(bool) || parameterType == typeof(object);
                case "int":
                    return parameterType == typeof(int) || parameterType == typeof(object);
                case "long":
                    return parameterType == typeof(long) || parameterType == typeof(object);
                case "float":
                    return parameterType == typeof(float) || parameterType == typeof(object);
                case "double":
                    return parameterType == typeof(double) || parameterType == typeof(object);
                case "vector2":
                    return parameterType == typeof(Vector2) || parameterType == typeof(object);
                case "vector3":
                    return parameterType == typeof(Vector3) || parameterType == typeof(object);
                case "color":
                    return parameterType == typeof(Color) || parameterType == typeof(object);
                case "enum":
                    return parameterType.IsEnum || parameterType == typeof(object);
                case "type":
                    return parameterType == typeof(Type) || parameterType == typeof(object);
                case "object_reference":
                    return typeof(UnityEngine.Object).IsAssignableFrom(parameterType) || parameterType == typeof(object);
            }

            var hintedType = ResolveTypeByName(typeHint, null) ?? Type.GetType(typeHint);
            if (hintedType == null)
            {
                return true;
            }

            return parameterType == typeof(object) ||
                   parameterType == hintedType ||
                   parameterType.IsAssignableFrom(hintedType) ||
                   hintedType.IsAssignableFrom(parameterType);
        }

        private static bool TryResolveInvocationObjectReference(
            string rawValue,
            Type targetType,
            UnityEngine.Object contextTarget,
            out object value,
            out string error)
        {
            value = null;
            error = string.Empty;

            UnityEngine.Object resolvedReference;
            if (!TryResolveObjectReference(rawValue, contextTarget, out resolvedReference, out error))
            {
                return false;
            }

            if (resolvedReference == null)
            {
                value = null;
                return true;
            }

            if (targetType.IsInstanceOfType(resolvedReference))
            {
                value = resolvedReference;
                return true;
            }

            var resolvedGameObject = resolvedReference as GameObject;
            if (resolvedGameObject != null)
            {
                if (targetType == typeof(Transform))
                {
                    value = resolvedGameObject.transform;
                    return true;
                }

                if (typeof(Component).IsAssignableFrom(targetType))
                {
                    var component = resolvedGameObject.GetComponent(targetType);
                    if (component == null)
                    {
                        error = "Referenced GameObject does not contain component type " + targetType.FullName;
                        return false;
                    }

                    value = component;
                    return true;
                }
            }

            var resolvedComponent = resolvedReference as Component;
            if (resolvedComponent != null)
            {
                if (targetType == typeof(GameObject))
                {
                    value = resolvedComponent.gameObject;
                    return true;
                }

                if (targetType == typeof(Transform))
                {
                    value = resolvedComponent.transform;
                    return true;
                }
            }

            error = "Resolved reference " + DescribeSelectionObject(resolvedReference) + " cannot be assigned to " + targetType.FullName;
            return false;
        }

        private static object GetDefaultValue(Type type)
        {
            if (type == null || !type.IsValueType)
            {
                return null;
            }

            return Activator.CreateInstance(type);
        }

        private static string FormatInvocationValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            var stringValue = value as string;
            if (stringValue != null)
            {
                return stringValue;
            }

            if (value is bool)
            {
                return ((bool)value) ? "true" : "false";
            }

            if (value is int || value is long || value is float || value is double)
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }

            if (value is Vector2)
            {
                var vector2 = (Vector2)value;
                return vector2.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                       vector2.y.ToString("0.###", CultureInfo.InvariantCulture);
            }

            if (value is Vector3)
            {
                return FormatVector3((Vector3)value);
            }

            if (value is Color)
            {
                var color = (Color)value;
                return color.r.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                       color.g.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                       color.b.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                       color.a.ToString("0.###", CultureInfo.InvariantCulture);
            }

            var unityObject = value as UnityEngine.Object;
            if (unityObject != null)
            {
                return DescribeSelectionObject(unityObject);
            }

            var enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                var count = 0;
                foreach (var ignored in enumerable)
                {
                    count++;
                }

                return value.GetType().Name + "(count=" + count.ToString(CultureInfo.InvariantCulture) + ")";
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }
}
