using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorRpc
{
    public static partial class EditorRpcMethods
    {
        static partial void RegisterTerrainMethods()
        {
            CachedMethods.Add(new EditorRpcMethodDefinition(
                "create_terrain",
                "terrain",
                "Create a Unity Terrain from a semantic description or structured terrain recipe.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "description", new EditorRpcParameterDefinition("string", "Optional semantic terrain description.", false) },
                    { "recipe_json", new EditorRpcParameterDefinition("string", "Optional JSON recipe with base_height, noise, and features[].", false) },
                    { "terrain_name", new EditorRpcParameterDefinition("string", "Terrain GameObject name. Default Generated_Terrain.", false) },
                    { "scene_path", new EditorRpcParameterDefinition("string", "Optional loaded scene path. Defaults to the active scene.", false) },
                    { "position", new EditorRpcParameterDefinition("vector3|string", "Terrain world position as x,y,z. Default 0,0,0.", false) },
                    { "size", new EditorRpcParameterDefinition("vector3|string", "Terrain size as x,y,z. Default 256,40,256.", false) },
                    { "heightmap_resolution", new EditorRpcParameterDefinition("integer", "Heightmap resolution: 33, 65, 129, 257, 513, or 1025. Default 257.", false) },
                    { "terrain_layer_paths", new EditorRpcParameterDefinition("array|string", "Optional TerrainLayer asset paths as array, comma list, or | list.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after creating the terrain. Default false.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "smooth_terrain",
                "terrain",
                "Apply the mandatory terrain smoothing and slope limit to an existing scene Terrain or TerrainData asset.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Optional scene Terrain hierarchy path.", false) },
                    { "terrain_data_path", new EditorRpcParameterDefinition("string", "Optional TerrainData asset path.", false) },
                    { "smoothing_passes", new EditorRpcParameterDefinition("integer", "Optional smoothing pass count. Values below the mandatory minimum are raised.", false) },
                    { "slope_limit", new EditorRpcParameterDefinition("float", "Optional max world-space rise per horizontal meter. Values above the mandatory cap are clamped.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after smoothing when path targets a scene Terrain. Default false.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "reserve_terrain_area",
                "terrain",
                "Reserve a flat rectangular area on an existing Terrain, with a smoothed transition band for buildings or gameplay spaces.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Optional scene Terrain hierarchy path.", false) },
                    { "terrain_data_path", new EditorRpcParameterDefinition("string", "Optional TerrainData asset path.", false) },
                    { "width", new EditorRpcParameterDefinition("float", "Reserved area width in terrain X world units. Default 100.", false) },
                    { "length", new EditorRpcParameterDefinition("float", "Reserved area length in terrain Z world units. Default 70.", false) },
                    { "center_x", new EditorRpcParameterDefinition("float", "Optional normalized center X from 0 to 1. If omitted, the highest suitable area is used.", false) },
                    { "center_z", new EditorRpcParameterDefinition("float", "Optional normalized center Z from 0 to 1. If omitted, the highest suitable area is used.", false) },
                    { "height", new EditorRpcParameterDefinition("float", "Optional target world height. If omitted, the current area average height is used.", false) },
                    { "blend_width", new EditorRpcParameterDefinition("float", "Smooth transition band width in world units. Default 18.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after reserving when path targets a scene Terrain. Default false.", false) }
                }));
        }
    }

    public static partial class EditorRpcMethodExecutor
    {
        private const string TerrainAssetFolder = "Assets/Generated/EditorRpc/Terrains";
        private const string DefaultTerrainLayerPath = TerrainAssetFolder + "/EditorRpc_DefaultTerrainLayer.terrainlayer";
        private const string DefaultTerrainTexturePath = TerrainAssetFolder + "/EditorRpc_DefaultTerrainTexture.asset";
        private const int MandatoryTerrainSmoothingPasses = 5;
        private const int MandatoryTerrainSlopeLimitPasses = 32;
        private const float MandatoryTerrainWorldSlopeLimit = 1.15f;
        private const float MinimumTerrainWorldSlopeLimit = 0.15f;

        static partial void RegisterTerrainExecutors()
        {
            Register("create_terrain", ExecuteCreateTerrain);
            Register("smooth_terrain", ExecuteSmoothTerrain);
            Register("reserve_terrain_area", ExecuteReserveTerrainArea);
        }

        private static EditorRpcMethodResult ExecuteCreateTerrain(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var description = GetString(args, "description", string.Empty);
            var recipeJson = GetString(args, "recipe_json", string.Empty);
            if (string.IsNullOrWhiteSpace(description) && string.IsNullOrWhiteSpace(recipeJson))
            {
                return Failure("create_terrain requires description or recipe_json.");
            }

            Scene scene;
            var scenePath = GetString(args, "scene_path", string.Empty);
            if (!ResolveTerrainTargetScene(scenePath, out scene))
            {
                return Failure(string.IsNullOrEmpty(scenePath)
                    ? "No valid active scene is available."
                    : "Target scene is not loaded: " + scenePath);
            }

            Vector3 position;
            if (!TryGetVector3(args, "position", out position))
            {
                position = Vector3.zero;
            }

            Vector3 size;
            if (!TryGetVector3(args, "size", out size))
            {
                size = new Vector3(256f, 40f, 256f);
            }

            if (size.x <= 0f || size.y <= 0f || size.z <= 0f)
            {
                return Failure("size must contain positive x, y, and z values.");
            }

            var resolution = GetInt(args, "heightmap_resolution", 257);
            if (!IsSupportedTerrainResolution(resolution))
            {
                return Failure("heightmap_resolution must be one of 33, 65, 129, 257, 513, or 1025.");
            }

            TerrainRecipe recipe;
            string recipeError;
            if (!TryBuildTerrainRecipe(description, recipeJson, out recipe, out recipeError))
            {
                return Failure(recipeError);
            }

            var warnings = new List<string>();
            var terrainData = new TerrainData
            {
                heightmapResolution = resolution,
                size = size
            };

            TerrainLayer[] layers;
            string layerError;
            if (!TryLoadTerrainLayers(args, out layers, out layerError))
            {
                return Failure(layerError);
            }

            if (layers.Length == 0)
            {
                layers = new[] { GetOrCreateDefaultTerrainLayer() };
            }

            terrainData.terrainLayers = layers;
            var terrainLayerPaths = GetTerrainLayerPaths(layers);

            float minHeight;
            float maxHeight;
            float maxNeighborWorldSlope;
            var heights = BuildTerrainHeights(recipe, resolution, size, out minHeight, out maxHeight, out maxNeighborWorldSlope, warnings);
            terrainData.SetHeights(0, 0, heights);

            var terrainName = SanitizeTerrainName(GetString(args, "terrain_name", "Generated_Terrain"));
            EnsureAssetFolderPath(TerrainAssetFolder);
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(TerrainAssetFolder + "/" + terrainName + ".asset");
            AssetDatabase.CreateAsset(terrainData, assetPath);
            AssetDatabase.SaveAssets();

            var go = Terrain.CreateTerrainGameObject(terrainData);
            go.name = GetUniqueRootName(scene, terrainName);
            go.transform.position = position;
            SceneManager.MoveGameObjectToScene(go, scene);
            EditorSceneManager.MarkSceneDirty(scene);
            SaveSceneIfRequested(args, go);

            Selection.activeGameObject = go;
            return Success("Terrain created.", new TerrainCreationPayload
            {
                sourceType = string.IsNullOrWhiteSpace(recipeJson) ? "description" : string.IsNullOrWhiteSpace(description) ? "recipe_json" : "description+recipe_json",
                terrainName = go.name,
                hierarchyPath = GetGameObjectPath(go),
                scenePath = scene.path,
                terrainDataAssetPath = assetPath,
                heightmapResolution = resolution,
                size = FormatVector3(size),
                position = FormatVector3(position),
                minHeight = minHeight * size.y,
                maxHeight = maxHeight * size.y,
                smoothingPasses = MandatoryTerrainSmoothingPasses,
                slopeLimit = MandatoryTerrainWorldSlopeLimit,
                maxNeighborWorldSlope = maxNeighborWorldSlope,
                terrainLayerPaths = terrainLayerPaths,
                warnings = warnings.ToArray()
            });
        }

        private static EditorRpcMethodResult ExecuteSmoothTerrain(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetString(args, "path", string.Empty);
            var terrainDataPath = GetString(args, "terrain_data_path", string.Empty);
            if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(terrainDataPath))
            {
                return Failure("smooth_terrain requires path or terrain_data_path.");
            }

            GameObject go = null;
            Terrain terrain = null;
            TerrainData terrainData = null;
            if (!string.IsNullOrEmpty(path))
            {
                if (!TryFindGameObject(path, out go))
                {
                    return Failure("GameObject not found at hierarchy path: " + path);
                }

                terrain = go.GetComponent<Terrain>();
                if (terrain == null)
                {
                    return Failure("GameObject does not have a Terrain component: " + path);
                }

                terrainData = terrain.terrainData;
                if (terrainData == null)
                {
                    return Failure("Terrain has no TerrainData: " + path);
                }
            }

            if (!string.IsNullOrEmpty(terrainDataPath))
            {
                terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainDataPath);
                if (terrainData == null)
                {
                    return Failure("TerrainData asset not found: " + terrainDataPath);
                }
            }

            var resolvedTerrainDataPath = AssetDatabase.GetAssetPath(terrainData);
            var resolution = terrainData.heightmapResolution;
            var heights = terrainData.GetHeights(0, 0, resolution, resolution);
            float beforeMin;
            float beforeMax;
            GetHeightRange(heights, out beforeMin, out beforeMax);
            var beforeMaxNeighborWorldSlope = GetMaxNeighborWorldSlope(heights, terrainData.size);

            var smoothingPasses = Mathf.Max(MandatoryTerrainSmoothingPasses, GetInt(args, "smoothing_passes", MandatoryTerrainSmoothingPasses));
            var slopeLimit = GetFloat(args, "slope_limit", MandatoryTerrainWorldSlopeLimit);
            if (slopeLimit <= 0f)
            {
                slopeLimit = MandatoryTerrainWorldSlopeLimit;
            }

            slopeLimit = Mathf.Clamp(slopeLimit, MinimumTerrainWorldSlopeLimit, MandatoryTerrainWorldSlopeLimit);

            var notes = new List<string>();
            ApplyMandatoryTerrainSmoothing(heights, terrainData.size, smoothingPasses, slopeLimit, notes);

            float afterMin;
            float afterMax;
            GetHeightRange(heights, out afterMin, out afterMax);
            var afterMaxNeighborWorldSlope = GetMaxNeighborWorldSlope(heights, terrainData.size);

            terrainData.SetHeights(0, 0, heights);
            EditorUtility.SetDirty(terrainData);
            AssetDatabase.SaveAssets();
            if (terrain != null)
            {
                terrain.Flush();
            }

            if (go != null)
            {
                MarkDirty(go);
                SaveSceneIfRequested(args, go);
                Selection.activeGameObject = go;
            }
            else if (!string.IsNullOrEmpty(resolvedTerrainDataPath))
            {
                Selection.activeObject = terrainData;
            }

            return Success("Terrain smoothed.", new TerrainSmoothingPayload
            {
                hierarchyPath = go != null ? GetGameObjectPath(go) : string.Empty,
                terrainDataAssetPath = resolvedTerrainDataPath,
                heightmapResolution = resolution,
                size = FormatVector3(terrainData.size),
                smoothingPasses = smoothingPasses,
                slopeLimit = slopeLimit,
                beforeMinHeight = beforeMin * terrainData.size.y,
                beforeMaxHeight = beforeMax * terrainData.size.y,
                beforeMaxNeighborWorldSlope = beforeMaxNeighborWorldSlope,
                afterMinHeight = afterMin * terrainData.size.y,
                afterMaxHeight = afterMax * terrainData.size.y,
                afterMaxNeighborWorldSlope = afterMaxNeighborWorldSlope,
                notes = notes.ToArray()
            });
        }

        private static EditorRpcMethodResult ExecuteReserveTerrainArea(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetString(args, "path", string.Empty);
            var terrainDataPath = GetString(args, "terrain_data_path", string.Empty);
            if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(terrainDataPath))
            {
                return Failure("reserve_terrain_area requires path or terrain_data_path.");
            }

            GameObject go = null;
            Terrain terrain = null;
            TerrainData terrainData = null;
            if (!string.IsNullOrEmpty(path))
            {
                if (!TryFindGameObject(path, out go))
                {
                    return Failure("GameObject not found at hierarchy path: " + path);
                }

                terrain = go.GetComponent<Terrain>();
                if (terrain == null)
                {
                    return Failure("GameObject does not have a Terrain component: " + path);
                }

                terrainData = terrain.terrainData;
                if (terrainData == null)
                {
                    return Failure("Terrain has no TerrainData: " + path);
                }
            }

            if (!string.IsNullOrEmpty(terrainDataPath))
            {
                terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainDataPath);
                if (terrainData == null)
                {
                    return Failure("TerrainData asset not found: " + terrainDataPath);
                }
            }

            var resolvedTerrainDataPath = AssetDatabase.GetAssetPath(terrainData);
            var resolution = terrainData.heightmapResolution;
            var size = terrainData.size;
            var width = Mathf.Clamp(GetFloat(args, "width", 100f), 4f, size.x);
            var length = Mathf.Clamp(GetFloat(args, "length", 70f), 4f, size.z);
            var blendWidth = Mathf.Clamp(GetFloat(args, "blend_width", 18f), 2f, Mathf.Min(size.x, size.z) * 0.25f);
            var heights = terrainData.GetHeights(0, 0, resolution, resolution);

            var centerX = GetFloat(args, "center_x", -1f);
            var centerZ = GetFloat(args, "center_z", -1f);
            Vector2 center;
            var autoPlaced = centerX < 0f || centerZ < 0f;
            if (autoPlaced)
            {
                center = FindHighestReservableTerrainCenter(heights, size, width, length, blendWidth);
            }
            else
            {
                center = ClampReserveCenter(new Vector2(centerX, centerZ), size, width, length, blendWidth);
            }

            float beforeMin;
            float beforeMax;
            GetTerrainAreaStats(heights, size, center, width, length, out var beforeAverage, out beforeMin, out beforeMax);

            var requestedHeight = GetFloat(args, "height", -1f);
            var targetHeight = requestedHeight >= 0f ? requestedHeight : beforeAverage * size.y;
            var targetNormalizedHeight = Mathf.Clamp01(targetHeight / Mathf.Max(0.001f, size.y));
            var preserveMask = BuildReservedTerrainMask(heights, size, center, width, length);

            ApplyReservedTerrainArea(heights, size, center, width, length, blendWidth, targetNormalizedHeight);
            SmoothTerrainHeights(heights, 3, preserveMask);
            LimitTerrainNeighborSlopes(heights, size, MandatoryTerrainWorldSlopeLimit * 0.96f, MandatoryTerrainSlopeLimitPasses, preserveMask);
            ApplyReservedTerrainArea(heights, size, center, width, length, blendWidth, targetNormalizedHeight);
            LimitTerrainNeighborSlopes(heights, size, MandatoryTerrainWorldSlopeLimit * 0.96f, MandatoryTerrainSlopeLimitPasses, preserveMask);
            ClampTerrainHeights(heights);

            GetTerrainAreaStats(heights, size, center, width, length, out var afterAverage, out var afterMin, out var afterMax);
            var maxNeighborWorldSlope = GetMaxNeighborWorldSlope(heights, size);

            terrainData.SetHeights(0, 0, heights);
            EditorUtility.SetDirty(terrainData);
            AssetDatabase.SaveAssets();
            if (terrain != null)
            {
                terrain.Flush();
            }

            if (go != null)
            {
                MarkDirty(go);
                SaveSceneIfRequested(args, go);
                Selection.activeGameObject = go;
            }
            else if (!string.IsNullOrEmpty(resolvedTerrainDataPath))
            {
                Selection.activeObject = terrainData;
            }

            var worldCenter = go != null
                ? go.transform.position + new Vector3(center.x * size.x, targetNormalizedHeight * size.y, center.y * size.z)
                : new Vector3(center.x * size.x, targetNormalizedHeight * size.y, center.y * size.z);

            return Success("Terrain area reserved.", new TerrainReservedAreaPayload
            {
                hierarchyPath = go != null ? GetGameObjectPath(go) : string.Empty,
                terrainDataAssetPath = resolvedTerrainDataPath,
                heightmapResolution = resolution,
                size = FormatVector3(size),
                width = width,
                length = length,
                blendWidth = blendWidth,
                center = FormatVector3(worldCenter),
                normalizedCenter = center.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                                   center.y.ToString("0.###", CultureInfo.InvariantCulture),
                targetHeight = targetNormalizedHeight * size.y,
                beforeAverageHeight = beforeAverage * size.y,
                beforeMinHeight = beforeMin * size.y,
                beforeMaxHeight = beforeMax * size.y,
                afterAverageHeight = afterAverage * size.y,
                afterMinHeight = afterMin * size.y,
                afterMaxHeight = afterMax * size.y,
                maxNeighborWorldSlope = maxNeighborWorldSlope,
                autoPlaced = autoPlaced
            });
        }

        private static bool ResolveTerrainTargetScene(string scenePath, out Scene scene)
        {
            if (!string.IsNullOrEmpty(scenePath))
            {
                return TryResolveScene(scenePath, out scene) && scene.IsValid() && scene.isLoaded;
            }

            scene = SceneManager.GetActiveScene();
            return scene.IsValid() && scene.isLoaded;
        }

        private static bool IsSupportedTerrainResolution(int resolution)
        {
            return resolution == 33 ||
                   resolution == 65 ||
                   resolution == 129 ||
                   resolution == 257 ||
                   resolution == 513 ||
                   resolution == 1025;
        }

        private static bool TryBuildTerrainRecipe(string description, string recipeJson, out TerrainRecipe recipe, out string error)
        {
            error = string.Empty;
            recipe = BuildRecipeFromDescription(description);

            if (string.IsNullOrWhiteSpace(recipeJson))
            {
                return true;
            }

            TerrainRecipe parsed;
            try
            {
                parsed = JsonUtility.FromJson<TerrainRecipe>(recipeJson);
            }
            catch (Exception e)
            {
                error = "recipe_json is not valid terrain recipe JSON: " + e.Message;
                return false;
            }

            if (parsed == null)
            {
                error = "recipe_json could not be parsed.";
                return false;
            }

            MergeRecipe(recipe, parsed);
            return true;
        }

        private static TerrainRecipe BuildRecipeFromDescription(string description)
        {
            var recipe = new TerrainRecipe
            {
                base_height = 0.12f,
                noise = new TerrainNoiseRecipe
                {
                    amplitude = 0.045f,
                    frequency = 3f,
                    octaves = 3,
                    persistence = 0.5f
                },
                features = new TerrainFeatureRecipe[0]
            };

            var features = new List<TerrainFeatureRecipe>();
            var text = (description ?? string.Empty).ToLowerInvariant();

            if (ContainsAny(text, "flat", "plain", "field"))
            {
                recipe.noise.amplitude = 0.012f;
                recipe.noise.frequency = 2f;
            }

            if (ContainsAny(text, "rolling", "hills", "hill"))
            {
                recipe.noise.amplitude = 0.065f;
                recipe.noise.frequency = 3.5f;
                features.Add(NewFeature("hill", 0.35f, 0.55f, 0.22f, 0.18f));
                features.Add(NewFeature("hill", 0.68f, 0.35f, 0.18f, 0.12f));
            }

            if (ContainsAny(text, "mountain", "peak", "summit"))
            {
                recipe.noise.amplitude = 0.07f;
                recipe.noise.frequency = 5f;
                features.Add(NewFeature("hill", 0.5f, 0.52f, 0.25f, 0.65f));
                features.Add(NewFeature("ridge", 0.48f, 0.5f, 0.12f, 0.25f, 1.05f, 35f));
            }

            if (ContainsAny(text, "ridge", "spine"))
            {
                features.Add(NewFeature("ridge", 0.5f, 0.5f, 0.1f, 0.22f, 1.1f, 25f));
            }

            if (ContainsAny(text, "valley", "ravine", "gully"))
            {
                features.Add(NewFeature("valley", 0.52f, 0.48f, 0.16f, 0.22f, 1.0f, -20f));
            }

            if (ContainsAny(text, "plateau", "mesa"))
            {
                features.Add(NewFeature("plateau", 0.5f, 0.5f, 0.27f, 0.32f));
            }

            if (ContainsAny(text, "basin", "crater", "bowl"))
            {
                features.Add(NewFeature("basin", 0.5f, 0.5f, 0.3f, 0.26f));
            }

            if (ContainsAny(text, "terrace", "stepped", "steps"))
            {
                features.Add(NewFeature("terrace", 0.5f, 0.5f, 0.48f, 0f, 5));
            }

            if (ContainsAny(text, "island"))
            {
                recipe.base_height = 0.04f;
                recipe.noise.amplitude = 0.035f;
                features.Add(NewFeature("hill", 0.5f, 0.5f, 0.46f, 0.22f));
                features.Add(NewFeature("basin", 0.5f, 0.5f, 0.7f, 0.08f));
            }

            if (features.Count == 0)
            {
                features.Add(NewFeature("hill", 0.42f, 0.45f, 0.24f, 0.16f));
                features.Add(NewFeature("valley", 0.68f, 0.58f, 0.15f, 0.1f, 0.65f, -35f));
            }

            recipe.features = features.ToArray();
            return recipe;
        }

        private static void MergeRecipe(TerrainRecipe target, TerrainRecipe source)
        {
            if (source.base_height >= 0f)
            {
                target.base_height = source.base_height;
            }

            if (source.noise != null)
            {
                if (target.noise == null)
                {
                    target.noise = new TerrainNoiseRecipe();
                }

                if (source.noise.amplitude >= 0f)
                {
                    target.noise.amplitude = source.noise.amplitude;
                }

                if (source.noise.frequency > 0f)
                {
                    target.noise.frequency = source.noise.frequency;
                }

                if (source.noise.octaves > 0)
                {
                    target.noise.octaves = source.noise.octaves;
                }

                if (source.noise.persistence > 0f)
                {
                    target.noise.persistence = source.noise.persistence;
                }
            }

            var mergedFeatures = new List<TerrainFeatureRecipe>();
            if (target.features != null)
            {
                mergedFeatures.AddRange(target.features);
            }

            if (source.features != null)
            {
                mergedFeatures.AddRange(source.features);
            }

            target.features = mergedFeatures.ToArray();
        }

        private static float[,] BuildTerrainHeights(
            TerrainRecipe recipe,
            int resolution,
            Vector3 size,
            out float minHeight,
            out float maxHeight,
            out float maxNeighborWorldSlope,
            List<string> warnings)
        {
            var heights = new float[resolution, resolution];
            minHeight = float.MaxValue;
            maxHeight = float.MinValue;

            var noise = recipe.noise ?? new TerrainNoiseRecipe();
            var baseHeight = Mathf.Clamp01(recipe.base_height >= 0f ? recipe.base_height : 0.12f);
            for (int z = 0; z < resolution; z++)
            {
                var v = z / (float)(resolution - 1);
                for (int x = 0; x < resolution; x++)
                {
                    var u = x / (float)(resolution - 1);
                    var height = baseHeight + EvaluateNoise(noise, u, v);
                    heights[z, x] = Mathf.Clamp01(height);
                }
            }

            var features = recipe.features ?? new TerrainFeatureRecipe[0];
            for (int i = 0; i < features.Length; i++)
            {
                ApplyTerrainFeature(heights, features[i], warnings);
            }

            ApplyMandatoryTerrainSmoothing(heights, size, MandatoryTerrainSmoothingPasses, MandatoryTerrainWorldSlopeLimit, warnings);

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    heights[z, x] = Mathf.Clamp01(heights[z, x]);
                    minHeight = Mathf.Min(minHeight, heights[z, x]);
                    maxHeight = Mathf.Max(maxHeight, heights[z, x]);
                }
            }

            if (minHeight == float.MaxValue)
            {
                minHeight = 0f;
                maxHeight = 0f;
            }

            maxNeighborWorldSlope = GetMaxNeighborWorldSlope(heights, size);
            return heights;
        }

        private static float EvaluateNoise(TerrainNoiseRecipe noise, float u, float v)
        {
            var amplitude = Mathf.Max(0f, noise.amplitude);
            if (amplitude <= 0f)
            {
                return 0f;
            }

            var frequency = Mathf.Max(0.001f, noise.frequency);
            var octaves = Mathf.Clamp(noise.octaves <= 0 ? 1 : noise.octaves, 1, 8);
            var persistence = Mathf.Clamp(noise.persistence <= 0f ? 0.5f : noise.persistence, 0.05f, 1f);
            var value = 0f;
            var weight = 1f;
            var weightTotal = 0f;
            for (int octave = 0; octave < octaves; octave++)
            {
                var f = frequency * Mathf.Pow(2f, octave);
                var n = Mathf.PerlinNoise((u + 17.31f) * f, (v + 43.79f) * f) - 0.5f;
                value += n * weight;
                weightTotal += weight;
                weight *= persistence;
            }

            return weightTotal > 0f ? amplitude * value / weightTotal : 0f;
        }

        private static void ApplyTerrainFeature(float[,] heights, TerrainFeatureRecipe feature, List<string> warnings)
        {
            if (feature == null || string.IsNullOrEmpty(feature.type))
            {
                warnings.Add("Skipped terrain feature without type.");
                return;
            }

            var type = feature.type.Trim().ToLowerInvariant();
            var resolution = heights.GetLength(0);
            var center = new Vector2(ClampNormalized(feature.x, 0.5f), ClampNormalized(feature.z, 0.5f));
            var radius = Mathf.Max(0.001f, feature.radius > 0f ? feature.radius : 0.2f);

            for (int z = 0; z < resolution; z++)
            {
                var v = z / (float)(resolution - 1);
                for (int x = 0; x < resolution; x++)
                {
                    var u = x / (float)(resolution - 1);
                    var current = heights[z, x];
                    var mask = EvaluateFeatureMask(feature, u, v, center, radius);
                    if (mask <= 0f)
                    {
                        continue;
                    }

                    if (type == "hill")
                    {
                        heights[z, x] = current + GetFeatureMagnitude(feature, 0.18f) * mask;
                    }
                    else if (type == "basin")
                    {
                        heights[z, x] = current - Mathf.Abs(GetFeatureMagnitude(feature, 0.16f)) * mask;
                    }
                    else if (type == "ridge")
                    {
                        heights[z, x] = current + GetFeatureMagnitude(feature, 0.18f) * mask;
                    }
                    else if (type == "valley")
                    {
                        heights[z, x] = current - Mathf.Abs(GetFeatureMagnitude(feature, 0.16f)) * mask;
                    }
                    else if (type == "plateau")
                    {
                        var level = feature.level >= 0f ? feature.level : Mathf.Max(current, 0.32f);
                        heights[z, x] = Mathf.Lerp(current, level, mask);
                    }
                    else if (type == "flatten")
                    {
                        var level = feature.level >= 0f ? feature.level : feature.height;
                        heights[z, x] = Mathf.Lerp(current, Mathf.Clamp01(level), mask);
                    }
                    else if (type == "terrace")
                    {
                        var steps = Mathf.Clamp(feature.steps > 0 ? feature.steps : 5, 2, 16);
                        var scaled = current * steps;
                        var lower = Mathf.Floor(scaled) / steps;
                        var upper = Mathf.Ceil(scaled) / steps;
                        var local = scaled - Mathf.Floor(scaled);
                        var terraced = Mathf.Lerp(lower, upper, Smooth01(local));
                        heights[z, x] = Mathf.Lerp(current, terraced, mask * 0.65f);
                    }
                    else
                    {
                        warnings.Add("Skipped unsupported terrain feature type: " + feature.type);
                        return;
                    }
                }
            }
        }

        private static float EvaluateFeatureMask(TerrainFeatureRecipe feature, float u, float v, Vector2 center, float radius)
        {
            var type = feature.type.Trim().ToLowerInvariant();
            if (type == "ridge" || type == "valley")
            {
                var angle = feature.angle * Mathf.Deg2Rad;
                var dx = u - center.x;
                var dz = v - center.y;
                var along = Mathf.Cos(angle) * dx + Mathf.Sin(angle) * dz;
                var across = -Mathf.Sin(angle) * dx + Mathf.Cos(angle) * dz;
                var halfLength = Mathf.Max(0.001f, (feature.length > 0f ? feature.length : 0.9f) * 0.5f);
                var halfWidth = Mathf.Max(0.001f, (feature.width > 0f ? feature.width : radius) * 0.5f);
                var longitudinal = 1f - Mathf.Clamp01(Mathf.Abs(along) / halfLength);
                var lateral = 1f - Mathf.Clamp01(Mathf.Abs(across) / halfWidth);
                return Smooth01(longitudinal) * Smooth01(lateral);
            }

            var distance = Vector2.Distance(new Vector2(u, v), center);
            return Smooth01(1f - Mathf.Clamp01(distance / radius));
        }

        private static Vector2 FindHighestReservableTerrainCenter(float[,] heights, Vector3 size, float width, float length, float blendWidth)
        {
            var resolution = heights.GetLength(0);
            var halfWidth = Mathf.Max(0.5f, width * 0.5f + blendWidth);
            var halfLength = Mathf.Max(0.5f, length * 0.5f + blendWidth);
            var marginX = Mathf.Clamp(Mathf.CeilToInt(halfWidth / Mathf.Max(0.001f, size.x) * (resolution - 1)), 1, resolution / 2);
            var marginZ = Mathf.Clamp(Mathf.CeilToInt(halfLength / Mathf.Max(0.001f, size.z) * (resolution - 1)), 1, resolution / 2);
            var step = Mathf.Max(2, Mathf.Min(marginX, marginZ) / 3);
            var bestScore = float.MinValue;
            var best = new Vector2(0.5f, 0.5f);

            for (int z = marginZ; z < resolution - marginZ; z += step)
            {
                for (int x = marginX; x < resolution - marginX; x += step)
                {
                    var center = new Vector2(x / (float)(resolution - 1), z / (float)(resolution - 1));
                    float average;
                    float min;
                    float max;
                    GetTerrainAreaStats(heights, size, center, width, length, out average, out min, out max);
                    var score = average - (max - min) * 0.2f;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = center;
                    }
                }
            }

            return ClampReserveCenter(best, size, width, length, blendWidth);
        }

        private static Vector2 ClampReserveCenter(Vector2 center, Vector3 size, float width, float length, float blendWidth)
        {
            var marginX = Mathf.Clamp01((width * 0.5f + blendWidth) / Mathf.Max(0.001f, size.x));
            var marginZ = Mathf.Clamp01((length * 0.5f + blendWidth) / Mathf.Max(0.001f, size.z));
            return new Vector2(
                Mathf.Clamp(center.x, marginX, 1f - marginX),
                Mathf.Clamp(center.y, marginZ, 1f - marginZ));
        }

        private static void ApplyReservedTerrainArea(
            float[,] heights,
            Vector3 size,
            Vector2 center,
            float width,
            float length,
            float blendWidth,
            float targetHeight)
        {
            var resolution = heights.GetLength(0);
            var centerX = center.x * size.x;
            var centerZ = center.y * size.z;
            var halfWidth = width * 0.5f;
            var halfLength = length * 0.5f;
            var safeBlend = Mathf.Max(0.001f, blendWidth);

            for (int z = 0; z < resolution; z++)
            {
                var worldZ = z / (float)(resolution - 1) * size.z;
                var dz = Mathf.Abs(worldZ - centerZ);
                if (dz > halfLength + safeBlend)
                {
                    continue;
                }

                for (int x = 0; x < resolution; x++)
                {
                    var worldX = x / (float)(resolution - 1) * size.x;
                    var dx = Mathf.Abs(worldX - centerX);
                    if (dx > halfWidth + safeBlend)
                    {
                        continue;
                    }

                    var xWeight = dx <= halfWidth ? 1f : 1f - Mathf.Clamp01((dx - halfWidth) / safeBlend);
                    var zWeight = dz <= halfLength ? 1f : 1f - Mathf.Clamp01((dz - halfLength) / safeBlend);
                    var mask = Smooth01(Mathf.Min(xWeight, zWeight));
                    heights[z, x] = Mathf.Lerp(heights[z, x], targetHeight, mask);
                }
            }
        }

        private static bool[,] BuildReservedTerrainMask(float[,] heights, Vector3 size, Vector2 center, float width, float length)
        {
            var resolution = heights.GetLength(0);
            var mask = new bool[resolution, resolution];
            var centerX = center.x * size.x;
            var centerZ = center.y * size.z;
            var halfWidth = width * 0.5f;
            var halfLength = length * 0.5f;

            for (int z = 0; z < resolution; z++)
            {
                var worldZ = z / (float)(resolution - 1) * size.z;
                for (int x = 0; x < resolution; x++)
                {
                    var worldX = x / (float)(resolution - 1) * size.x;
                    mask[z, x] = Mathf.Abs(worldX - centerX) <= halfWidth &&
                                 Mathf.Abs(worldZ - centerZ) <= halfLength;
                }
            }

            return mask;
        }

        private static void GetTerrainAreaStats(
            float[,] heights,
            Vector3 size,
            Vector2 center,
            float width,
            float length,
            out float average,
            out float min,
            out float max)
        {
            var resolution = heights.GetLength(0);
            var centerX = center.x * size.x;
            var centerZ = center.y * size.z;
            var halfWidth = width * 0.5f;
            var halfLength = length * 0.5f;
            var total = 0f;
            var count = 0;
            min = float.MaxValue;
            max = float.MinValue;

            for (int z = 0; z < resolution; z++)
            {
                var worldZ = z / (float)(resolution - 1) * size.z;
                if (Mathf.Abs(worldZ - centerZ) > halfLength)
                {
                    continue;
                }

                for (int x = 0; x < resolution; x++)
                {
                    var worldX = x / (float)(resolution - 1) * size.x;
                    if (Mathf.Abs(worldX - centerX) > halfWidth)
                    {
                        continue;
                    }

                    var height = Mathf.Clamp01(heights[z, x]);
                    total += height;
                    count++;
                    min = Mathf.Min(min, height);
                    max = Mathf.Max(max, height);
                }
            }

            if (count == 0)
            {
                average = 0f;
                min = 0f;
                max = 0f;
                return;
            }

            average = total / count;
        }

        private static void ApplyMandatoryTerrainSmoothing(
            float[,] heights,
            Vector3 size,
            int smoothingPasses,
            float worldSlopeLimit,
            List<string> notes)
        {
            var safeSmoothingPasses = Mathf.Max(MandatoryTerrainSmoothingPasses, smoothingPasses);
            var safeWorldSlopeLimit = Mathf.Clamp(worldSlopeLimit, MinimumTerrainWorldSlopeLimit, MandatoryTerrainWorldSlopeLimit);
            var enforcedWorldSlopeLimit = safeWorldSlopeLimit * 0.96f;

            ClampTerrainHeights(heights);
            SmoothTerrainHeights(heights, Mathf.Max(1, safeSmoothingPasses - 2));
            ClampTerrainHeights(heights);
            LimitTerrainNeighborSlopes(heights, size, enforcedWorldSlopeLimit, MandatoryTerrainSlopeLimitPasses);
            ClampTerrainHeights(heights);
            SmoothTerrainHeights(heights, 2);
            ClampTerrainHeights(heights);
            LimitTerrainNeighborSlopes(heights, size, enforcedWorldSlopeLimit, MandatoryTerrainSlopeLimitPasses);
            ClampTerrainHeights(heights);
            LimitTerrainNeighborSlopes(heights, size, enforcedWorldSlopeLimit, MandatoryTerrainSlopeLimitPasses);
            ClampTerrainHeights(heights);

            if (notes != null)
            {
                notes.Add("Applied mandatory terrain smoothing and slope limiting.");
            }
        }

        private static void SmoothTerrainHeights(float[,] heights, int passes, bool[,] preserveMask = null)
        {
            if (heights == null || passes <= 0)
            {
                return;
            }

            var resolution = heights.GetLength(0);
            if (resolution <= 2)
            {
                return;
            }

            var buffer = new float[resolution, resolution];
            for (int pass = 0; pass < passes; pass++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        if (IsTerrainCellPreserved(preserveMask, z, x))
                        {
                            buffer[z, x] = heights[z, x];
                            continue;
                        }

                        var total = 0f;
                        var weightTotal = 0f;
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            var nz = z + dz;
                            if (nz < 0 || nz >= resolution)
                            {
                                continue;
                            }

                            for (int dx = -1; dx <= 1; dx++)
                            {
                                var nx = x + dx;
                                if (nx < 0 || nx >= resolution)
                                {
                                    continue;
                                }

                                var weight = dx == 0 && dz == 0 ? 4f : dx == 0 || dz == 0 ? 2f : 1f;
                                total += heights[nz, nx] * weight;
                                weightTotal += weight;
                            }
                        }

                        buffer[z, x] = weightTotal > 0f ? total / weightTotal : heights[z, x];
                    }
                }

                for (int z = 0; z < resolution; z++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        heights[z, x] = buffer[z, x];
                    }
                }
            }
        }

        private static void LimitTerrainNeighborSlopes(float[,] heights, Vector3 size, float worldSlopeLimit, int passes, bool[,] preserveMask = null)
        {
            if (heights == null || passes <= 0)
            {
                return;
            }

            var resolution = heights.GetLength(0);
            if (resolution <= 1)
            {
                return;
            }

            var maxNormalizedDelta = GetMaxNormalizedNeighborDelta(size, resolution, worldSlopeLimit);
            for (int pass = 0; pass < passes; pass++)
            {
                var maxExcess = 0f;
                maxExcess = Mathf.Max(maxExcess, RelaxTerrainSlopePairs(heights, maxNormalizedDelta, true, preserveMask));
                maxExcess = Mathf.Max(maxExcess, RelaxTerrainSlopePairs(heights, maxNormalizedDelta, false, preserveMask));
                if (maxExcess <= 0.000001f)
                {
                    break;
                }
            }
        }

        private static float RelaxTerrainSlopePairs(float[,] heights, float maxDelta, bool forward, bool[,] preserveMask)
        {
            var resolution = heights.GetLength(0);
            var maxExcess = 0f;
            var zStart = forward ? 0 : resolution - 1;
            var zEnd = forward ? resolution : -1;
            var zStep = forward ? 1 : -1;
            var xStart = forward ? 0 : resolution - 1;
            var xEnd = forward ? resolution : -1;
            var xStep = forward ? 1 : -1;

            for (int z = zStart; z != zEnd; z += zStep)
            {
                for (int x = xStart; x != xEnd; x += xStep)
                {
                    if (x + xStep >= 0 && x + xStep < resolution)
                    {
                        maxExcess = Mathf.Max(maxExcess, RelaxTerrainSlopePair(heights, z, x, z, x + xStep, maxDelta, preserveMask));
                    }

                    if (z + zStep >= 0 && z + zStep < resolution)
                    {
                        maxExcess = Mathf.Max(maxExcess, RelaxTerrainSlopePair(heights, z, x, z + zStep, x, maxDelta, preserveMask));
                    }
                }
            }

            return maxExcess;
        }

        private static float RelaxTerrainSlopePair(float[,] heights, int za, int xa, int zb, int xb, float maxDelta, bool[,] preserveMask)
        {
            var a = heights[za, xa];
            var b = heights[zb, xb];
            var diff = a - b;
            var excess = Mathf.Abs(diff) - maxDelta;
            if (excess <= 0f)
            {
                return 0f;
            }

            var preserveA = IsTerrainCellPreserved(preserveMask, za, xa);
            var preserveB = IsTerrainCellPreserved(preserveMask, zb, xb);
            if (preserveA && preserveB)
            {
                return 0f;
            }

            if (preserveA || preserveB)
            {
                if (preserveA)
                {
                    heights[zb, xb] = diff > 0f ? a - maxDelta : a + maxDelta;
                }
                else
                {
                    heights[za, xa] = diff > 0f ? b + maxDelta : b - maxDelta;
                }

                return excess;
            }

            var adjustment = excess * 0.5f;
            if (diff > 0f)
            {
                heights[za, xa] = a - adjustment;
                heights[zb, xb] = b + adjustment;
            }
            else
            {
                heights[za, xa] = a + adjustment;
                heights[zb, xb] = b - adjustment;
            }

            return excess;
        }

        private static bool IsTerrainCellPreserved(bool[,] preserveMask, int z, int x)
        {
            return preserveMask != null &&
                   z >= 0 &&
                   x >= 0 &&
                   z < preserveMask.GetLength(0) &&
                   x < preserveMask.GetLength(1) &&
                   preserveMask[z, x];
        }

        private static float GetMaxNormalizedNeighborDelta(Vector3 size, int resolution, float worldSlopeLimit)
        {
            var horizontalCellSize = GetHorizontalCellSize(size, resolution);
            var heightSize = Mathf.Max(0.001f, size.y);
            var maxDelta = Mathf.Clamp(worldSlopeLimit, MinimumTerrainWorldSlopeLimit, MandatoryTerrainWorldSlopeLimit) * horizontalCellSize / heightSize;
            return Mathf.Clamp(maxDelta, 0.0005f, 0.1f);
        }

        private static float GetMaxNeighborWorldSlope(float[,] heights, Vector3 size)
        {
            if (heights == null)
            {
                return 0f;
            }

            var resolution = heights.GetLength(0);
            if (resolution <= 1)
            {
                return 0f;
            }

            var horizontalCellSize = Mathf.Max(0.001f, GetHorizontalCellSize(size, resolution));
            var maxSlope = 0f;
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    if (x + 1 < resolution)
                    {
                        maxSlope = Mathf.Max(maxSlope, Mathf.Abs(heights[z, x] - heights[z, x + 1]) * size.y / horizontalCellSize);
                    }

                    if (z + 1 < resolution)
                    {
                        maxSlope = Mathf.Max(maxSlope, Mathf.Abs(heights[z, x] - heights[z + 1, x]) * size.y / horizontalCellSize);
                    }
                }
            }

            return maxSlope;
        }

        private static float GetHorizontalCellSize(Vector3 size, int resolution)
        {
            if (resolution <= 1)
            {
                return 1f;
            }

            return Mathf.Max(0.001f, Mathf.Min(size.x, size.z) / (resolution - 1));
        }

        private static void ClampTerrainHeights(float[,] heights)
        {
            if (heights == null)
            {
                return;
            }

            var resolution = heights.GetLength(0);
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    heights[z, x] = Mathf.Clamp01(heights[z, x]);
                }
            }
        }

        private static void GetHeightRange(float[,] heights, out float minHeight, out float maxHeight)
        {
            minHeight = float.MaxValue;
            maxHeight = float.MinValue;
            if (heights == null)
            {
                minHeight = 0f;
                maxHeight = 0f;
                return;
            }

            var resolution = heights.GetLength(0);
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    var height = Mathf.Clamp01(heights[z, x]);
                    minHeight = Mathf.Min(minHeight, height);
                    maxHeight = Mathf.Max(maxHeight, height);
                }
            }

            if (minHeight == float.MaxValue)
            {
                minHeight = 0f;
                maxHeight = 0f;
            }
        }

        private static bool TryLoadTerrainLayers(Dictionary<string, string> args, out TerrainLayer[] layers, out string error)
        {
            layers = new TerrainLayer[0];
            error = string.Empty;
            var raw = GetString(args, "terrain_layer_paths", string.Empty);
            var paths = ParseStringArray(raw);
            if (paths.Length == 0)
            {
                return true;
            }

            var loaded = new List<TerrainLayer>();
            for (int i = 0; i < paths.Length; i++)
            {
                var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(paths[i]);
                if (layer == null)
                {
                    error = "TerrainLayer asset not found: " + paths[i];
                    return false;
                }

                loaded.Add(layer);
            }

            layers = loaded.ToArray();
            return true;
        }

        private static TerrainLayer GetOrCreateDefaultTerrainLayer()
        {
            EnsureAssetFolderPath(TerrainAssetFolder);

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultTerrainTexturePath);
            if (texture == null)
            {
                texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true)
                {
                    name = "EditorRpc_DefaultTerrainTexture",
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Point
                };
                texture.SetPixel(0, 0, new Color(0.62f, 0.58f, 0.50f, 1f));
                texture.Apply();
                AssetDatabase.CreateAsset(texture, DefaultTerrainTexturePath);
            }

            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(DefaultTerrainLayerPath);
            if (layer == null)
            {
                layer = new TerrainLayer
                {
                    name = "EditorRpc_DefaultTerrainLayer"
                };
                AssetDatabase.CreateAsset(layer, DefaultTerrainLayerPath);
            }

            layer.diffuseTexture = texture;
            layer.tileSize = new Vector2(18f, 18f);
            layer.tileOffset = Vector2.zero;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        private static string[] GetTerrainLayerPaths(TerrainLayer[] layers)
        {
            if (layers == null || layers.Length == 0)
            {
                return new string[0];
            }

            var paths = new List<string>();
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null)
                {
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(layers[i]);
                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                }
            }

            return paths.ToArray();
        }

        private static void EnsureAssetFolderPath(string folderPath)
        {
            var parts = folderPath.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
            {
                return;
            }

            var current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string SanitizeTerrainName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Generated_Terrain";
            }

            foreach (var invalid in System.IO.Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid, '_');
            }

            return name.Trim();
        }

        private static string GetUniqueRootName(Scene scene, string desiredName)
        {
            var roots = scene.GetRootGameObjects();
            var candidate = desiredName;
            var index = 1;
            while (RootNameExists(roots, candidate))
            {
                candidate = desiredName + "_" + index.ToString(CultureInfo.InvariantCulture);
                index++;
            }

            return candidate;
        }

        private static bool RootNameExists(GameObject[] roots, string candidate)
        {
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && string.Equals(roots[i].name, candidate, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsAny(string text, params string[] needles)
        {
            for (int i = 0; i < needles.Length; i++)
            {
                if (text.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static TerrainFeatureRecipe NewFeature(string type, float x, float z, float radius, float height)
        {
            return new TerrainFeatureRecipe
            {
                type = type,
                x = x,
                z = z,
                radius = radius,
                height = height,
                strength = height,
                width = radius,
                length = radius * 2f
            };
        }

        private static TerrainFeatureRecipe NewFeature(string type, float x, float z, float width, float height, float length, float angle)
        {
            return new TerrainFeatureRecipe
            {
                type = type,
                x = x,
                z = z,
                radius = width,
                width = width,
                length = length,
                height = height,
                strength = height,
                angle = angle
            };
        }

        private static TerrainFeatureRecipe NewFeature(string type, float x, float z, float radius, float height, int steps)
        {
            var feature = NewFeature(type, x, z, radius, height);
            feature.steps = steps;
            return feature;
        }

        private static float GetFeatureMagnitude(TerrainFeatureRecipe feature, float defaultValue)
        {
            if (Mathf.Abs(feature.strength) > 0.0001f)
            {
                return feature.strength;
            }

            if (Mathf.Abs(feature.height) > 0.0001f)
            {
                return feature.height;
            }

            return defaultValue;
        }

        private static float ClampNormalized(float value, float defaultValue)
        {
            return value >= 0f ? Mathf.Clamp01(value) : defaultValue;
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        [Serializable]
        public sealed class TerrainRecipe
        {
            public float base_height = -1f;
            public TerrainNoiseRecipe noise = new TerrainNoiseRecipe();
            public TerrainFeatureRecipe[] features = new TerrainFeatureRecipe[0];
        }

        [Serializable]
        public sealed class TerrainNoiseRecipe
        {
            public float amplitude = -1f;
            public float frequency = -1f;
            public int octaves;
            public float persistence = -1f;
        }

        [Serializable]
        public sealed class TerrainFeatureRecipe
        {
            public string type = string.Empty;
            public float x = -1f;
            public float z = -1f;
            public float radius = -1f;
            public float width = -1f;
            public float length = -1f;
            public float height;
            public float strength;
            public float angle;
            public float level = -1f;
            public int steps;
        }

        [Serializable]
        public sealed class TerrainCreationPayload
        {
            public string sourceType;
            public string terrainName;
            public string hierarchyPath;
            public string scenePath;
            public string terrainDataAssetPath;
            public int heightmapResolution;
            public string size;
            public string position;
            public float minHeight;
            public float maxHeight;
            public int smoothingPasses;
            public float slopeLimit;
            public float maxNeighborWorldSlope;
            public string[] terrainLayerPaths;
            public string[] warnings;
        }

        [Serializable]
        public sealed class TerrainSmoothingPayload
        {
            public string hierarchyPath;
            public string terrainDataAssetPath;
            public int heightmapResolution;
            public string size;
            public int smoothingPasses;
            public float slopeLimit;
            public float beforeMinHeight;
            public float beforeMaxHeight;
            public float beforeMaxNeighborWorldSlope;
            public float afterMinHeight;
            public float afterMaxHeight;
            public float afterMaxNeighborWorldSlope;
            public string[] notes;
        }

        [Serializable]
        public sealed class TerrainReservedAreaPayload
        {
            public string hierarchyPath;
            public string terrainDataAssetPath;
            public int heightmapResolution;
            public string size;
            public float width;
            public float length;
            public float blendWidth;
            public string center;
            public string normalizedCenter;
            public float targetHeight;
            public float beforeAverageHeight;
            public float beforeMinHeight;
            public float beforeMaxHeight;
            public float afterAverageHeight;
            public float afterMinHeight;
            public float afterMaxHeight;
            public float maxNeighborWorldSlope;
            public bool autoPlaced;
        }
    }
}
