using System;
using System.Collections.Generic;

namespace EditorRpc
{
    public static partial class EditorRpcMethods
    {
        private static readonly List<EditorRpcMethodDefinition> CachedMethods = new List<EditorRpcMethodDefinition>();

        static partial void RegisterAdvancedMethods();
        static partial void RegisterUiMethods();
        static partial void RegisterTerrainMethods();

        static EditorRpcMethods()
        {
            RegisterAllMethods();
        }

        private static void RegisterAllMethods()
        {
            if (CachedMethods.Count > 0)
            {
                return;
            }

            EditorRpcMethodExecutor.Initialize();

            RegisterSystemMethods();
            RegisterAssetMethods();
            RegisterMaterialMethods();
            RegisterSceneMethods();
            RegisterTerrainMethods();
            RegisterAdvancedMethods();
            RegisterUiMethods();
            RegisterConsoleMethods();
            RegisterAnimatorMethods();
        }

        private static void RegisterSystemMethods()
        {
            CachedMethods.Add(new EditorRpcMethodDefinition(
                "list_methods",
                "system",
                "List all available RPC methods and their parameter schemas.",
                new Dictionary<string, EditorRpcParameterDefinition>()));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "get_editor_state",
                "system",
                "Get the current Unity editor state, active scene, and current selection.",
                new Dictionary<string, EditorRpcParameterDefinition>()));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "enter_play_mode",
                "system",
                "Enter Unity Play Mode.",
                new Dictionary<string, EditorRpcParameterDefinition>()));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "exit_play_mode",
                "system",
                "Exit Unity Play Mode.",
                new Dictionary<string, EditorRpcParameterDefinition>()));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "set_play_mode_pause",
                "system",
                "Pause or resume Play Mode.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "paused", new EditorRpcParameterDefinition("boolean", "When true, pause Play Mode. When false, resume Play Mode.", true) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "execute_menu_item",
                "system",
                "Execute a Unity editor menu item by exact path.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "menu_path", new EditorRpcParameterDefinition("string", "Exact editor menu path.", true) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "execute_editor_static_method",
                "system",
                "Execute a parameterless static method on an editor-loaded type.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "type_name", new EditorRpcParameterDefinition("string", "Type name or full type name.", true) },
                    { "method_name", new EditorRpcParameterDefinition("string", "Static method name.", true) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "invoke_editor_static_method",
                "system",
                "Invoke a static editor-loaded method with optional arguments and a return payload.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "type_name", new EditorRpcParameterDefinition("string", "Type name or full type name.", true) },
                    { "method_name", new EditorRpcParameterDefinition("string", "Static method name.", true) },
                    { "arguments", new EditorRpcParameterDefinition("array<object>", "Optional JSON array like [{\"type\":\"int\",\"value\":\"1\"}]. Object references use asset: or scene: prefixes.", false) },
                    { "include_non_public", new EditorRpcParameterDefinition("boolean", "When true, allow non-public methods. Default true.", false) }
                }));
        }

        private static void RegisterAssetMethods()
        {
            CachedMethods.Add(new EditorRpcMethodDefinition(
                "find_assets",
                "assets",
                "Search assets by AssetDatabase filter and optional folders.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "filter", new EditorRpcParameterDefinition("string", "AssetDatabase filter, for example t:Scene Main.", false) },
                    { "folders", new EditorRpcParameterDefinition("array|string", "Optional folders as a JSON array or a | separated string.", false) },
                    { "limit", new EditorRpcParameterDefinition("integer", "Maximum returned assets. Default 50.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "select_asset",
                "assets",
                "Select and ping an asset by path or guid.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Asset path.", false) },
                    { "guid", new EditorRpcParameterDefinition("string", "Asset guid when path is not provided.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "list_asset_sub_objects",
                "assets",
                "List all sub-objects loaded from an asset path, optionally filtered by type.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "asset_path", new EditorRpcParameterDefinition("string", "Asset path.", true) },
                    { "type_filter", new EditorRpcParameterDefinition("string", "Optional type name or full type name, for example AnimationClip.", false) },
                    { "limit", new EditorRpcParameterDefinition("integer", "Maximum returned sub-objects. Default 200.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "refresh_assets",
                "assets",
                "Run AssetDatabase.Refresh.",
                new Dictionary<string, EditorRpcParameterDefinition>()));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "reimport_asset",
                "assets",
                "Reimport a specific asset path.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Asset path to reimport.", true) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "inspect_asset_object",
                "assets",
                "Inspect a prefab object or serialized asset and optionally list serialized properties.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "asset_path", new EditorRpcParameterDefinition("string", "Asset path.", true) },
                    { "object_path", new EditorRpcParameterDefinition("string", "Optional prefab hierarchy path relative to the root.", false) },
                    { "component_type", new EditorRpcParameterDefinition("string", "Optional component type on the prefab object.", false) },
                    { "component_index", new EditorRpcParameterDefinition("integer", "Optional zero-based index when multiple components match.", false) },
                    { "include_properties", new EditorRpcParameterDefinition("boolean", "Include serialized properties and previews.", false) },
                    { "property_limit", new EditorRpcParameterDefinition("integer", "Maximum returned properties. Default 100.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "set_asset_object_property",
                "assets",
                "Set a serialized property on a prefab object or serialized asset.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "asset_path", new EditorRpcParameterDefinition("string", "Asset path to modify.", true) },
                    { "property_path", new EditorRpcParameterDefinition("string", "SerializedProperty path.", true) },
                    { "value", new EditorRpcParameterDefinition("string", "New value.", true) },
                    { "object_path", new EditorRpcParameterDefinition("string", "Optional prefab hierarchy path relative to the root.", false) },
                    { "component_type", new EditorRpcParameterDefinition("string", "Optional component type on the prefab object.", false) },
                    { "component_index", new EditorRpcParameterDefinition("integer", "Optional zero-based index when multiple components match.", false) },
                    { "value_type", new EditorRpcParameterDefinition("string", "Value type: bool, int, float, string, enum, vector2, vector3, color, object_reference.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "remove_asset_object_component",
                "assets",
                "Remove a component from a prefab object.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "asset_path", new EditorRpcParameterDefinition("string", "Prefab asset path to modify.", true) },
                    { "component_type", new EditorRpcParameterDefinition("string", "Component type on the prefab object.", true) },
                    { "object_path", new EditorRpcParameterDefinition("string", "Optional prefab hierarchy path relative to the root.", false) },
                    { "component_index", new EditorRpcParameterDefinition("integer", "Optional zero-based index when multiple components match.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "ensure_prefab_child",
                "assets",
                "Create or complete a direct child GameObject inside a prefab asset.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "asset_path", new EditorRpcParameterDefinition("string", "Prefab asset path to modify.", true) },
                    { "name", new EditorRpcParameterDefinition("string", "Direct child GameObject name.", true) },
                    { "parent_path", new EditorRpcParameterDefinition("string", "Optional prefab hierarchy path of the parent. Defaults to the root.", false) },
                    { "component_types", new EditorRpcParameterDefinition("array|string", "Optional component type names to ensure, as JSON array or | separated string.", false) },
                    { "sibling_index", new EditorRpcParameterDefinition("integer", "Optional sibling index after creation or update.", false) },
                    { "active", new EditorRpcParameterDefinition("boolean", "Optional activeSelf state.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "invoke_asset_object_method",
                "assets",
                "Invoke a method on a prefab object, prefab component, or serialized asset without adding a temporary bridge method.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "asset_path", new EditorRpcParameterDefinition("string", "Asset path to target.", true) },
                    { "method_name", new EditorRpcParameterDefinition("string", "Method name to invoke.", true) },
                    { "arguments", new EditorRpcParameterDefinition("array<object>", "Optional JSON array like [{\"type\":\"string\",\"value\":\"foo\"}]. Object references use asset: or scene: prefixes.", false) },
                    { "object_path", new EditorRpcParameterDefinition("string", "Optional prefab hierarchy path relative to the root.", false) },
                    { "component_type", new EditorRpcParameterDefinition("string", "Optional component type on the prefab object.", false) },
                    { "component_index", new EditorRpcParameterDefinition("integer", "Optional zero-based index when multiple components match.", false) },
                    { "include_non_public", new EditorRpcParameterDefinition("boolean", "When true, allow non-public methods. Default true.", false) },
                    { "save_asset", new EditorRpcParameterDefinition("boolean", "When true, persist asset changes after invocation. Default true.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "create_empty_prefab",
                "assets",
                "Create a new prefab asset with an empty root GameObject.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "asset_path", new EditorRpcParameterDefinition("string", "Prefab asset path to create.", true) },
                    { "root_name", new EditorRpcParameterDefinition("string", "Optional root GameObject name.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "save_scene_object_as_prefab",
                "assets",
                "Save a scene object as a prefab asset.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the scene object.", true) },
                    { "asset_path", new EditorRpcParameterDefinition("string", "Prefab asset path to write.", true) },
                    { "connect_instance", new EditorRpcParameterDefinition("boolean", "When true, connect the scene object to the new prefab.", false) }
                }));
        }

        private static void RegisterSceneMethods()
        {
            CachedMethods.Add(new EditorRpcMethodDefinition(
                "open_scene",
                "scene",
                "Open a scene in single mode.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Scene asset path.", true) },
                    { "save_current_if_dirty", new EditorRpcParameterDefinition("boolean", "When true, save dirty open scenes first.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "open_scene_additive",
                "scene",
                "Open a scene additively.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Scene asset path.", true) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "set_active_scene",
                "scene",
                "Set the active scene among loaded scenes.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Loaded scene path.", true) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "close_scene",
                "scene",
                "Close a loaded scene.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Loaded scene path.", true) },
                    { "save_if_dirty", new EditorRpcParameterDefinition("boolean", "Save the scene before closing if dirty.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "list_loaded_scenes",
                "scene",
                "List all loaded scenes and active state.",
                new Dictionary<string, EditorRpcParameterDefinition>()));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "save_open_scenes",
                "scene",
                "Save all open scenes and project assets.",
                new Dictionary<string, EditorRpcParameterDefinition>()));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "list_hierarchy",
                "scene",
                "List a flattened hierarchy snapshot for a loaded scene.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "scene_path", new EditorRpcParameterDefinition("string", "Optional loaded scene path. Defaults to the active scene.", false) },
                    { "max_depth", new EditorRpcParameterDefinition("integer", "Maximum depth to include. Default 3.", false) },
                    { "limit", new EditorRpcParameterDefinition("integer", "Maximum returned nodes. Default 200.", false) },
                    { "include_components", new EditorRpcParameterDefinition("boolean", "Include component type names for each node.", false) },
                    { "include_inactive", new EditorRpcParameterDefinition("boolean", "Include inactive objects. Default true.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "find_game_objects",
                "scene",
                "Find scene objects by partial name match.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "name_contains", new EditorRpcParameterDefinition("string", "Substring to search for in object names.", true) },
                    { "scene_path", new EditorRpcParameterDefinition("string", "Optional loaded scene path.", false) },
                    { "include_inactive", new EditorRpcParameterDefinition("boolean", "Include inactive objects. Default true.", false) },
                    { "limit", new EditorRpcParameterDefinition("integer", "Maximum returned matches. Default 20.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "select_game_object",
                "scene",
                "Select and ping a scene object by hierarchy path.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path.", true) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "inspect_scene_object",
                "scene",
                "Inspect a scene object or one of its components and optionally list serialized properties.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path.", true) },
                    { "component_type", new EditorRpcParameterDefinition("string", "Optional component type on the target object.", false) },
                    { "component_index", new EditorRpcParameterDefinition("integer", "Optional zero-based index when multiple components match.", false) },
                    { "include_properties", new EditorRpcParameterDefinition("boolean", "Include serialized properties and previews.", false) },
                    { "property_limit", new EditorRpcParameterDefinition("integer", "Maximum returned properties. Default 100.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "create_game_object",
                "scene",
                "Create a GameObject in the active scene or under a parent object.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "name", new EditorRpcParameterDefinition("string", "Name of the new object.", true) },
                    { "parent_path", new EditorRpcParameterDefinition("string", "Optional parent hierarchy path.", false) },
                    { "scene_path", new EditorRpcParameterDefinition("string", "Optional target loaded scene path.", false) },
                    { "position", new EditorRpcParameterDefinition("string", "Optional world position as x,y,z.", false) },
                    { "local_position", new EditorRpcParameterDefinition("string", "Optional local position as x,y,z.", false) },
                    { "local_rotation_euler", new EditorRpcParameterDefinition("string", "Optional local rotation as x,y,z degrees.", false) },
                    { "local_scale", new EditorRpcParameterDefinition("string", "Optional local scale as x,y,z.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after creation.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "instantiate_prefab",
                "scene",
                "Instantiate a prefab into a loaded scene.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "asset_path", new EditorRpcParameterDefinition("string", "Prefab asset path.", true) },
                    { "scene_path", new EditorRpcParameterDefinition("string", "Optional loaded scene path.", false) },
                    { "parent_path", new EditorRpcParameterDefinition("string", "Optional parent hierarchy path.", false) },
                    { "name", new EditorRpcParameterDefinition("string", "Optional instance name override.", false) },
                    { "position", new EditorRpcParameterDefinition("string", "Optional world position as x,y,z.", false) },
                    { "local_position", new EditorRpcParameterDefinition("string", "Optional local position as x,y,z.", false) },
                    { "local_rotation_euler", new EditorRpcParameterDefinition("string", "Optional local rotation as x,y,z degrees.", false) },
                    { "local_scale", new EditorRpcParameterDefinition("string", "Optional local scale as x,y,z.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after instantiation.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "set_transform",
                "scene",
                "Update transform values on a scene object by hierarchy path.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the target object.", true) },
                    { "position", new EditorRpcParameterDefinition("string", "Optional world position as x,y,z.", false) },
                    { "local_position", new EditorRpcParameterDefinition("string", "Optional local position as x,y,z.", false) },
                    { "rotation_euler", new EditorRpcParameterDefinition("string", "Optional world rotation as x,y,z degrees.", false) },
                    { "local_rotation_euler", new EditorRpcParameterDefinition("string", "Optional local rotation as x,y,z degrees.", false) },
                    { "local_scale", new EditorRpcParameterDefinition("string", "Optional local scale as x,y,z.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after the change.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "add_component",
                "scene",
                "Add a component to a scene object.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the target object.", true) },
                    { "component_type", new EditorRpcParameterDefinition("string", "Component type name or full type name.", true) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after the change.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "set_scene_object_property",
                "scene",
                "Set a serialized property on a scene object or one of its components.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the target object.", true) },
                    { "property_path", new EditorRpcParameterDefinition("string", "SerializedProperty path.", true) },
                    { "value", new EditorRpcParameterDefinition("string", "New value.", true) },
                    { "component_type", new EditorRpcParameterDefinition("string", "Optional component type on the target object.", false) },
                    { "component_index", new EditorRpcParameterDefinition("integer", "Optional zero-based index when multiple components match.", false) },
                    { "value_type", new EditorRpcParameterDefinition("string", "Value type: bool, int, float, string, enum, vector3, color, object_reference.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after the change.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "invoke_scene_object_method",
                "scene",
                "Invoke a method on a scene object or one of its components without adding a temporary bridge method.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the target object.", true) },
                    { "method_name", new EditorRpcParameterDefinition("string", "Method name to invoke.", true) },
                    { "arguments", new EditorRpcParameterDefinition("array<object>", "Optional JSON array like [{\"type\":\"bool\",\"value\":\"true\"}]. Object references use asset: or scene: prefixes.", false) },
                    { "component_type", new EditorRpcParameterDefinition("string", "Optional component type on the target object.", false) },
                    { "component_index", new EditorRpcParameterDefinition("integer", "Optional zero-based index when multiple components match.", false) },
                    { "include_non_public", new EditorRpcParameterDefinition("boolean", "When true, allow non-public methods. Default true.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after the invocation.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "delete_game_object",
                "scene",
                "Delete a scene object by hierarchy path.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the target object.", true) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after deletion.", false) }
                }));
        }

        private static void RegisterMaterialMethods()
        {
            CachedMethods.Add(new EditorRpcMethodDefinition(
                "batch_assign_materials",
                "materials",
                "Batch-assign a material across prefab assets or scene object hierarchies using simple renderer and slot filters.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "target_kind", new EditorRpcParameterDefinition("string", "Target kind: prefab_assets or scene_objects.", true, new List<string> { "prefab_assets", "scene_objects" }) },
                    { "targets", new EditorRpcParameterDefinition("array|string", "Target paths. For prefab_assets: prefab paths or folders. For scene_objects: hierarchy paths.", true) },
                    { "material_path", new EditorRpcParameterDefinition("string", "Material asset path. Use null to clear matched slots.", true) },
                    { "renderer_type", new EditorRpcParameterDefinition("string", "Optional renderer type filter, for example MeshRenderer or SkinnedMeshRenderer.", false) },
                    { "renderer_name_contains", new EditorRpcParameterDefinition("string", "Optional substring filter for renderer GameObject names.", false) },
                    { "hierarchy_path_contains", new EditorRpcParameterDefinition("string", "Optional substring filter for hierarchy paths.", false) },
                    { "current_material_name_contains", new EditorRpcParameterDefinition("string", "Optional substring filter for current material names.", false) },
                    { "current_material_path", new EditorRpcParameterDefinition("string", "Optional exact asset path filter for the current material.", false) },
                    { "slot_mode", new EditorRpcParameterDefinition("string", "Slot mode: all or specific.", false, new List<string> { "all", "specific" }) },
                    { "slot_index", new EditorRpcParameterDefinition("integer", "Zero-based slot index when slot_mode is specific.", false) },
                    { "include_inactive", new EditorRpcParameterDefinition("boolean", "Include inactive children when scanning targets. Default true.", false) },
                    { "dry_run", new EditorRpcParameterDefinition("boolean", "When true, report intended changes without writing assets or scene state.", false) },
                    { "save_open_scenes", new EditorRpcParameterDefinition("boolean", "When target_kind is scene_objects and dry_run is false, save open scenes after assignment.", false) }
                }));
        }

        private static void RegisterConsoleMethods()
        {
            CachedMethods.Add(new EditorRpcMethodDefinition(
                "read_console",
                "console",
                "Read recent Unity console entries.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "count", new EditorRpcParameterDefinition("integer", "Maximum number of recent console entries. Default 20.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "clear_console",
                "console",
                "Clear the Unity console.",
                new Dictionary<string, EditorRpcParameterDefinition>()));
        }

        private static void RegisterAnimatorMethods()
        {
            CachedMethods.Add(new EditorRpcMethodDefinition(
                "inspect_animator_controller",
                "animator",
                "Inspect an AnimatorController asset, including layers, parameters, state machines, transitions, conditions, and BlendTree details.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "asset_path", new EditorRpcParameterDefinition("string", "AnimatorController asset path.", true) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "add_animator_parameter",
                "animator",
                "Add a parameter to an AnimatorController.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "asset_path", new EditorRpcParameterDefinition("string", "AnimatorController asset path.", true) },
                    { "name", new EditorRpcParameterDefinition("string", "Parameter name.", true) },
                    { "type", new EditorRpcParameterDefinition("string", "Parameter type.", true, new List<string> { "Float", "Int", "Bool", "Trigger" }) },
                    { "default_value", new EditorRpcParameterDefinition("string", "Optional default value for non-trigger parameters.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "add_animator_state",
                "animator",
                "Add a state to an AnimatorController state machine.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "asset_path", new EditorRpcParameterDefinition("string", "AnimatorController asset path.", true) },
                    { "state_name", new EditorRpcParameterDefinition("string", "State name.", true) },
                    { "layer_name", new EditorRpcParameterDefinition("string", "Optional layer name. Defaults to the first layer.", false) },
                    { "state_machine_path", new EditorRpcParameterDefinition("string", "Optional state machine path under the layer root.", false) },
                    { "position", new EditorRpcParameterDefinition("string", "Optional graph position as x,y.", false) },
                    { "motion_asset_path", new EditorRpcParameterDefinition("string", "Optional AnimationClip or Motion asset path.", false) },
                    { "motion_name", new EditorRpcParameterDefinition("string", "Optional Motion sub-asset name when the asset path contains multiple motions.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "set_animator_default_state",
                "animator",
                "Set the default state on an AnimatorController state machine.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "asset_path", new EditorRpcParameterDefinition("string", "AnimatorController asset path.", true) },
                    { "state_name", new EditorRpcParameterDefinition("string", "State name.", true) },
                    { "layer_name", new EditorRpcParameterDefinition("string", "Optional layer name. Defaults to the first layer.", false) },
                    { "state_machine_path", new EditorRpcParameterDefinition("string", "Optional state machine path under the layer root.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "set_animator_state_motion",
                "animator",
                "Assign a Motion asset to an Animator state.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "asset_path", new EditorRpcParameterDefinition("string", "AnimatorController asset path.", true) },
                    { "state_name", new EditorRpcParameterDefinition("string", "State name.", true) },
                    { "motion_asset_path", new EditorRpcParameterDefinition("string", "AnimationClip or Motion asset path.", true) },
                    { "motion_name", new EditorRpcParameterDefinition("string", "Optional Motion sub-asset name when the asset path contains multiple motions.", false) },
                    { "layer_name", new EditorRpcParameterDefinition("string", "Optional layer name. Defaults to the first layer.", false) },
                    { "state_machine_path", new EditorRpcParameterDefinition("string", "Optional state machine path under the layer root.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "add_animator_transition",
                "animator",
                "Add a transition between two Animator states.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "asset_path", new EditorRpcParameterDefinition("string", "AnimatorController asset path.", true) },
                    { "from_state", new EditorRpcParameterDefinition("string", "Source state name. Required unless from_any_state is true.", false) },
                    { "to_state", new EditorRpcParameterDefinition("string", "Destination state name.", true) },
                    { "from_any_state", new EditorRpcParameterDefinition("boolean", "When true, create the transition from Any State.", false) },
                    { "layer_name", new EditorRpcParameterDefinition("string", "Optional layer name. Defaults to the first layer.", false) },
                    { "state_machine_path", new EditorRpcParameterDefinition("string", "Optional state machine path under the layer root.", false) },
                    { "has_exit_time", new EditorRpcParameterDefinition("boolean", "Whether the transition uses exit time.", false) },
                    { "exit_time", new EditorRpcParameterDefinition("float", "Exit time value.", false) },
                    { "duration", new EditorRpcParameterDefinition("float", "Transition duration.", false) },
                    { "offset", new EditorRpcParameterDefinition("float", "Optional transition offset.", false) },
                    { "fixed_duration", new EditorRpcParameterDefinition("boolean", "Optional fixed duration flag.", false) },
                    { "mute", new EditorRpcParameterDefinition("boolean", "Optional mute flag.", false) },
                    { "solo", new EditorRpcParameterDefinition("boolean", "Optional solo flag.", false) },
                    { "can_transition_to_self", new EditorRpcParameterDefinition("boolean", "Optional Any State self-transition flag.", false) },
                    { "ordered_interruption", new EditorRpcParameterDefinition("boolean", "Optional ordered interruption flag.", false) },
                    { "interruption_source", new EditorRpcParameterDefinition("string", "Optional interruption source.", false, new List<string> { "None", "Source", "Destination", "SourceThenDestination", "DestinationThenSource" }) },
                    { "condition_mode", new EditorRpcParameterDefinition("string", "Optional condition mode.", false, new List<string> { "If", "IfNot", "Greater", "Less", "Equals", "NotEqual" }) },
                    { "condition_parameter", new EditorRpcParameterDefinition("string", "Optional condition parameter name.", false) },
                    { "condition_threshold", new EditorRpcParameterDefinition("float", "Optional condition threshold.", false) },
                    { "conditions", new EditorRpcParameterDefinition("string", "Optional multi-condition descriptor list formatted as Mode:Parameter[:Threshold];Mode:Parameter[:Threshold].", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "set_animator_transition_properties",
                "animator",
                "Update an existing Animator transition, including timing, interruption, and conditions.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "asset_path", new EditorRpcParameterDefinition("string", "AnimatorController asset path.", true) },
                    { "from_state", new EditorRpcParameterDefinition("string", "Source state name. Required unless from_any_state is true.", false) },
                    { "to_state", new EditorRpcParameterDefinition("string", "Destination state name.", true) },
                    { "from_any_state", new EditorRpcParameterDefinition("boolean", "When true, target an Any State transition.", false) },
                    { "transition_index", new EditorRpcParameterDefinition("integer", "Optional zero-based index when multiple transitions match the same destination.", false) },
                    { "layer_name", new EditorRpcParameterDefinition("string", "Optional layer name. Defaults to the first layer.", false) },
                    { "state_machine_path", new EditorRpcParameterDefinition("string", "Optional state machine path under the layer root.", false) },
                    { "has_exit_time", new EditorRpcParameterDefinition("boolean", "Optional exit time flag.", false) },
                    { "exit_time", new EditorRpcParameterDefinition("float", "Optional exit time value.", false) },
                    { "duration", new EditorRpcParameterDefinition("float", "Optional transition duration.", false) },
                    { "offset", new EditorRpcParameterDefinition("float", "Optional transition offset.", false) },
                    { "fixed_duration", new EditorRpcParameterDefinition("boolean", "Optional fixed duration flag.", false) },
                    { "mute", new EditorRpcParameterDefinition("boolean", "Optional mute flag.", false) },
                    { "solo", new EditorRpcParameterDefinition("boolean", "Optional solo flag.", false) },
                    { "can_transition_to_self", new EditorRpcParameterDefinition("boolean", "Optional Any State self-transition flag.", false) },
                    { "ordered_interruption", new EditorRpcParameterDefinition("boolean", "Optional ordered interruption flag.", false) },
                    { "interruption_source", new EditorRpcParameterDefinition("string", "Optional interruption source.", false, new List<string> { "None", "Source", "Destination", "SourceThenDestination", "DestinationThenSource" }) },
                    { "clear_conditions", new EditorRpcParameterDefinition("boolean", "When true, remove all existing conditions.", false) },
                    { "replace_conditions", new EditorRpcParameterDefinition("boolean", "When true, clear existing conditions before adding new ones.", false) },
                    { "conditions", new EditorRpcParameterDefinition("string", "Optional multi-condition descriptor list formatted as Mode:Parameter[:Threshold];Mode:Parameter[:Threshold].", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "remove_animator_transition",
                "animator",
                "Remove an Animator transition.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "asset_path", new EditorRpcParameterDefinition("string", "AnimatorController asset path.", true) },
                    { "from_state", new EditorRpcParameterDefinition("string", "Source state name. Required unless from_any_state is true.", false) },
                    { "to_state", new EditorRpcParameterDefinition("string", "Destination state name.", true) },
                    { "from_any_state", new EditorRpcParameterDefinition("boolean", "When true, target an Any State transition.", false) },
                    { "transition_index", new EditorRpcParameterDefinition("integer", "Optional zero-based index when multiple transitions match the same destination.", false) },
                    { "layer_name", new EditorRpcParameterDefinition("string", "Optional layer name. Defaults to the first layer.", false) },
                    { "state_machine_path", new EditorRpcParameterDefinition("string", "Optional state machine path under the layer root.", false) }
                }));
        }

        public static List<EditorRpcMethodDefinition> GetMethods()
        {
            return CachedMethods;
        }
    }

    public static partial class EditorRpcMethodExecutor
    {
        private static readonly Dictionary<string, Func<string, string, EditorRpcMethodResult>> Executors =
            new Dictionary<string, Func<string, string, EditorRpcMethodResult>>(StringComparer.OrdinalIgnoreCase);

        private static bool _isInitialized;

        static partial void RegisterAdvancedExecutors();
        static partial void RegisterUiExecutors();
        static partial void RegisterTerrainExecutors();

        public static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            Register("list_methods", ExecuteListMethods);
            Register("get_editor_state", ExecuteGetEditorState);
            Register("enter_play_mode", ExecuteEnterPlayMode);
            Register("exit_play_mode", ExecuteExitPlayMode);
            Register("set_play_mode_pause", ExecuteSetPlayModePause);
            Register("execute_menu_item", ExecuteMenuItem);
            Register("execute_editor_static_method", ExecuteEditorStaticMethod);
            Register("invoke_editor_static_method", ExecuteInvokeEditorStaticMethod);

            Register("find_assets", ExecuteFindAssets);
            Register("select_asset", ExecuteSelectAsset);
            Register("list_asset_sub_objects", ExecuteListAssetSubObjects);
            Register("refresh_assets", ExecuteRefreshAssets);
            Register("reimport_asset", ExecuteReimportAsset);
            Register("inspect_asset_object", ExecuteInspectAssetObject);
            Register("set_asset_object_property", ExecuteSetAssetObjectProperty);
            Register("remove_asset_object_component", ExecuteRemoveAssetObjectComponent);
            Register("ensure_prefab_child", ExecuteEnsurePrefabChild);
            Register("invoke_asset_object_method", ExecuteInvokeAssetObjectMethod);
            Register("create_empty_prefab", ExecuteCreateEmptyPrefab);
            Register("save_scene_object_as_prefab", ExecuteSaveSceneObjectAsPrefab);
            Register("batch_assign_materials", ExecuteBatchAssignMaterials);

            Register("open_scene", ExecuteOpenScene);
            Register("open_scene_additive", ExecuteOpenSceneAdditive);
            Register("set_active_scene", ExecuteSetActiveScene);
            Register("close_scene", ExecuteCloseScene);
            Register("list_loaded_scenes", ExecuteListLoadedScenes);
            Register("save_open_scenes", ExecuteSaveOpenScenes);
            Register("list_hierarchy", ExecuteListHierarchy);
            Register("find_game_objects", ExecuteFindGameObjects);
            Register("select_game_object", ExecuteSelectGameObject);
            Register("inspect_scene_object", ExecuteInspectSceneObject);
            Register("create_game_object", ExecuteCreateGameObject);
            Register("instantiate_prefab", ExecuteInstantiatePrefab);
            Register("set_transform", ExecuteSetTransform);
            Register("add_component", ExecuteAddComponent);
            Register("set_scene_object_property", ExecuteSetSceneObjectProperty);
            Register("invoke_scene_object_method", ExecuteInvokeSceneObjectMethod);
            Register("delete_game_object", ExecuteDeleteGameObject);
            RegisterTerrainExecutors();
            RegisterAdvancedExecutors();
            RegisterUiExecutors();

            Register("read_console", ExecuteReadConsole);
            Register("clear_console", ExecuteClearConsole);

            Register("inspect_animator_controller", ExecuteInspectAnimatorController);
            Register("add_animator_parameter", ExecuteAddAnimatorParameter);
            Register("add_animator_state", ExecuteAddAnimatorState);
            Register("set_animator_default_state", ExecuteSetAnimatorDefaultState);
            Register("set_animator_state_motion", ExecuteSetAnimatorStateMotion);
            Register("add_animator_transition", ExecuteAddAnimatorTransition);
            Register("set_animator_transition_properties", ExecuteSetAnimatorTransitionProperties);
            Register("remove_animator_transition", ExecuteRemoveAnimatorTransition);
        }

        public static EditorRpcMethodResult Execute(string methodName, string argumentsJson)
        {
            Initialize();

            Func<string, string, EditorRpcMethodResult> executor;
            if (!Executors.TryGetValue(methodName, out executor))
            {
                return Failure("Unknown RPC method: " + methodName);
            }

            try
            {
                return executor(methodName, string.IsNullOrEmpty(argumentsJson) ? "{}" : argumentsJson);
            }
            catch (Exception e)
            {
                return Failure("RPC method execution failed: " + e.Message);
            }
        }

        private static void Register(string methodName, Func<string, string, EditorRpcMethodResult> executor)
        {
            if (!Executors.ContainsKey(methodName))
            {
                Executors.Add(methodName, executor);
            }
        }
    }
}
