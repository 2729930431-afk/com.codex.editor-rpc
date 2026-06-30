using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EditorRpc
{
    public static partial class EditorRpcMethodExecutor
    {
        private static EditorRpcMethodResult ExecuteFindAssets(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var filter = GetString(args, "filter", string.Empty);
            var folderTokens = ParseStringArray(GetString(args, "folders", string.Empty));
            var limit = Mathf.Max(1, GetInt(args, "limit", 50));

            string[] guids = folderTokens.Length > 0
                ? AssetDatabase.FindAssets(filter, folderTokens)
                : AssetDatabase.FindAssets(filter);

            var assets = new List<AssetInfo>();
            for (int i = 0; i < guids.Length && assets.Count < limit; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                assets.Add(new AssetInfo
                {
                    guid = guids[i],
                    path = path,
                    typeName = type != null ? type.FullName : string.Empty
                });
            }

            return Success("Asset search completed.", new AssetSearchPayload
            {
                filter = filter,
                folders = folderTokens,
                totalCount = guids.Length,
                returnedCount = assets.Count,
                assets = assets.ToArray()
            });
        }

        private static EditorRpcMethodResult ExecuteSelectAsset(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetString(args, "path", string.Empty);
            if (string.IsNullOrEmpty(path))
            {
                var guid = GetString(args, "guid", string.Empty);
                if (!string.IsNullOrEmpty(guid))
                {
                    path = AssetDatabase.GUIDToAssetPath(guid);
                }
            }

            if (string.IsNullOrEmpty(path))
            {
                return Failure("select_asset requires path or guid.");
            }

            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
            {
                return Failure("Asset not found at path: " + path);
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            return Success("Asset selected.", new SelectionPayload
            {
                selection = new[] { "asset:" + path }
            });
        }

        private static EditorRpcMethodResult ExecuteListAssetSubObjects(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var assetPath = GetRequiredString(args, "asset_path");
            var typeFilter = GetString(args, "type_filter", string.Empty);
            var limit = Mathf.Max(1, GetInt(args, "limit", 200));
            if (string.IsNullOrEmpty(assetPath))
            {
                return Failure("list_asset_sub_objects requires asset_path.");
            }

            if (!AssetExists(assetPath))
            {
                return Failure("Asset not found at path: " + assetPath);
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            var results = new List<AssetSubObjectInfo>();

            for (int i = 0; i < assets.Length && results.Count < limit; i++)
            {
                var asset = assets[i];
                if (asset == null)
                {
                    continue;
                }

                var type = asset.GetType();
                if (!string.IsNullOrEmpty(typeFilter) &&
                    !string.Equals(type.Name, typeFilter, StringComparison.Ordinal) &&
                    !string.Equals(type.FullName, typeFilter, StringComparison.Ordinal))
                {
                    continue;
                }

                results.Add(new AssetSubObjectInfo
                {
                    name = asset.name,
                    typeName = type.FullName,
                    reference = DescribeSelectionObject(asset),
                    isMainAsset = asset == mainAsset
                });
            }

            return Success("Asset sub-objects listed.", new AssetSubObjectListPayload
            {
                assetPath = assetPath,
                typeFilter = typeFilter,
                totalCount = assets.Length,
                returnedCount = results.Count,
                objects = results.ToArray()
            });
        }

        private static EditorRpcMethodResult ExecuteRefreshAssets(string methodName, string argumentsJson)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            return Success("AssetDatabase.Refresh completed.");
        }

        private static EditorRpcMethodResult ExecuteReimportAsset(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            if (string.IsNullOrEmpty(path))
            {
                return Failure("reimport_asset requires path.");
            }

            if (!AssetExists(path))
            {
                return Failure("Asset not found at path: " + path);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return Success("Asset reimported.", new AssetPathPayload { path = path });
        }

        private static EditorRpcMethodResult ExecuteInspectAssetObject(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var assetPath = GetRequiredString(args, "asset_path");
            var objectPath = GetString(args, "object_path", string.Empty);
            var componentType = GetString(args, "component_type", string.Empty);
            var componentIndex = GetInt(args, "component_index", 0);
            var includeProperties = GetBool(args, "include_properties", false);
            var propertyLimit = Mathf.Max(1, GetInt(args, "property_limit", 100));
            if (string.IsNullOrEmpty(assetPath))
            {
                return Failure("inspect_asset_object requires asset_path.");
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
                    string error;
                    if (!TryResolveSerializedTarget(targetGo, componentType, componentIndex, out target, out error))
                    {
                        return Failure(error);
                    }

                    return Success("Asset object inspected.", BuildSerializedTargetPayload(
                        BuildAssetTargetPath(assetPath, targetGo, target),
                        target,
                        GetComponentTypeNames(targetGo),
                        includeProperties,
                        propertyLimit));
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

            return Success("Asset inspected.", BuildSerializedTargetPayload(
                assetPath,
                asset,
                new string[0],
                includeProperties,
                propertyLimit));
        }

        private static EditorRpcMethodResult ExecuteSetAssetObjectProperty(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var assetPath = GetRequiredString(args, "asset_path");
            var objectPath = GetString(args, "object_path", string.Empty);
            var componentType = GetString(args, "component_type", string.Empty);
            var componentIndex = GetInt(args, "component_index", 0);
            var propertyPath = GetRequiredString(args, "property_path");
            var valueType = GetString(args, "value_type", string.Empty);
            var value = GetString(args, "value", string.Empty);
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(propertyPath))
            {
                return Failure("set_asset_object_property requires asset_path and property_path.");
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
                    string error;
                    if (!TryResolveSerializedTarget(targetGo, componentType, componentIndex, out target, out error))
                    {
                        return Failure(error);
                    }

                    PropertySetPayload payload;
                    if (!TrySetSerializedPropertyValue(new SerializedObject(target), propertyPath, valueType, value, out payload, out error))
                    {
                        return Failure(error);
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                    AssetDatabase.SaveAssets();
                    return Success("Prefab property updated.", payload);
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

            PropertySetPayload setPayload;
            string setError;
            if (!TrySetSerializedPropertyValue(new SerializedObject(asset), propertyPath, valueType, value, out setPayload, out setError))
            {
                return Failure(setError);
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return Success("Asset property updated.", setPayload);
        }

        private static EditorRpcMethodResult ExecuteRemoveAssetObjectComponent(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var assetPath = GetRequiredString(args, "asset_path");
            var objectPath = GetString(args, "object_path", string.Empty);
            var componentType = GetRequiredString(args, "component_type");
            var componentIndex = GetInt(args, "component_index", 0);
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(componentType))
            {
                return Failure("remove_asset_object_component requires asset_path and component_type.");
            }

            if (!assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return Failure("remove_asset_object_component only supports prefab assets.");
            }

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
                string error;
                if (!TryResolveSerializedTarget(targetGo, componentType, componentIndex, out target, out error))
                {
                    return Failure(error);
                }

                var component = target as Component;
                if (component == null)
                {
                    return Failure("Resolved target is not a component: " + componentType);
                }

                if (component is Transform)
                {
                    return Failure("Transform or RectTransform cannot be removed from a prefab object.");
                }

                var removedTypeName = component.GetType().Name;
                UnityEngine.Object.DestroyImmediate(component, true);

                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                AssetDatabase.SaveAssets();
                return Success("Prefab component removed.", new RemovedComponentPayload
                {
                    assetPath = assetPath,
                    objectPath = GetPrefabObjectPath(root, targetGo),
                    componentType = removedTypeName,
                    componentIndex = componentIndex,
                    remainingComponentTypes = GetComponentTypeNames(targetGo)
                });
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static EditorRpcMethodResult ExecuteCreateEmptyPrefab(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var assetPath = GetRequiredString(args, "asset_path");
            var rootName = GetString(args, "root_name", string.Empty);
            if (string.IsNullOrEmpty(assetPath))
            {
                return Failure("create_empty_prefab requires asset_path.");
            }

            if (!assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return Failure("create_empty_prefab requires a .prefab asset_path.");
            }

            EnsureAssetParentDirectoryExists(assetPath);
            var tempRoot = new GameObject(string.IsNullOrEmpty(rootName) ? Path.GetFileNameWithoutExtension(assetPath) : rootName);
            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, assetPath);
                AssetDatabase.SaveAssets();
                return Success("Empty prefab created.", new AssetPathPayload
                {
                    path = AssetDatabase.GetAssetPath(prefab)
                });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tempRoot);
            }
        }

        private static EditorRpcMethodResult ExecuteEnsurePrefabChild(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var assetPath = GetRequiredString(args, "asset_path");
            var parentPath = GetString(args, "parent_path", string.Empty);
            var childName = GetRequiredString(args, "name");
            var componentTypeNames = ParseStringArray(GetString(args, "component_types", string.Empty));
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(childName))
            {
                return Failure("ensure_prefab_child requires asset_path and name.");
            }

            if (!assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return Failure("ensure_prefab_child requires a .prefab asset_path.");
            }

            Type[] componentTypes;
            string error;
            if (!TryResolvePrefabComponentTypes(componentTypeNames, out componentTypes, out error))
            {
                return Failure(error);
            }

            var root = PrefabUtility.LoadPrefabContents(assetPath);
            if (root == null)
            {
                return Failure("Failed to load prefab contents: " + assetPath);
            }

            try
            {
                GameObject parentGo;
                if (!TryFindPrefabGameObject(root, parentPath, out parentGo))
                {
                    return Failure("Prefab parent path not found: " + parentPath);
                }

                var childTransform = parentGo.transform.Find(childName);
                var created = childTransform == null;
                var childGo = created ? new GameObject(childName, componentTypes) : childTransform.gameObject;
                if (created)
                {
                    childGo.transform.SetParent(parentGo.transform, false);
                    childGo.layer = parentGo.layer;
                }
                else if (!TryEnsurePrefabComponents(childGo, componentTypes, out error))
                {
                    return Failure(error);
                }

                int siblingIndex;
                if (TryGetOptionalInt(args, "sibling_index", out siblingIndex))
                {
                    childGo.transform.SetSiblingIndex(Mathf.Max(0, siblingIndex));
                }

                bool activeSelf;
                if (TryGetOptionalBool(args, "active", out activeSelf))
                {
                    childGo.SetActive(activeSelf);
                }

                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                AssetDatabase.SaveAssets();
                return Success(created ? "Prefab child created." : "Prefab child ensured.", new PrefabObjectPayload
                {
                    assetPath = assetPath,
                    objectPath = GetPrefabObjectPath(root, childGo),
                    name = childGo.name,
                    created = created,
                    componentTypes = GetComponentTypeNames(childGo)
                });
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static EditorRpcMethodResult ExecuteSaveSceneObjectAsPrefab(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            var assetPath = GetRequiredString(args, "asset_path");
            var connectInstance = GetBool(args, "connect_instance", true);
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(assetPath))
            {
                return Failure("save_scene_object_as_prefab requires path and asset_path.");
            }

            GameObject go;
            if (!TryFindGameObject(path, out go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            EnsureAssetParentDirectoryExists(assetPath);
            if (connectInstance)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(go, assetPath, InteractionMode.AutomatedAction);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            }

            AssetDatabase.SaveAssets();
            return Success("Scene object saved as prefab.", new AssetPathPayload { path = assetPath });
        }

        private static bool TryResolvePrefabComponentTypes(string[] componentTypeNames, out Type[] componentTypes, out string error)
        {
            var resolvedTypes = new List<Type>();
            error = string.Empty;
            componentTypes = new Type[0];
            if (componentTypeNames == null)
            {
                return true;
            }

            for (int componentTypeIndex = 0; componentTypeIndex < componentTypeNames.Length; componentTypeIndex++)
            {
                var componentTypeName = componentTypeNames[componentTypeIndex];
                if (string.IsNullOrEmpty(componentTypeName))
                {
                    continue;
                }

                var resolvedType = ResolveTypeByName(componentTypeName, typeof(Component));
                if (resolvedType == null)
                {
                    error = "Could not resolve component type: " + componentTypeName;
                    return false;
                }

                if (resolvedType.IsAbstract)
                {
                    error = "Component type is abstract: " + componentTypeName;
                    return false;
                }

                if (!resolvedTypes.Contains(resolvedType))
                {
                    resolvedTypes.Add(resolvedType);
                }
            }

            componentTypes = resolvedTypes.ToArray();
            return true;
        }

        private static bool TryEnsurePrefabComponents(GameObject targetGo, Type[] componentTypes, out string error)
        {
            error = string.Empty;
            if (targetGo == null || componentTypes == null)
            {
                return true;
            }

            for (int componentTypeIndex = 0; componentTypeIndex < componentTypes.Length; componentTypeIndex++)
            {
                var componentType = componentTypes[componentTypeIndex];
                if (componentType == null || targetGo.GetComponent(componentType) != null)
                {
                    continue;
                }

                if (componentType == typeof(RectTransform))
                {
                    error = "RectTransform can only be supplied when creating a new UI object.";
                    return false;
                }

                targetGo.AddComponent(componentType);
            }

            return true;
        }

        private static string GetPrefabObjectPath(GameObject root, GameObject targetGo)
        {
            if (root == null || targetGo == null)
            {
                return string.Empty;
            }

            var pathParts = new List<string>();
            var current = targetGo.transform;
            while (current != null)
            {
                pathParts.Insert(0, current.name);
                if (current.gameObject == root)
                {
                    break;
                }

                current = current.parent;
            }

            return string.Join("/", pathParts.ToArray());
        }
    }
}
