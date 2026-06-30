using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EditorRpc
{
    public static partial class EditorRpcMethodExecutor
    {
        private sealed class MaterialAssignmentRunState
        {
            public readonly List<string> changedTargets = new List<string>();
            public readonly List<string> messages = new List<string>();
            public int scannedRendererCount;
            public int matchedRendererCount;
            public int changedRendererCount;
            public int changedSlotCount;
            public int changedTargetCount;
            public int skippedTargetCount;
        }

        private sealed class MaterialAssignmentFilter
        {
            public string rendererType;
            public string rendererNameContains;
            public string hierarchyPathContains;
            public string currentMaterialNameContains;
            public string currentMaterialPath;
            public bool includeInactive;
            public bool dryRun;
            public bool saveOpenScenes;
            public bool specificSlotOnly;
            public int slotIndex;
        }

        private static EditorRpcMethodResult ExecuteBatchAssignMaterials(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var targetKind = GetRequiredString(args, "target_kind");
            var targets = ParseStringArray(GetString(args, "targets", string.Empty));
            var materialPath = GetRequiredString(args, "material_path");
            if (string.IsNullOrEmpty(targetKind))
            {
                return Failure("batch_assign_materials requires target_kind.");
            }

            if (targets.Length == 0)
            {
                return Failure("batch_assign_materials requires at least one target.");
            }

            Material targetMaterial;
            string materialError;
            if (!TryResolveTargetMaterial(materialPath, out targetMaterial, out materialError))
            {
                return Failure(materialError);
            }

            var filter = new MaterialAssignmentFilter
            {
                rendererType = GetString(args, "renderer_type", string.Empty),
                rendererNameContains = GetString(args, "renderer_name_contains", string.Empty),
                hierarchyPathContains = GetString(args, "hierarchy_path_contains", string.Empty),
                currentMaterialNameContains = GetString(args, "current_material_name_contains", string.Empty),
                currentMaterialPath = GetString(args, "current_material_path", string.Empty),
                includeInactive = GetBool(args, "include_inactive", true),
                dryRun = GetBool(args, "dry_run", false),
                saveOpenScenes = GetBool(args, "save_open_scenes", false),
                specificSlotOnly = string.Equals(GetString(args, "slot_mode", "all"), "specific", StringComparison.OrdinalIgnoreCase),
                slotIndex = Mathf.Max(0, GetInt(args, "slot_index", 0))
            };

            var state = new MaterialAssignmentRunState();
            int resolvedTargetCount = 0;

            if (string.Equals(targetKind, "prefab_assets", StringComparison.OrdinalIgnoreCase))
            {
                resolvedTargetCount = ExecuteBatchAssignMaterialsForPrefabAssets(targets, targetMaterial, filter, state);
                if (!filter.dryRun)
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
            else if (string.Equals(targetKind, "scene_objects", StringComparison.OrdinalIgnoreCase))
            {
                resolvedTargetCount = ExecuteBatchAssignMaterialsForSceneObjects(targets, targetMaterial, filter, state);
                if (!filter.dryRun && filter.saveOpenScenes)
                {
                    EditorSceneManager.SaveOpenScenes();
                    AssetDatabase.SaveAssets();
                }
            }
            else
            {
                return Failure("Unsupported target_kind: " + targetKind + ". Use prefab_assets or scene_objects.");
            }

            var payload = new MaterialAssignmentPayload
            {
                targetKind = targetKind,
                materialPath = string.Equals(materialPath, "null", StringComparison.OrdinalIgnoreCase) ? "null" : materialPath,
                dryRun = filter.dryRun,
                includeInactive = filter.includeInactive,
                saveOpenScenes = filter.saveOpenScenes,
                slotMode = filter.specificSlotOnly ? "specific" : "all",
                slotIndex = filter.slotIndex,
                inputTargetCount = targets.Length,
                resolvedTargetCount = resolvedTargetCount,
                scannedRendererCount = state.scannedRendererCount,
                matchedRendererCount = state.matchedRendererCount,
                changedRendererCount = state.changedRendererCount,
                changedSlotCount = state.changedSlotCount,
                changedTargetCount = state.changedTargetCount,
                skippedTargetCount = state.skippedTargetCount,
                changedTargets = state.changedTargets.ToArray(),
                messages = state.messages.ToArray()
            };

            string message;
            if (filter.dryRun)
            {
                message = "Material assignment scan completed.";
            }
            else
            {
                message = "Material assignment completed.";
            }

            if (resolvedTargetCount == 0)
            {
                message = "No valid targets resolved for material assignment.";
            }

            return Success(message, payload);
        }

        private static int ExecuteBatchAssignMaterialsForPrefabAssets(
            string[] rawTargets,
            Material targetMaterial,
            MaterialAssignmentFilter filter,
            MaterialAssignmentRunState state)
        {
            var prefabPaths = ResolvePrefabAssetTargets(rawTargets, state);
            for (int i = 0; i < prefabPaths.Count; i++)
            {
                ProcessPrefabTarget(prefabPaths[i], targetMaterial, filter, state);
            }

            return prefabPaths.Count;
        }

        private static int ExecuteBatchAssignMaterialsForSceneObjects(
            string[] rawTargets,
            Material targetMaterial,
            MaterialAssignmentFilter filter,
            MaterialAssignmentRunState state)
        {
            int resolvedCount = 0;
            for (int i = 0; i < rawTargets.Length; i++)
            {
                GameObject root;
                if (!TryFindGameObject(rawTargets[i], out root))
                {
                    state.skippedTargetCount++;
                    AppendMaterialAssignmentMessage(state, "Scene object not found: " + rawTargets[i]);
                    continue;
                }

                resolvedCount++;
                ProcessSceneTarget(root, targetMaterial, filter, state);
            }

            return resolvedCount;
        }

        private static List<string> ResolvePrefabAssetTargets(string[] rawTargets, MaterialAssignmentRunState state)
        {
            var prefabPaths = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rawTargets.Length; i++)
            {
                var target = rawTargets[i];
                if (string.IsNullOrEmpty(target))
                {
                    continue;
                }

                if (AssetDatabase.IsValidFolder(target))
                {
                    var guids = AssetDatabase.FindAssets("t:Prefab", new[] { target });
                    for (int guidIndex = 0; guidIndex < guids.Length; guidIndex++)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guids[guidIndex]);
                        if (!string.IsNullOrEmpty(path) && seen.Add(path))
                        {
                            prefabPaths.Add(path);
                        }
                    }

                    continue;
                }

                if (!AssetExists(target))
                {
                    state.skippedTargetCount++;
                    AppendMaterialAssignmentMessage(state, "Asset target not found: " + target);
                    continue;
                }

                if (string.Equals(Path.GetExtension(target), ".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    if (seen.Add(target))
                    {
                        prefabPaths.Add(target);
                    }

                    continue;
                }

                if (AssetDatabase.LoadAssetAtPath<GameObject>(target) != null)
                {
                    state.skippedTargetCount++;
                    AppendMaterialAssignmentMessage(state, "Imported model assets are not directly writable by batch_assign_materials. Use a prefab wrapper instead: " + target);
                    continue;
                }

                state.skippedTargetCount++;
                AppendMaterialAssignmentMessage(state, "Unsupported prefab_assets target: " + target);
            }

            return prefabPaths;
        }

        private static void ProcessPrefabTarget(
            string prefabPath,
            Material targetMaterial,
            MaterialAssignmentFilter filter,
            MaterialAssignmentRunState state)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                state.skippedTargetCount++;
                AppendMaterialAssignmentMessage(state, "Failed to load prefab contents: " + prefabPath);
                return;
            }

            bool targetChanged = false;
            int targetChangedSlots = 0;
            int targetChangedRenderers = 0;

            try
            {
                var renderers = root.GetComponentsInChildren<Renderer>(filter.includeInactive);
                state.scannedRendererCount += renderers.Length;
                for (int i = 0; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    var hierarchyPath = BuildRelativeHierarchyPath(root.transform, renderer.transform);
                    bool rendererMatched;
                    int changedSlots = AssignMaterialOnRenderer(renderer, hierarchyPath, targetMaterial, filter, false, out rendererMatched);
                    if (rendererMatched)
                    {
                        state.matchedRendererCount++;
                    }

                    if (changedSlots <= 0)
                    {
                        continue;
                    }

                    targetChanged = true;
                    targetChangedSlots += changedSlots;
                    targetChangedRenderers++;
                    state.changedRendererCount++;
                    state.changedSlotCount += changedSlots;
                }

                if (targetChanged)
                {
                    state.changedTargetCount++;
                    state.changedTargets.Add(prefabPath);
                    AppendMaterialAssignmentMessage(
                        state,
                        (filter.dryRun ? "Prefab would change: " : "Prefab changed: ") +
                        prefabPath + " | renderers=" + targetChangedRenderers + " | slots=" + targetChangedSlots);

                    if (!filter.dryRun)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ProcessSceneTarget(
            GameObject root,
            Material targetMaterial,
            MaterialAssignmentFilter filter,
            MaterialAssignmentRunState state)
        {
            bool targetChanged = false;
            int targetChangedSlots = 0;
            int targetChangedRenderers = 0;
            var renderers = root.GetComponentsInChildren<Renderer>(filter.includeInactive);
            state.scannedRendererCount += renderers.Length;

            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var hierarchyPath = GetGameObjectPath(renderer.gameObject);
                bool rendererMatched;
                int changedSlots = AssignMaterialOnRenderer(renderer, hierarchyPath, targetMaterial, filter, true, out rendererMatched);
                if (rendererMatched)
                {
                    state.matchedRendererCount++;
                }

                if (changedSlots <= 0)
                {
                    continue;
                }

                targetChanged = true;
                targetChangedSlots += changedSlots;
                targetChangedRenderers++;
                state.changedRendererCount++;
                state.changedSlotCount += changedSlots;
            }

            if (!targetChanged)
            {
                return;
            }

            var targetPath = GetGameObjectPath(root);
            state.changedTargetCount++;
            state.changedTargets.Add(targetPath);
            AppendMaterialAssignmentMessage(
                state,
                (filter.dryRun ? "Scene target would change: " : "Scene target changed: ") +
                targetPath + " | renderers=" + targetChangedRenderers + " | slots=" + targetChangedSlots);

            if (!filter.dryRun)
            {
                MarkDirty(root);
            }
        }

        private static int AssignMaterialOnRenderer(
            Renderer renderer,
            string hierarchyPath,
            Material targetMaterial,
            MaterialAssignmentFilter filter,
            bool recordUndo,
            out bool rendererMatched)
        {
            rendererMatched = false;
            if (renderer == null)
            {
                return 0;
            }

            if (!MatchesRendererType(renderer, filter.rendererType))
            {
                return 0;
            }

            if (!ContainsIgnoreCase(renderer.gameObject.name, filter.rendererNameContains))
            {
                return 0;
            }

            if (!ContainsIgnoreCase(hierarchyPath, filter.hierarchyPathContains))
            {
                return 0;
            }

            var materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                return 0;
            }

            int changedSlots = 0;
            bool undoRecorded = false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (filter.specificSlotOnly && i != filter.slotIndex)
                {
                    continue;
                }

                var currentMaterial = materials[i];
                if (!MatchesCurrentMaterialFilter(currentMaterial, filter))
                {
                    continue;
                }

                rendererMatched = true;
                if (currentMaterial == targetMaterial)
                {
                    continue;
                }

                if (!filter.dryRun)
                {
                    if (recordUndo && !undoRecorded)
                    {
                        Undo.RecordObject(renderer, "Editor RPC Batch Assign Materials");
                        undoRecorded = true;
                    }

                    materials[i] = targetMaterial;
                }

                changedSlots++;
            }

            if (changedSlots > 0 && !filter.dryRun)
            {
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }

            return changedSlots;
        }

        private static bool TryResolveTargetMaterial(string materialPath, out Material material, out string error)
        {
            material = null;
            error = string.Empty;

            if (string.Equals(materialPath, "null", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.IsNullOrEmpty(materialPath))
            {
                error = "material_path is required.";
                return false;
            }

            material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                error = "Material not found at path: " + materialPath;
                return false;
            }

            return true;
        }

        private static bool MatchesRendererType(Renderer renderer, string rendererType)
        {
            if (string.IsNullOrEmpty(rendererType))
            {
                return true;
            }

            var resolvedType = ResolveTypeByName(rendererType, typeof(Renderer));
            if (resolvedType != null)
            {
                return resolvedType.IsAssignableFrom(renderer.GetType());
            }

            return string.Equals(renderer.GetType().Name, rendererType, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(renderer.GetType().FullName, rendererType, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesCurrentMaterialFilter(Material currentMaterial, MaterialAssignmentFilter filter)
        {
            if (!string.IsNullOrEmpty(filter.currentMaterialNameContains))
            {
                if (currentMaterial == null ||
                    currentMaterial.name.IndexOf(filter.currentMaterialNameContains, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(filter.currentMaterialPath))
            {
                var currentPath = currentMaterial != null ? AssetDatabase.GetAssetPath(currentMaterial) : string.Empty;
                if (!string.Equals(currentPath, filter.currentMaterialPath, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private static string BuildRelativeHierarchyPath(Transform root, Transform target)
        {
            if (root == null || target == null)
            {
                return string.Empty;
            }

            var names = new List<string>();
            var current = target;
            while (current != null)
            {
                names.Add(current.name);
                if (current == root)
                {
                    break;
                }

                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        private static bool ContainsIgnoreCase(string source, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return true;
            }

            if (string.IsNullOrEmpty(source))
            {
                return false;
            }

            return source.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AppendMaterialAssignmentMessage(MaterialAssignmentRunState state, string message)
        {
            if (state.messages.Count < 200)
            {
                state.messages.Add(message);
            }
        }
    }
}
