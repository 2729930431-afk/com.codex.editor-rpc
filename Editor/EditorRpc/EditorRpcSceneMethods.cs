using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorRpc
{
    public static partial class EditorRpcMethodExecutor
    {
        private static EditorRpcMethodResult ExecuteListLoadedScenes(string methodName, string argumentsJson)
        {
            var scenes = new List<SceneInfo>();
            var activeScene = SceneManager.GetActiveScene();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid())
                {
                    continue;
                }

                scenes.Add(new SceneInfo
                {
                    scenePath = scene.path,
                    sceneName = scene.name,
                    isDirty = scene.isDirty,
                    isLoaded = scene.isLoaded,
                    isActive = scene == activeScene
                });
            }

            return Success("Loaded scenes retrieved.", new LoadedScenesPayload
            {
                returnedCount = scenes.Count,
                scenes = scenes.ToArray()
            });
        }

        private static EditorRpcMethodResult ExecuteOpenScene(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            var saveCurrentIfDirty = GetBool(args, "save_current_if_dirty", false);
            if (string.IsNullOrEmpty(path))
            {
                return Failure("open_scene requires path.");
            }

            if (!AssetExists(path))
            {
                return Failure("Scene not found at path: " + path);
            }

            if (saveCurrentIfDirty)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            var openedScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            return Success("Scene opened.", new ScenePayload
            {
                scenePath = openedScene.path,
                sceneName = openedScene.name,
                isDirty = openedScene.isDirty
            });
        }

        private static EditorRpcMethodResult ExecuteOpenSceneAdditive(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            if (string.IsNullOrEmpty(path))
            {
                return Failure("open_scene_additive requires path.");
            }

            if (!AssetExists(path))
            {
                return Failure("Scene not found at path: " + path);
            }

            var openedScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            return Success("Scene opened additively.", new ScenePayload
            {
                scenePath = openedScene.path,
                sceneName = openedScene.name,
                isDirty = openedScene.isDirty
            });
        }

        private static EditorRpcMethodResult ExecuteSetActiveScene(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            if (string.IsNullOrEmpty(path))
            {
                return Failure("set_active_scene requires path.");
            }

            Scene scene;
            if (!TryResolveScene(path, out scene))
            {
                return Failure("Loaded scene not found: " + path);
            }

            if (!SceneManager.SetActiveScene(scene))
            {
                return Failure("Failed to set active scene: " + path);
            }

            return Success("Active scene updated.", new ScenePayload
            {
                scenePath = scene.path,
                sceneName = scene.name,
                isDirty = scene.isDirty
            });
        }

        private static EditorRpcMethodResult ExecuteCloseScene(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            var saveIfDirty = GetBool(args, "save_if_dirty", false);
            if (string.IsNullOrEmpty(path))
            {
                return Failure("close_scene requires path.");
            }

            Scene scene;
            if (!TryResolveScene(path, out scene))
            {
                return Failure("Loaded scene not found: " + path);
            }

            if (saveIfDirty && scene.isDirty)
            {
                EditorSceneManager.SaveScene(scene);
            }

            if (!EditorSceneManager.CloseScene(scene, true))
            {
                return Failure("Failed to close scene: " + path);
            }

            return Success("Scene closed.", new ScenePayload
            {
                scenePath = scene.path,
                sceneName = scene.name,
                isDirty = scene.isDirty
            });
        }

        private static EditorRpcMethodResult ExecuteSaveOpenScenes(string methodName, string argumentsJson)
        {
            var saved = EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            return Success(saved ? "Open scenes and assets saved." : "SaveOpenScenes returned false.", new SavePayload
            {
                saved = saved
            });
        }

        private static EditorRpcMethodResult ExecuteListHierarchy(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var scenePath = GetString(args, "scene_path", string.Empty);
            Scene scene;
            if (!TryResolveScene(scenePath, out scene))
            {
                return Failure("Scene is not loaded. Open the scene first, then list its hierarchy.");
            }

            var includeComponents = GetBool(args, "include_components", false);
            var includeInactive = GetBool(args, "include_inactive", true);
            var maxDepth = Mathf.Max(0, GetInt(args, "max_depth", 3));
            var limit = Mathf.Max(1, GetInt(args, "limit", 200));

            var nodes = new List<HierarchyNodeInfo>();
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length && nodes.Count < limit; i++)
            {
                AppendHierarchyNode(roots[i].transform, 0, maxDepth, includeComponents, includeInactive, nodes, limit);
            }

            return Success("Hierarchy snapshot created.", new HierarchyPayload
            {
                scenePath = scene.path,
                returnedCount = nodes.Count,
                nodes = nodes.ToArray()
            });
        }

        private static EditorRpcMethodResult ExecuteFindGameObjects(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var needle = GetRequiredString(args, "name_contains");
            if (string.IsNullOrEmpty(needle))
            {
                return Failure("find_game_objects requires name_contains.");
            }

            var includeInactive = GetBool(args, "include_inactive", true);
            var limit = Mathf.Max(1, GetInt(args, "limit", 20));
            var scenePath = GetString(args, "scene_path", string.Empty);
            var scenes = GetSearchScenes(scenePath);
            if (scenes.Count == 0)
            {
                return Failure("No loaded scenes available for search.");
            }

            var matches = new List<GameObjectInfo>();
            for (int i = 0; i < scenes.Count && matches.Count < limit; i++)
            {
                var roots = scenes[i].GetRootGameObjects();
                for (int j = 0; j < roots.Length && matches.Count < limit; j++)
                {
                    AppendMatchingObjects(roots[j].transform, needle, includeInactive, matches, limit);
                }
            }

            return Success("GameObject search completed.", new GameObjectSearchPayload
            {
                nameContains = needle,
                returnedCount = matches.Count,
                objects = matches.ToArray()
            });
        }

        private static EditorRpcMethodResult ExecuteSelectGameObject(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            if (string.IsNullOrEmpty(path))
            {
                return Failure("select_game_object requires path.");
            }

            GameObject go;
            if (!TryFindGameObject(path, out go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            return Success("GameObject selected.", BuildGameObjectPayload(go));
        }

        private static EditorRpcMethodResult ExecuteInspectSceneObject(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            var componentType = GetString(args, "component_type", string.Empty);
            var componentIndex = GetInt(args, "component_index", 0);
            var includeProperties = GetBool(args, "include_properties", false);
            var propertyLimit = Mathf.Max(1, GetInt(args, "property_limit", 100));
            if (string.IsNullOrEmpty(path))
            {
                return Failure("inspect_scene_object requires path.");
            }

            GameObject go;
            if (!TryFindGameObject(path, out go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            UnityEngine.Object target;
            string error;
            if (!TryResolveSerializedTarget(go, componentType, componentIndex, out target, out error))
            {
                return Failure(error);
            }

            return Success("Scene object inspected.", BuildSerializedTargetPayload(
                BuildSceneTargetPath(go, target),
                target,
                GetComponentTypeNames(go),
                includeProperties,
                propertyLimit));
        }

        private static EditorRpcMethodResult ExecuteCreateGameObject(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var name = GetRequiredString(args, "name");
            if (string.IsNullOrEmpty(name))
            {
                return Failure("create_game_object requires name.");
            }

            GameObject parent = null;
            var parentPath = GetString(args, "parent_path", string.Empty);
            if (!string.IsNullOrEmpty(parentPath) && !TryFindGameObject(parentPath, out parent))
            {
                return Failure("Parent GameObject not found at path: " + parentPath);
            }

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Editor RPC Create GameObject");

            if (parent != null)
            {
                go.transform.SetParent(parent.transform, false);
            }
            else
            {
                var scenePath = GetString(args, "scene_path", string.Empty);
                if (!string.IsNullOrEmpty(scenePath))
                {
                    Scene scene;
                    if (!TryResolveScene(scenePath, out scene))
                    {
                        UnityEngine.Object.DestroyImmediate(go);
                        return Failure("Target scene is not loaded: " + scenePath);
                    }

                    SceneManager.MoveGameObjectToScene(go, scene);
                }
            }

            Vector3 vector3Value;
            if (TryGetVector3(args, "position", out vector3Value))
            {
                go.transform.position = vector3Value;
            }

            if (TryGetVector3(args, "local_position", out vector3Value))
            {
                go.transform.localPosition = vector3Value;
            }

            if (TryGetVector3(args, "local_rotation_euler", out vector3Value))
            {
                go.transform.localEulerAngles = vector3Value;
            }

            if (TryGetVector3(args, "local_scale", out vector3Value))
            {
                go.transform.localScale = vector3Value;
            }

            MarkDirty(go);
            SaveSceneIfRequested(args, go);
            return Success("GameObject created.", BuildGameObjectPayload(go));
        }

        private static EditorRpcMethodResult ExecuteInstantiatePrefab(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var assetPath = GetRequiredString(args, "asset_path");
            if (string.IsNullOrEmpty(assetPath))
            {
                return Failure("instantiate_prefab requires asset_path.");
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                return Failure("Prefab not found at path: " + assetPath);
            }

            GameObject parent = null;
            var parentPath = GetString(args, "parent_path", string.Empty);
            if (!string.IsNullOrEmpty(parentPath) && !TryFindGameObject(parentPath, out parent))
            {
                return Failure("Parent GameObject not found at path: " + parentPath);
            }

            GameObject instance;
            if (parent != null)
            {
                instance = PrefabUtility.InstantiatePrefab(prefab, parent.transform) as GameObject;
            }
            else
            {
                var scenePath = GetString(args, "scene_path", string.Empty);
                if (!string.IsNullOrEmpty(scenePath))
                {
                    Scene targetScene;
                    if (!TryResolveScene(scenePath, out targetScene))
                    {
                        return Failure("Target scene is not loaded: " + scenePath);
                    }

                    instance = PrefabUtility.InstantiatePrefab(prefab, targetScene) as GameObject;
                }
                else
                {
                    instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                }
            }

            if (instance == null)
            {
                return Failure("Prefab instantiation failed: " + assetPath);
            }

            var name = GetString(args, "name", string.Empty);
            if (!string.IsNullOrEmpty(name))
            {
                instance.name = name;
            }

            Vector3 vector3Value;
            if (TryGetVector3(args, "position", out vector3Value))
            {
                instance.transform.position = vector3Value;
            }

            if (TryGetVector3(args, "local_position", out vector3Value))
            {
                instance.transform.localPosition = vector3Value;
            }

            if (TryGetVector3(args, "local_rotation_euler", out vector3Value))
            {
                instance.transform.localEulerAngles = vector3Value;
            }

            if (TryGetVector3(args, "local_scale", out vector3Value))
            {
                instance.transform.localScale = vector3Value;
            }

            MarkDirty(instance);
            SaveSceneIfRequested(args, instance);
            return Success("Prefab instantiated.", BuildGameObjectPayload(instance));
        }

        private static EditorRpcMethodResult ExecuteSetTransform(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            if (string.IsNullOrEmpty(path))
            {
                return Failure("set_transform requires path.");
            }

            GameObject go;
            if (!TryFindGameObject(path, out go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            Vector3 vector3Value;
            if (TryGetVector3(args, "position", out vector3Value))
            {
                go.transform.position = vector3Value;
            }

            if (TryGetVector3(args, "local_position", out vector3Value))
            {
                go.transform.localPosition = vector3Value;
            }

            if (TryGetVector3(args, "rotation_euler", out vector3Value))
            {
                go.transform.eulerAngles = vector3Value;
            }

            if (TryGetVector3(args, "local_rotation_euler", out vector3Value))
            {
                go.transform.localEulerAngles = vector3Value;
            }

            if (TryGetVector3(args, "local_scale", out vector3Value))
            {
                go.transform.localScale = vector3Value;
            }

            MarkDirty(go);
            SaveSceneIfRequested(args, go);
            return Success("Transform updated.", BuildGameObjectPayload(go));
        }

        private static EditorRpcMethodResult ExecuteAddComponent(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            var componentType = GetRequiredString(args, "component_type");
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(componentType))
            {
                return Failure("add_component requires path and component_type.");
            }

            GameObject go;
            if (!TryFindGameObject(path, out go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            var resolvedType = ResolveTypeByName(componentType, typeof(Component));
            if (resolvedType == null)
            {
                return Failure("Could not resolve component type: " + componentType);
            }

            var component = Undo.AddComponent(go, resolvedType);
            if (component == null)
            {
                return Failure("Failed to add component: " + componentType);
            }

            MarkDirty(go);
            SaveSceneIfRequested(args, go);
            return Success("Component added.", new ComponentPayload
            {
                gameObjectPath = GetGameObjectPath(go),
                componentType = component.GetType().FullName
            });
        }

        private static EditorRpcMethodResult ExecuteSetSceneObjectProperty(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            var componentType = GetString(args, "component_type", string.Empty);
            var componentIndex = GetInt(args, "component_index", 0);
            var propertyPath = GetRequiredString(args, "property_path");
            var valueType = GetString(args, "value_type", string.Empty);
            var value = GetString(args, "value", string.Empty);
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(propertyPath))
            {
                return Failure("set_scene_object_property requires path and property_path.");
            }

            GameObject go;
            if (!TryFindGameObject(path, out go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            UnityEngine.Object target;
            string error;
            if (!TryResolveSerializedTarget(go, componentType, componentIndex, out target, out error))
            {
                return Failure(error);
            }

            PropertySetPayload payload;
            if (!TrySetSerializedPropertyValue(new SerializedObject(target), propertyPath, valueType, value, out payload, out error))
            {
                return Failure(error);
            }

            MarkDirty(go);
            SaveSceneIfRequested(args, go);
            return Success("Scene object property updated.", payload);
        }

        private static EditorRpcMethodResult ExecuteDeleteGameObject(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            if (string.IsNullOrEmpty(path))
            {
                return Failure("delete_game_object requires path.");
            }

            GameObject go;
            if (!TryFindGameObject(path, out go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            var scene = go.scene;
            Undo.DestroyObjectImmediate(go);
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (GetBool(args, "save_scene", false))
                {
                    EditorSceneManager.SaveScene(scene);
                }
            }

            return Success("GameObject deleted.");
        }
    }
}
