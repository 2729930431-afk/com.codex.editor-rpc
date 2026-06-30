using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EditorRpc
{
    public static partial class EditorRpcMethods
    {
        static partial void RegisterAdvancedMethods()
        {
            CachedMethods.Add(new EditorRpcMethodDefinition(
                "batch_execute",
                "advanced",
                "Run multiple RPC operations across categories in one call, with optional stop-on-error behavior.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "operations", new EditorRpcParameterDefinition("array<object>", "JSON array of operation objects. Each object must include method plus that method's normal parameters.", true) },
                    { "stop_on_error", new EditorRpcParameterDefinition("boolean", "Stop at the first failed operation. Default true.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "search_types",
                "advanced",
                "Search loaded editor/runtime types by partial name and optional assignable base type.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "query", new EditorRpcParameterDefinition("string", "Optional partial type name or full name filter.", false) },
                    { "assignable_to", new EditorRpcParameterDefinition("string", "Optional base type or interface that returned types must be assignable to.", false) },
                    { "limit", new EditorRpcParameterDefinition("integer", "Maximum returned types. Default 50.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "list_type_methods",
                "advanced",
                "List callable methods on a resolved type for agent-side discovery.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "type_name", new EditorRpcParameterDefinition("string", "Type name or full type name.", true) },
                    { "method_name", new EditorRpcParameterDefinition("string", "Optional method name filter.", false) },
                    { "include_inherited", new EditorRpcParameterDefinition("boolean", "Include inherited methods. Default true.", false) },
                    { "include_non_public", new EditorRpcParameterDefinition("boolean", "Include non-public methods. Default false.", false) },
                    { "limit", new EditorRpcParameterDefinition("integer", "Maximum returned methods. Default 100.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "search_menu_items",
                "advanced",
                "Search registered Unity editor menu items by substring.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "query", new EditorRpcParameterDefinition("string", "Optional partial menu path filter.", false) },
                    { "limit", new EditorRpcParameterDefinition("integer", "Maximum returned menu items. Default 100.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "list_scene_components",
                "advanced",
                "List all components on a scene GameObject with serialized property previews.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the scene GameObject.", true) },
                    { "property_limit", new EditorRpcParameterDefinition("integer", "Maximum returned serialized properties per component. Default 40.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "analyze_scene_rendering",
                "advanced",
                "Analyze renderer, material, mesh, collider, and component pressure under a scene GameObject subtree.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the scene GameObject subtree root.", true) },
                    { "include_inactive", new EditorRpcParameterDefinition("boolean", "Include inactive GameObjects in total scans. Active renderer counts are still reported separately. Default true.", false) },
                    { "top_limit", new EditorRpcParameterDefinition("integer", "Maximum returned hot entries per ranking. Default 10.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "validate_workspace",
                "advanced",
                "Refresh assets, capture editor state, inspect recent console output, and optionally include loaded scene or hierarchy snapshots.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "refresh_assets", new EditorRpcParameterDefinition("boolean", "Run AssetDatabase.Refresh before validation. Default true.", false) },
                    { "console_count", new EditorRpcParameterDefinition("integer", "How many recent console entries to include. Default 30.", false) },
                    { "include_loaded_scenes", new EditorRpcParameterDefinition("boolean", "Include loaded scene summary. Default true.", false) },
                    { "include_hierarchy", new EditorRpcParameterDefinition("boolean", "Include hierarchy summary for one loaded scene. Default false.", false) },
                    { "scene_path", new EditorRpcParameterDefinition("string", "Optional scene path for hierarchy snapshot. Defaults to active scene when include_hierarchy is true.", false) },
                    { "hierarchy_max_depth", new EditorRpcParameterDefinition("integer", "Hierarchy snapshot max depth. Default 2.", false) },
                    { "hierarchy_limit", new EditorRpcParameterDefinition("integer", "Hierarchy snapshot node limit. Default 120.", false) },
                    { "clear_console_first", new EditorRpcParameterDefinition("boolean", "Clear the Unity console before validation. Default false.", false) }
                }));
        }
    }

    public static partial class EditorRpcMethodExecutor
    {
        private const int ConsoleErrorMask = 1;
        private const int ConsoleAssertMask = 2;
        private const int ConsoleWarningMask = 4;
        private const int ConsoleExceptionMask = 1 << 13;

        static partial void RegisterAdvancedExecutors()
        {
            Register("batch_execute", ExecuteBatchExecute);
            Register("search_types", ExecuteSearchTypes);
            Register("list_type_methods", ExecuteListTypeMethods);
            Register("search_menu_items", ExecuteSearchMenuItems);
            Register("list_scene_components", ExecuteListSceneComponents);
            Register("analyze_scene_rendering", ExecuteAnalyzeSceneRendering);
            Register("validate_workspace", ExecuteValidateWorkspace);
        }

        private static EditorRpcMethodResult ExecuteBatchExecute(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var rawOperations = GetRequiredString(args, "operations");
            if (!TrySplitJsonObjectArray(rawOperations, out var operations, out var error))
            {
                return Failure(error);
            }

            bool stopOnError = GetBool(args, "stop_on_error", true);
            var results = new List<BatchOperationResult>();
            int successCount = 0;
            int failedCount = 0;

            for (int index = 0; index < operations.Count; index++)
            {
                string operationJson = operations[index];
                var operationArgs = ParseArgs(operationJson);
                string operationMethod = GetString(operationArgs, "method", GetString(operationArgs, "operation", string.Empty));
                if (string.IsNullOrEmpty(operationMethod))
                {
                    failedCount++;
                    results.Add(new BatchOperationResult
                    {
                        index = index,
                        method = string.Empty,
                        success = false,
                        message = "Each batch operation requires method.",
                        payloadJson = string.Empty
                    });

                    if (stopOnError)
                    {
                        break;
                    }

                    continue;
                }

                var result = Execute(operationMethod, operationJson);
                bool operationSucceeded = result != null && result.success;
                if (operationSucceeded)
                {
                    successCount++;
                }
                else
                {
                    failedCount++;
                }

                results.Add(new BatchOperationResult
                {
                    index = index,
                    method = operationMethod,
                    success = operationSucceeded,
                    message = result != null ? result.message : "Operation returned no result.",
                    payloadJson = result != null ? result.payloadJson : string.Empty
                });

                if (!operationSucceeded && stopOnError)
                {
                    break;
                }
            }

            return Success("Batch execution completed.", new BatchOperationsPayload
            {
                requestedCount = operations.Count,
                succeededCount = successCount,
                failedCount = failedCount,
                results = results.ToArray()
            });
        }

        private static EditorRpcMethodResult ExecuteSearchTypes(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var query = GetString(args, "query", string.Empty);
            var assignableToName = GetString(args, "assignable_to", string.Empty);
            var limit = Mathf.Max(1, GetInt(args, "limit", 50));

            Type assignableTo = null;
            if (!string.IsNullOrEmpty(assignableToName))
            {
                assignableTo = ResolveTypeByName(assignableToName, null);
                if (assignableTo == null)
                {
                    return Failure("Could not resolve assignable_to type: " + assignableToName);
                }
            }

            var matches = SearchTypes(query, assignableTo, limit);
            var items = new TypeSearchInfo[matches.Length];
            for (int index = 0; index < matches.Length; index++)
            {
                items[index] = BuildTypeSearchInfo(matches[index]);
            }

            return Success("Type search completed.", new TypeSearchPayload
            {
                query = query,
                assignableTo = assignableTo != null ? assignableTo.FullName : string.Empty,
                returnedCount = items.Length,
                types = items
            });
        }

        private static EditorRpcMethodResult ExecuteListTypeMethods(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var typeName = GetRequiredString(args, "type_name");
            if (string.IsNullOrEmpty(typeName))
            {
                return Failure("list_type_methods requires type_name.");
            }

            var targetType = ResolveTypeByName(typeName, null);
            if (targetType == null)
            {
                return Failure("Could not resolve type: " + typeName);
            }

            var methodNameFilter = GetString(args, "method_name", string.Empty);
            var includeInherited = GetBool(args, "include_inherited", true);
            var includeNonPublic = GetBool(args, "include_non_public", false);
            var limit = Mathf.Max(1, GetInt(args, "limit", 100));
            var methods = BuildMethodSearchInfos(targetType, methodNameFilter, includeInherited, includeNonPublic, limit);

            return Success("Type methods listed.", new MethodSearchPayload
            {
                typeName = targetType.FullName,
                methodNameFilter = methodNameFilter,
                includeInherited = includeInherited,
                returnedCount = methods.Length,
                methods = methods
            });
        }

        private static EditorRpcMethodResult ExecuteSearchMenuItems(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var query = GetString(args, "query", string.Empty);
            var limit = Mathf.Max(1, GetInt(args, "limit", 100));
            var menuPaths = CollectMenuItemPaths(query, limit);

            return Success("Menu items listed.", new MenuSearchPayload
            {
                query = query,
                returnedCount = menuPaths.Length,
                menuItems = menuPaths
            });
        }

        private static EditorRpcMethodResult ExecuteListSceneComponents(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            var propertyLimit = Mathf.Max(1, GetInt(args, "property_limit", 40));
            if (string.IsNullOrEmpty(path))
            {
                return Failure("list_scene_components requires path.");
            }

            if (!TryFindGameObject(path, out var go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            var components = BuildComponentInfos(go, propertyLimit);
            return Success("Scene components listed.", new ComponentSearchPayload
            {
                path = GetGameObjectPath(go),
                assetPath = string.Empty,
                returnedCount = components.Length,
                components = components
            });
        }

        private static EditorRpcMethodResult ExecuteAnalyzeSceneRendering(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            if (string.IsNullOrEmpty(path))
            {
                return Failure("analyze_scene_rendering requires path.");
            }

            if (!TryFindGameObject(path, out var go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            var includeInactive = GetBool(args, "include_inactive", true);
            var topLimit = Mathf.Max(1, GetInt(args, "top_limit", 10));
            var total = new SceneRenderingAccumulator();
            CollectSceneRenderingStats(go.transform, total, includeInactive, 0);

            var childSummaries = new List<SceneRenderingChildSummary>();
            for (int childIndex = 0; childIndex < go.transform.childCount; childIndex++)
            {
                var child = go.transform.GetChild(childIndex);
                if (!includeInactive && !child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var childStats = new SceneRenderingAccumulator();
                CollectSceneRenderingStats(child, childStats, includeInactive, 0);
                childSummaries.Add(BuildChildSummary(child, childIndex, childStats));
            }

            childSummaries.Sort(CompareChildByRendererPressure);
            var rendererChildren = TakeChildSummaries(childSummaries, topLimit);

            childSummaries.Sort(CompareChildByTrianglePressure);
            var triangleChildren = TakeChildSummaries(childSummaries, topLimit);

            var payload = new SceneRenderingAnalysisPayload
            {
                path = GetGameObjectPath(go),
                scenePath = go.scene.path,
                activeSelf = go.activeSelf,
                activeInHierarchy = go.activeInHierarchy,
                includeInactive = includeInactive,
                directChildCount = go.transform.childCount,
                totals = BuildTotals(total),
                topChildrenByRendererCount = rendererChildren,
                topChildrenByTriangleCount = triangleChildren,
                topMaterials = BuildTopCounterInfos(total.materialCounters, topLimit, false),
                topMeshes = BuildTopCounterInfos(total.meshCounters, topLimit, true),
                topMeshColliders = BuildTopCounterInfos(total.meshColliderCounters, topLimit, true),
                topComponentTypes = BuildTopNameCounts(total.componentTypeCounts, topLimit)
            };

            return Success("Scene rendering analysis completed.", payload);
        }

        private static EditorRpcMethodResult ExecuteValidateWorkspace(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            if (GetBool(args, "clear_console_first", false))
            {
                var clearResult = ExecuteClearConsole("clear_console", "{}");
                if (clearResult == null || !clearResult.success)
                {
                    return Failure(clearResult != null ? clearResult.message : "Failed to clear console before validation.");
                }
            }

            if (GetBool(args, "refresh_assets", true))
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }

            var consoleCount = Mathf.Max(1, GetInt(args, "console_count", 30));
            var consoleEntries = GetConsoleEntries(consoleCount);
            var includeLoadedScenes = GetBool(args, "include_loaded_scenes", true);
            var includeHierarchy = GetBool(args, "include_hierarchy", false);

            LoadedScenesPayload loadedScenes = null;
            if (includeLoadedScenes)
            {
                loadedScenes = BuildLoadedScenesPayload();
            }

            HierarchyPayload hierarchy = null;
            if (includeHierarchy)
            {
                var scenePath = GetString(args, "scene_path", string.Empty);
                if (string.IsNullOrEmpty(scenePath))
                {
                    scenePath = BuildEditorStatePayload().activeScenePath;
                }

                if (!string.IsNullOrEmpty(scenePath))
                {
                    var hierarchyJson = "{\"scene_path\":\"" + EscapeJsonString(scenePath) + "\",\"max_depth\":\"" +
                                        Mathf.Max(0, GetInt(args, "hierarchy_max_depth", 2)).ToString() +
                                        "\",\"limit\":\"" + Mathf.Max(1, GetInt(args, "hierarchy_limit", 120)).ToString() + "\"}";
                    var hierarchyResult = ExecuteListHierarchy("list_hierarchy", hierarchyJson);
                    if (hierarchyResult != null && hierarchyResult.success && !string.IsNullOrEmpty(hierarchyResult.payloadJson))
                    {
                        hierarchy = JsonUtility.FromJson<HierarchyPayload>(hierarchyResult.payloadJson);
                    }
                }
            }

            int errorCount = CountConsoleEntriesByMask(consoleEntries, ConsoleErrorMask | ConsoleAssertMask | ConsoleExceptionMask);
            int warningCount = CountConsoleEntriesByMask(consoleEntries, ConsoleWarningMask);
            var payload = new ValidationPayload
            {
                editorState = BuildEditorStatePayload(),
                consoleCount = consoleEntries.Length,
                errorCount = errorCount,
                warningCount = warningCount,
                consoleEntries = consoleEntries,
                loadedScenes = loadedScenes,
                hierarchy = hierarchy
            };

            return Success("Workspace validation snapshot captured.", payload);
        }

        private static LoadedScenesPayload BuildLoadedScenesPayload()
        {
            var scenes = new List<SceneInfo>();
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            for (int index = 0; index < UnityEngine.SceneManagement.SceneManager.sceneCount; index++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(index);
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

            return new LoadedScenesPayload
            {
                returnedCount = scenes.Count,
                scenes = scenes.ToArray()
            };
        }

        private static void CollectSceneRenderingStats(Transform current, SceneRenderingAccumulator stats, bool includeInactive, int depth)
        {
            if (current == null)
            {
                return;
            }

            var go = current.gameObject;
            if (!includeInactive && !go.activeInHierarchy)
            {
                return;
            }

            stats.gameObjectCount++;
            if (go.activeInHierarchy)
            {
                stats.activeGameObjectCount++;
            }
            else
            {
                stats.inactiveGameObjectCount++;
            }

            if (depth > stats.maxDepth)
            {
                stats.maxDepth = depth;
            }

            if (go.isStatic)
            {
                stats.staticGameObjectCount++;
            }

            var staticFlags = GameObjectUtility.GetStaticEditorFlags(go);
            if ((staticFlags & StaticEditorFlags.BatchingStatic) != 0)
            {
                stats.batchingStaticGameObjectCount++;
            }

            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null)
                {
                    stats.missingComponentCount++;
                    AddNameCount(stats.componentTypeCounts, "MissingComponent", 1);
                    continue;
                }

                var componentType = component.GetType();
                AddNameCount(stats.componentTypeCounts, componentType.Name, 1);

                if (component is MonoBehaviour)
                {
                    stats.monoBehaviourCount++;
                }

                var renderer = component as Renderer;
                if (renderer != null)
                {
                    TrackRenderer(renderer, stats);
                    continue;
                }

                var meshCollider = component as MeshCollider;
                if (meshCollider != null)
                {
                    TrackMeshCollider(meshCollider, stats);
                    continue;
                }

                if (component is Collider)
                {
                    stats.colliderCount++;
                    continue;
                }

                if (component is LODGroup)
                {
                    stats.lodGroupCount++;
                    continue;
                }

                var light = component as Light;
                if (light != null)
                {
                    TrackLight(light, stats);
                    continue;
                }

                if (component is ReflectionProbe)
                {
                    stats.reflectionProbeCount++;
                    continue;
                }

                if (component is ParticleSystem)
                {
                    stats.particleSystemCount++;
                    continue;
                }

                if (component is Canvas)
                {
                    stats.canvasCount++;
                    continue;
                }

                if (component is Terrain)
                {
                    stats.terrainCount++;
                }
            }

            for (int childIndex = 0; childIndex < current.childCount; childIndex++)
            {
                CollectSceneRenderingStats(current.GetChild(childIndex), stats, includeInactive, depth + 1);
            }
        }

        private static void TrackRenderer(Renderer renderer, SceneRenderingAccumulator stats)
        {
            stats.rendererCount++;
            if (renderer.enabled)
            {
                stats.enabledRendererCount++;
            }

            var activeEnabled = renderer.enabled && renderer.gameObject.activeInHierarchy;
            if (activeEnabled)
            {
                stats.activeEnabledRendererCount++;
            }

            if (renderer is MeshRenderer)
            {
                stats.meshRendererCount++;
            }
            else if (renderer is SkinnedMeshRenderer)
            {
                stats.skinnedMeshRendererCount++;
            }
            else if (renderer is ParticleSystemRenderer)
            {
                stats.particleSystemRendererCount++;
            }
            else if (renderer is TrailRenderer)
            {
                stats.trailRendererCount++;
            }
            else if (renderer is LineRenderer)
            {
                stats.lineRendererCount++;
            }
            else if (renderer is SpriteRenderer)
            {
                stats.spriteRendererCount++;
            }

            var sharedMaterials = renderer.sharedMaterials;
            if (sharedMaterials != null)
            {
                stats.materialSlotCount += sharedMaterials.Length;
                for (int index = 0; index < sharedMaterials.Length; index++)
                {
                    TrackMaterial(sharedMaterials[index], stats);
                }
            }

            if (renderer.shadowCastingMode != ShadowCastingMode.Off)
            {
                stats.shadowCastingRendererCount++;
            }

            if (renderer.receiveShadows)
            {
                stats.shadowReceivingRendererCount++;
            }

            if (activeEnabled)
            {
                if (!stats.hasBounds)
                {
                    stats.bounds = renderer.bounds;
                    stats.hasBounds = true;
                }
                else
                {
                    stats.bounds.Encapsulate(renderer.bounds);
                }
            }

            var mesh = ResolveRendererMesh(renderer);
            if (mesh == null)
            {
                return;
            }

            stats.rendererMeshInstanceCount++;
            stats.rendererVertexCount += mesh.vertexCount;
            var triangleCount = GetMeshTriangleCount(mesh);
            stats.rendererTriangleCount += triangleCount;
            TrackMesh(mesh, triangleCount, stats.meshCounters);
        }

        private static void TrackMeshCollider(MeshCollider meshCollider, SceneRenderingAccumulator stats)
        {
            stats.colliderCount++;
            stats.meshColliderCount++;
            var mesh = meshCollider.sharedMesh;
            if (mesh == null)
            {
                return;
            }

            var triangleCount = GetMeshTriangleCount(mesh);
            stats.meshColliderTriangleCount += triangleCount;
            TrackMesh(mesh, triangleCount, stats.meshColliderCounters);
        }

        private static void TrackLight(Light light, SceneRenderingAccumulator stats)
        {
            stats.lightCount++;
            if (light.enabled)
            {
                stats.enabledLightCount++;
            }

            if (light.enabled && light.gameObject.activeInHierarchy)
            {
                stats.activeEnabledLightCount++;
            }

            if (light.lightmapBakeType == LightmapBakeType.Realtime)
            {
                stats.realtimeLightCount++;
            }
            else if (light.lightmapBakeType == LightmapBakeType.Mixed)
            {
                stats.mixedLightCount++;
            }
            else if (light.lightmapBakeType == LightmapBakeType.Baked)
            {
                stats.bakedLightCount++;
            }

            if (light.shadows != LightShadows.None)
            {
                stats.shadowCastingLightCount++;
            }
        }

        private static Mesh ResolveRendererMesh(Renderer renderer)
        {
            var skinned = renderer as SkinnedMeshRenderer;
            if (skinned != null)
            {
                return skinned.sharedMesh;
            }

            var meshFilter = renderer.GetComponent<MeshFilter>();
            return meshFilter != null ? meshFilter.sharedMesh : null;
        }

        private static long GetMeshTriangleCount(Mesh mesh)
        {
            if (mesh == null)
            {
                return 0L;
            }

            long triangleCount = 0L;
            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {
                triangleCount += (long)mesh.GetIndexCount(subMeshIndex) / 3L;
            }

            return triangleCount;
        }

        private static void TrackMaterial(Material material, SceneRenderingAccumulator stats)
        {
            if (material == null)
            {
                AddCounter(stats.materialCounters, "null", "null", string.Empty, false, 1, 0L, 0L);
                return;
            }

            var path = AssetDatabase.GetAssetPath(material);
            var key = string.IsNullOrEmpty(path) ? material.GetInstanceID().ToString() : path;
            AddCounter(stats.materialCounters, key, material.name, path, material.enableInstancing, 1, 0L, 0L);
        }

        private static void TrackMesh(Mesh mesh, long triangleCount, Dictionary<string, SceneRenderingCounter> counters)
        {
            if (mesh == null)
            {
                return;
            }

            var path = AssetDatabase.GetAssetPath(mesh);
            var key = string.IsNullOrEmpty(path) ? mesh.GetInstanceID().ToString() : path + "#" + mesh.name;
            AddCounter(counters, key, mesh.name, path, false, 1, triangleCount, mesh.vertexCount);
        }

        private static void AddCounter(Dictionary<string, SceneRenderingCounter> counters, string key, string name, string path, bool instancingEnabled, int countDelta, long triangleDelta, long vertexDelta)
        {
            SceneRenderingCounter counter;
            if (!counters.TryGetValue(key, out counter))
            {
                counter = new SceneRenderingCounter
                {
                    name = name,
                    path = path,
                    instancingEnabled = instancingEnabled
                };
                counters.Add(key, counter);
            }

            counter.count += countDelta;
            counter.triangleCount += triangleDelta;
            counter.vertexCount += vertexDelta;
            counter.instancingEnabled = counter.instancingEnabled || instancingEnabled;
        }

        private static void AddNameCount(Dictionary<string, int> counts, string key, int countDelta)
        {
            int count;
            counts.TryGetValue(key, out count);
            counts[key] = count + countDelta;
        }

        private static SceneRenderingTotals BuildTotals(SceneRenderingAccumulator stats)
        {
            return new SceneRenderingTotals
            {
                gameObjectCount = stats.gameObjectCount,
                activeGameObjectCount = stats.activeGameObjectCount,
                inactiveGameObjectCount = stats.inactiveGameObjectCount,
                maxDepth = stats.maxDepth,
                rendererCount = stats.rendererCount,
                enabledRendererCount = stats.enabledRendererCount,
                activeEnabledRendererCount = stats.activeEnabledRendererCount,
                meshRendererCount = stats.meshRendererCount,
                skinnedMeshRendererCount = stats.skinnedMeshRendererCount,
                particleSystemRendererCount = stats.particleSystemRendererCount,
                trailRendererCount = stats.trailRendererCount,
                lineRendererCount = stats.lineRendererCount,
                spriteRendererCount = stats.spriteRendererCount,
                materialSlotCount = stats.materialSlotCount,
                uniqueMaterialCount = stats.materialCounters.Count,
                instancingMaterialCount = CountInstancingMaterials(stats.materialCounters, true),
                nonInstancingMaterialCount = CountInstancingMaterials(stats.materialCounters, false),
                shadowCastingRendererCount = stats.shadowCastingRendererCount,
                shadowReceivingRendererCount = stats.shadowReceivingRendererCount,
                rendererMeshInstanceCount = stats.rendererMeshInstanceCount,
                uniqueMeshCount = stats.meshCounters.Count,
                rendererTriangleCount = stats.rendererTriangleCount,
                rendererVertexCount = stats.rendererVertexCount,
                colliderCount = stats.colliderCount,
                meshColliderCount = stats.meshColliderCount,
                meshColliderTriangleCount = stats.meshColliderTriangleCount,
                uniqueMeshColliderMeshCount = stats.meshColliderCounters.Count,
                lodGroupCount = stats.lodGroupCount,
                lightCount = stats.lightCount,
                enabledLightCount = stats.enabledLightCount,
                activeEnabledLightCount = stats.activeEnabledLightCount,
                realtimeLightCount = stats.realtimeLightCount,
                mixedLightCount = stats.mixedLightCount,
                bakedLightCount = stats.bakedLightCount,
                shadowCastingLightCount = stats.shadowCastingLightCount,
                reflectionProbeCount = stats.reflectionProbeCount,
                terrainCount = stats.terrainCount,
                canvasCount = stats.canvasCount,
                particleSystemCount = stats.particleSystemCount,
                staticGameObjectCount = stats.staticGameObjectCount,
                batchingStaticGameObjectCount = stats.batchingStaticGameObjectCount,
                monoBehaviourCount = stats.monoBehaviourCount,
                missingComponentCount = stats.missingComponentCount,
                activeRendererBoundsCenter = stats.hasBounds ? FormatVector3(stats.bounds.center) : string.Empty,
                activeRendererBoundsSize = stats.hasBounds ? FormatVector3(stats.bounds.size) : string.Empty
            };
        }

        private static int CountInstancingMaterials(Dictionary<string, SceneRenderingCounter> materialCounters, bool enabled)
        {
            int count = 0;
            foreach (var pair in materialCounters)
            {
                if (pair.Value.instancingEnabled == enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private static SceneRenderingChildSummary BuildChildSummary(Transform child, int childIndex, SceneRenderingAccumulator stats)
        {
            return new SceneRenderingChildSummary
            {
                path = GetGameObjectPath(child.gameObject),
                name = child.name,
                childIndex = childIndex,
                activeSelf = child.gameObject.activeSelf,
                activeInHierarchy = child.gameObject.activeInHierarchy,
                gameObjectCount = stats.gameObjectCount,
                rendererCount = stats.rendererCount,
                activeEnabledRendererCount = stats.activeEnabledRendererCount,
                materialSlotCount = stats.materialSlotCount,
                uniqueMaterialCount = stats.materialCounters.Count,
                rendererTriangleCount = stats.rendererTriangleCount,
                rendererVertexCount = stats.rendererVertexCount,
                colliderCount = stats.colliderCount,
                meshColliderCount = stats.meshColliderCount,
                meshColliderTriangleCount = stats.meshColliderTriangleCount,
                lodGroupCount = stats.lodGroupCount,
                lightCount = stats.lightCount,
                activeEnabledLightCount = stats.activeEnabledLightCount,
                realtimeLightCount = stats.realtimeLightCount,
                shadowCastingLightCount = stats.shadowCastingLightCount,
                particleSystemCount = stats.particleSystemCount,
                monoBehaviourCount = stats.monoBehaviourCount,
                maxDepth = stats.maxDepth
            };
        }

        private static int CompareChildByRendererPressure(SceneRenderingChildSummary left, SceneRenderingChildSummary right)
        {
            int compare = right.activeEnabledRendererCount.CompareTo(left.activeEnabledRendererCount);
            if (compare != 0)
            {
                return compare;
            }

            compare = right.rendererCount.CompareTo(left.rendererCount);
            return compare != 0 ? compare : right.materialSlotCount.CompareTo(left.materialSlotCount);
        }

        private static int CompareChildByTrianglePressure(SceneRenderingChildSummary left, SceneRenderingChildSummary right)
        {
            int compare = right.rendererTriangleCount.CompareTo(left.rendererTriangleCount);
            if (compare != 0)
            {
                return compare;
            }

            compare = right.rendererVertexCount.CompareTo(left.rendererVertexCount);
            return compare != 0 ? compare : right.activeEnabledRendererCount.CompareTo(left.activeEnabledRendererCount);
        }

        private static SceneRenderingChildSummary[] TakeChildSummaries(List<SceneRenderingChildSummary> summaries, int limit)
        {
            var count = Mathf.Min(limit, summaries.Count);
            var result = new SceneRenderingChildSummary[count];
            for (int index = 0; index < count; index++)
            {
                result[index] = summaries[index];
            }

            return result;
        }

        private static SceneRenderingCounterInfo[] BuildTopCounterInfos(Dictionary<string, SceneRenderingCounter> counters, int limit, bool sortByTriangles)
        {
            var values = new List<SceneRenderingCounter>(counters.Values);
            values.Sort((left, right) =>
            {
                int compare = sortByTriangles
                    ? right.triangleCount.CompareTo(left.triangleCount)
                    : right.count.CompareTo(left.count);
                if (compare != 0)
                {
                    return compare;
                }

                return string.Compare(left.name, right.name, StringComparison.Ordinal);
            });

            var count = Mathf.Min(limit, values.Count);
            var result = new SceneRenderingCounterInfo[count];
            for (int index = 0; index < count; index++)
            {
                var counter = values[index];
                result[index] = new SceneRenderingCounterInfo
                {
                    name = counter.name,
                    path = counter.path,
                    count = counter.count,
                    instancingEnabled = counter.instancingEnabled,
                    triangleCount = counter.triangleCount,
                    vertexCount = counter.vertexCount
                };
            }

            return result;
        }

        private static SceneRenderingNameCount[] BuildTopNameCounts(Dictionary<string, int> counts, int limit)
        {
            var items = new List<SceneRenderingNameCount>();
            foreach (var pair in counts)
            {
                items.Add(new SceneRenderingNameCount
                {
                    name = pair.Key,
                    count = pair.Value
                });
            }

            items.Sort((left, right) =>
            {
                int compare = right.count.CompareTo(left.count);
                return compare != 0 ? compare : string.Compare(left.name, right.name, StringComparison.Ordinal);
            });

            var count = Mathf.Min(limit, items.Count);
            var result = new SceneRenderingNameCount[count];
            for (int index = 0; index < count; index++)
            {
                result[index] = items[index];
            }

            return result;
        }

        private sealed class SceneRenderingAccumulator
        {
            public int gameObjectCount;
            public int activeGameObjectCount;
            public int inactiveGameObjectCount;
            public int maxDepth;
            public int rendererCount;
            public int enabledRendererCount;
            public int activeEnabledRendererCount;
            public int meshRendererCount;
            public int skinnedMeshRendererCount;
            public int particleSystemRendererCount;
            public int trailRendererCount;
            public int lineRendererCount;
            public int spriteRendererCount;
            public int materialSlotCount;
            public int shadowCastingRendererCount;
            public int shadowReceivingRendererCount;
            public int rendererMeshInstanceCount;
            public long rendererTriangleCount;
            public long rendererVertexCount;
            public int colliderCount;
            public int meshColliderCount;
            public long meshColliderTriangleCount;
            public int lodGroupCount;
            public int lightCount;
            public int enabledLightCount;
            public int activeEnabledLightCount;
            public int realtimeLightCount;
            public int mixedLightCount;
            public int bakedLightCount;
            public int shadowCastingLightCount;
            public int reflectionProbeCount;
            public int terrainCount;
            public int canvasCount;
            public int particleSystemCount;
            public int staticGameObjectCount;
            public int batchingStaticGameObjectCount;
            public int monoBehaviourCount;
            public int missingComponentCount;
            public bool hasBounds;
            public Bounds bounds;
            public readonly Dictionary<string, SceneRenderingCounter> materialCounters = new Dictionary<string, SceneRenderingCounter>();
            public readonly Dictionary<string, SceneRenderingCounter> meshCounters = new Dictionary<string, SceneRenderingCounter>();
            public readonly Dictionary<string, SceneRenderingCounter> meshColliderCounters = new Dictionary<string, SceneRenderingCounter>();
            public readonly Dictionary<string, int> componentTypeCounts = new Dictionary<string, int>();
        }

        private sealed class SceneRenderingCounter
        {
            public string name;
            public string path;
            public int count;
            public bool instancingEnabled;
            public long triangleCount;
            public long vertexCount;
        }

        private static string[] CollectMenuItemPaths(string query, int limit)
        {
            query = NormalizeStringValue(query);
            bool hasQuery = !string.IsNullOrEmpty(query);
            var list = new List<string>();
            var names = Unsupported.GetSubmenus(string.Empty);
            if (names == null || names.Length == 0)
            {
                return new string[0];
            }

            for (int index = 0; index < names.Length && list.Count < limit; index++)
            {
                var name = names[index];
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                if (hasQuery && name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                list.Add(name);
            }

            return list.ToArray();
        }

        private static string EscapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return Regex.Replace(value, "[\\\\\"]", match => "\\" + match.Value);
        }
    }
}
