using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EditorRpc
{
    public static partial class EditorRpcMethods
    {
        static partial void RegisterUiMethods()
        {
            CachedMethods.Add(new EditorRpcMethodDefinition(
                "list_ui_roots",
                "ui",
                "List canvas roots, UI controller nodes, and EventSystem entries in loaded scenes.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "scene_path", new EditorRpcParameterDefinition("string", "Optional loaded scene path. Defaults to all loaded scenes.", false) },
                    { "include_controllers", new EditorRpcParameterDefinition("boolean", "Include objects with UI controller components. Default true.", false) },
                    { "limit", new EditorRpcParameterDefinition("integer", "Maximum returned roots. Default 100.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "inspect_ui_controller",
                "ui",
                "Inspect a UI controller component and return its serialized Unity object bindings.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the controller GameObject.", true) },
                    { "component_type", new EditorRpcParameterDefinition("string", "Optional controller component type. Defaults to the first UI-like MonoBehaviour on the object.", false) },
                    { "component_index", new EditorRpcParameterDefinition("integer", "Optional zero-based component index when multiple controller components match.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "map_ui_controller_bindings",
                "ui",
                "Return the serialized Unity object bindings for a UI controller component.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the controller GameObject.", true) },
                    { "component_type", new EditorRpcParameterDefinition("string", "Optional controller component type. Defaults to the first UI-like MonoBehaviour on the object.", false) },
                    { "component_index", new EditorRpcParameterDefinition("integer", "Optional zero-based component index when multiple controller components match.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "list_ui_subtree",
                "ui",
                "List a UI-focused subtree snapshot rooted at a Canvas or UI node.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the subtree root.", true) },
                    { "max_depth", new EditorRpcParameterDefinition("integer", "Maximum depth to include. Default 3.", false) },
                    { "limit", new EditorRpcParameterDefinition("integer", "Maximum returned nodes. Default 200.", false) },
                    { "include_inactive", new EditorRpcParameterDefinition("boolean", "Include inactive objects. Default true.", false) },
                    { "ui_only", new EditorRpcParameterDefinition("boolean", "When true, only include UI-related objects. Default true.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "inspect_unity_event_bindings",
                "ui",
                "Inspect persistent UnityEvent bindings on a UI object or one of its components.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the target object.", true) },
                    { "component_type", new EditorRpcParameterDefinition("string", "Optional component type. Defaults to all components on the object.", false) },
                    { "component_index", new EditorRpcParameterDefinition("integer", "Optional zero-based component index when multiple components match.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "find_ui_prefab_references",
                "ui",
                "Find prefab asset references serialized on a UI controller component.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the controller GameObject.", true) },
                    { "component_type", new EditorRpcParameterDefinition("string", "Optional controller component type. Defaults to the first UI-like MonoBehaviour on the object.", false) },
                    { "component_index", new EditorRpcParameterDefinition("integer", "Optional zero-based component index when multiple controller components match.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "ensure_canvas_root",
                "ui",
                "Create or complete a Canvas root with CanvasScaler, GraphicRaycaster, and optional EventSystem.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "name", new EditorRpcParameterDefinition("string", "Canvas root name.", true) },
                    { "scene_path", new EditorRpcParameterDefinition("string", "Optional loaded scene path. Defaults to the active scene.", false) },
                    { "render_mode", new EditorRpcParameterDefinition("string", "Canvas render mode: overlay, screen_space_camera, or world_space. Default overlay.", false, new List<string> { "overlay", "screen_space_camera", "world_space" }) },
                    { "sorting_order", new EditorRpcParameterDefinition("integer", "Optional Canvas sorting order.", false) },
                    { "target_display", new EditorRpcParameterDefinition("integer", "Optional Canvas target display.", false) },
                    { "add_scaler", new EditorRpcParameterDefinition("boolean", "Ensure a CanvasScaler component. Default true.", false) },
                    { "add_graphic_raycaster", new EditorRpcParameterDefinition("boolean", "Ensure a GraphicRaycaster component. Default true.", false) },
                    { "ensure_event_system", new EditorRpcParameterDefinition("boolean", "Ensure a scene EventSystem. Default true.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after creation.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "create_ui_container",
                "ui",
                "Create a RectTransform-based UI container.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "name", new EditorRpcParameterDefinition("string", "New object name.", true) },
                    { "parent_path", new EditorRpcParameterDefinition("string", "Optional parent hierarchy path.", false) },
                    { "scene_path", new EditorRpcParameterDefinition("string", "Optional loaded scene path when parent_path is omitted.", false) },
                    { "anchor_preset", new EditorRpcParameterDefinition("string", "Optional anchor preset.", false) },
                    { "anchor_min", new EditorRpcParameterDefinition("string", "Optional anchorMin as x,y.", false) },
                    { "anchor_max", new EditorRpcParameterDefinition("string", "Optional anchorMax as x,y.", false) },
                    { "anchored_position", new EditorRpcParameterDefinition("string", "Optional anchoredPosition as x,y.", false) },
                    { "size_delta", new EditorRpcParameterDefinition("string", "Optional sizeDelta as x,y.", false) },
                    { "pivot", new EditorRpcParameterDefinition("string", "Optional pivot as x,y.", false) },
                    { "offset_min", new EditorRpcParameterDefinition("string", "Optional offsetMin as x,y.", false) },
                    { "offset_max", new EditorRpcParameterDefinition("string", "Optional offsetMax as x,y.", false) },
                    { "local_scale", new EditorRpcParameterDefinition("string", "Optional local scale as x,y,z.", false) },
                    { "local_rotation_euler", new EditorRpcParameterDefinition("string", "Optional local rotation as x,y,z degrees.", false) },
                    { "add_canvas_group", new EditorRpcParameterDefinition("boolean", "Add a CanvasGroup component. Default false.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after creation.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "create_ui_image",
                "ui",
                "Create an Image UI node.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "name", new EditorRpcParameterDefinition("string", "New object name.", true) },
                    { "parent_path", new EditorRpcParameterDefinition("string", "Optional parent hierarchy path.", false) },
                    { "scene_path", new EditorRpcParameterDefinition("string", "Optional loaded scene path when parent_path is omitted.", false) },
                    { "color", new EditorRpcParameterDefinition("string", "Optional color as r,g,b,a.", false) },
                    { "sprite_asset_path", new EditorRpcParameterDefinition("string", "Optional sprite asset path.", false) },
                    { "raycast_target", new EditorRpcParameterDefinition("boolean", "Optional raycast target flag.", false) },
                    { "preserve_aspect", new EditorRpcParameterDefinition("boolean", "Optional preserveAspect flag on Image.", false) },
                    { "image_type", new EditorRpcParameterDefinition("string", "Optional Image.Type enum value.", false) },
                    { "fill_amount", new EditorRpcParameterDefinition("float", "Optional fill amount for filled images.", false) },
                    { "anchor_preset", new EditorRpcParameterDefinition("string", "Optional anchor preset.", false) },
                    { "anchor_min", new EditorRpcParameterDefinition("string", "Optional anchorMin as x,y.", false) },
                    { "anchor_max", new EditorRpcParameterDefinition("string", "Optional anchorMax as x,y.", false) },
                    { "anchored_position", new EditorRpcParameterDefinition("string", "Optional anchoredPosition as x,y.", false) },
                    { "size_delta", new EditorRpcParameterDefinition("string", "Optional sizeDelta as x,y.", false) },
                    { "pivot", new EditorRpcParameterDefinition("string", "Optional pivot as x,y.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after creation.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "create_ui_text",
                "ui",
                "Create a legacy UGUI Text node.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "name", new EditorRpcParameterDefinition("string", "New object name.", true) },
                    { "parent_path", new EditorRpcParameterDefinition("string", "Optional parent hierarchy path.", false) },
                    { "scene_path", new EditorRpcParameterDefinition("string", "Optional loaded scene path when parent_path is omitted.", false) },
                    { "text", new EditorRpcParameterDefinition("string", "Text content.", false) },
                    { "font_size", new EditorRpcParameterDefinition("integer", "Optional font size. Default 24.", false) },
                    { "color", new EditorRpcParameterDefinition("string", "Optional color as r,g,b,a.", false) },
                    { "alignment", new EditorRpcParameterDefinition("string", "Optional TextAnchor enum value.", false) },
                    { "raycast_target", new EditorRpcParameterDefinition("boolean", "Optional raycast target flag. Default false.", false) },
                    { "anchor_preset", new EditorRpcParameterDefinition("string", "Optional anchor preset.", false) },
                    { "anchor_min", new EditorRpcParameterDefinition("string", "Optional anchorMin as x,y.", false) },
                    { "anchor_max", new EditorRpcParameterDefinition("string", "Optional anchorMax as x,y.", false) },
                    { "anchored_position", new EditorRpcParameterDefinition("string", "Optional anchoredPosition as x,y.", false) },
                    { "size_delta", new EditorRpcParameterDefinition("string", "Optional sizeDelta as x,y.", false) },
                    { "pivot", new EditorRpcParameterDefinition("string", "Optional pivot as x,y.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after creation.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "create_ui_button",
                "ui",
                "Create a Button with Image and child Text.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "name", new EditorRpcParameterDefinition("string", "New object name.", true) },
                    { "parent_path", new EditorRpcParameterDefinition("string", "Optional parent hierarchy path.", false) },
                    { "scene_path", new EditorRpcParameterDefinition("string", "Optional loaded scene path when parent_path is omitted.", false) },
                    { "text", new EditorRpcParameterDefinition("string", "Button label text.", false) },
                    { "font_size", new EditorRpcParameterDefinition("integer", "Optional label font size. Default 24.", false) },
                    { "color", new EditorRpcParameterDefinition("string", "Optional button image color as r,g,b,a.", false) },
                    { "text_color", new EditorRpcParameterDefinition("string", "Optional label color as r,g,b,a.", false) },
                    { "raycast_target", new EditorRpcParameterDefinition("boolean", "Optional raycast target flag on the button image. Default true.", false) },
                    { "anchor_preset", new EditorRpcParameterDefinition("string", "Optional anchor preset.", false) },
                    { "anchor_min", new EditorRpcParameterDefinition("string", "Optional anchorMin as x,y.", false) },
                    { "anchor_max", new EditorRpcParameterDefinition("string", "Optional anchorMax as x,y.", false) },
                    { "anchored_position", new EditorRpcParameterDefinition("string", "Optional anchoredPosition as x,y.", false) },
                    { "size_delta", new EditorRpcParameterDefinition("string", "Optional sizeDelta as x,y.", false) },
                    { "pivot", new EditorRpcParameterDefinition("string", "Optional pivot as x,y.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after creation.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "create_ui_scroll_view",
                "ui",
                "Create a ScrollView using Unity default controls and return the key child paths.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "name", new EditorRpcParameterDefinition("string", "New object name.", true) },
                    { "parent_path", new EditorRpcParameterDefinition("string", "Optional parent hierarchy path.", false) },
                    { "scene_path", new EditorRpcParameterDefinition("string", "Optional loaded scene path when parent_path is omitted.", false) },
                    { "anchor_preset", new EditorRpcParameterDefinition("string", "Optional anchor preset.", false) },
                    { "anchor_min", new EditorRpcParameterDefinition("string", "Optional anchorMin as x,y.", false) },
                    { "anchor_max", new EditorRpcParameterDefinition("string", "Optional anchorMax as x,y.", false) },
                    { "anchored_position", new EditorRpcParameterDefinition("string", "Optional anchoredPosition as x,y.", false) },
                    { "size_delta", new EditorRpcParameterDefinition("string", "Optional sizeDelta as x,y.", false) },
                    { "pivot", new EditorRpcParameterDefinition("string", "Optional pivot as x,y.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after creation.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "set_rect_transform",
                "ui",
                "Update RectTransform values on a UI node.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the target UI object.", true) },
                    { "anchor_preset", new EditorRpcParameterDefinition("string", "Optional anchor preset.", false) },
                    { "anchor_min", new EditorRpcParameterDefinition("string", "Optional anchorMin as x,y.", false) },
                    { "anchor_max", new EditorRpcParameterDefinition("string", "Optional anchorMax as x,y.", false) },
                    { "anchored_position", new EditorRpcParameterDefinition("string", "Optional anchoredPosition as x,y.", false) },
                    { "size_delta", new EditorRpcParameterDefinition("string", "Optional sizeDelta as x,y.", false) },
                    { "pivot", new EditorRpcParameterDefinition("string", "Optional pivot as x,y.", false) },
                    { "offset_min", new EditorRpcParameterDefinition("string", "Optional offsetMin as x,y.", false) },
                    { "offset_max", new EditorRpcParameterDefinition("string", "Optional offsetMax as x,y.", false) },
                    { "local_scale", new EditorRpcParameterDefinition("string", "Optional local scale as x,y,z.", false) },
                    { "local_rotation_euler", new EditorRpcParameterDefinition("string", "Optional local rotation as x,y,z degrees.", false) },
                    { "sibling_index", new EditorRpcParameterDefinition("integer", "Optional sibling index.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after the change.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "set_anchor_preset",
                "ui",
                "Apply a named RectTransform anchor preset.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the target UI object.", true) },
                    { "preset", new EditorRpcParameterDefinition("string", "Anchor preset name.", true) },
                    { "preserve_offsets", new EditorRpcParameterDefinition("boolean", "When true, keep current anchoredPosition/offsets. Default false.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after the change.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "set_ui_text",
                "ui",
                "Update a legacy UGUI Text component on a UI node.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the target UI object.", true) },
                    { "component_type", new EditorRpcParameterDefinition("string", "Optional component type. Defaults to Text.", false) },
                    { "component_index", new EditorRpcParameterDefinition("integer", "Optional zero-based component index when multiple text-like components match.", false) },
                    { "text", new EditorRpcParameterDefinition("string", "Optional new text content.", false) },
                    { "font_size", new EditorRpcParameterDefinition("integer", "Optional font size.", false) },
                    { "color", new EditorRpcParameterDefinition("string", "Optional color as r,g,b,a.", false) },
                    { "alignment", new EditorRpcParameterDefinition("string", "Optional TextAnchor enum value.", false) },
                    { "raycast_target", new EditorRpcParameterDefinition("boolean", "Optional raycast target flag.", false) },
                    { "horizontal_overflow", new EditorRpcParameterDefinition("string", "Optional HorizontalWrapMode enum value.", false) },
                    { "vertical_overflow", new EditorRpcParameterDefinition("string", "Optional VerticalWrapMode enum value.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after the change.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "set_ui_graphic",
                "ui",
                "Update a Graphic or Image component on a UI node.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the target UI object.", true) },
                    { "component_type", new EditorRpcParameterDefinition("string", "Optional component type. Defaults to the first Graphic on the object.", false) },
                    { "component_index", new EditorRpcParameterDefinition("integer", "Optional zero-based component index when multiple graphics match.", false) },
                    { "color", new EditorRpcParameterDefinition("string", "Optional color as r,g,b,a.", false) },
                    { "sprite_asset_path", new EditorRpcParameterDefinition("string", "Optional sprite asset path for Image components.", false) },
                    { "raycast_target", new EditorRpcParameterDefinition("boolean", "Optional raycast target flag.", false) },
                    { "preserve_aspect", new EditorRpcParameterDefinition("boolean", "Optional preserveAspect flag on Image.", false) },
                    { "image_type", new EditorRpcParameterDefinition("string", "Optional Image.Type enum value.", false) },
                    { "fill_amount", new EditorRpcParameterDefinition("float", "Optional fill amount for filled images.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after the change.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "set_ui_interactable",
                "ui",
                "Update a Selectable or CanvasGroup component on a UI node.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the target UI object.", true) },
                    { "component_type", new EditorRpcParameterDefinition("string", "Optional component type. Defaults to Selectable, then CanvasGroup.", false) },
                    { "component_index", new EditorRpcParameterDefinition("integer", "Optional zero-based component index when multiple components match.", false) },
                    { "interactable", new EditorRpcParameterDefinition("boolean", "Optional interactable flag.", false) },
                    { "alpha", new EditorRpcParameterDefinition("float", "Optional CanvasGroup alpha.", false) },
                    { "blocks_raycasts", new EditorRpcParameterDefinition("boolean", "Optional CanvasGroup blocksRaycasts flag.", false) },
                    { "ignore_parent_groups", new EditorRpcParameterDefinition("boolean", "Optional CanvasGroup ignoreParentGroups flag.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after the change.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "reparent_ui_node",
                "ui",
                "Reparent a UI node under a different parent and optionally set sibling index.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the UI node to move.", true) },
                    { "parent_path", new EditorRpcParameterDefinition("string", "Optional new parent hierarchy path. Empty moves to the scene root.", false) },
                    { "world_position_stays", new EditorRpcParameterDefinition("boolean", "When true, preserve world position while reparenting. Default false.", false) },
                    { "sibling_index", new EditorRpcParameterDefinition("integer", "Optional sibling index after reparenting.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after the change.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "duplicate_ui_node",
                "ui",
                "Duplicate a UI node and optionally reparent or rename it.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the source UI node.", true) },
                    { "parent_path", new EditorRpcParameterDefinition("string", "Optional new parent hierarchy path for the copy.", false) },
                    { "name", new EditorRpcParameterDefinition("string", "Optional name override for the duplicate.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after the change.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "add_ui_layout_group",
                "ui",
                "Add or configure a VerticalLayoutGroup, HorizontalLayoutGroup, GridLayoutGroup, or ContentSizeFitter.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "path", new EditorRpcParameterDefinition("string", "Hierarchy path of the target UI object.", true) },
                    { "layout_type", new EditorRpcParameterDefinition("string", "Layout type: vertical, horizontal, grid, or fitter.", true, new List<string> { "vertical", "horizontal", "grid", "fitter" }) },
                    { "spacing", new EditorRpcParameterDefinition("string", "Optional spacing. Use a single float for vertical/horizontal or x,y for grid.", false) },
                    { "padding", new EditorRpcParameterDefinition("string", "Optional padding as left,top,right,bottom.", false) },
                    { "child_alignment", new EditorRpcParameterDefinition("string", "Optional TextAnchor enum value.", false) },
                    { "child_control_width", new EditorRpcParameterDefinition("boolean", "Optional childControlWidth flag on layout groups.", false) },
                    { "child_control_height", new EditorRpcParameterDefinition("boolean", "Optional childControlHeight flag on layout groups.", false) },
                    { "child_force_expand_width", new EditorRpcParameterDefinition("boolean", "Optional childForceExpandWidth flag on layout groups.", false) },
                    { "child_force_expand_height", new EditorRpcParameterDefinition("boolean", "Optional childForceExpandHeight flag on layout groups.", false) },
                    { "cell_size", new EditorRpcParameterDefinition("string", "Optional grid cell size as x,y.", false) },
                    { "constraint", new EditorRpcParameterDefinition("string", "Optional GridLayoutGroup.Constraint enum value.", false) },
                    { "constraint_count", new EditorRpcParameterDefinition("integer", "Optional grid constraint count.", false) },
                    { "fit_horizontal", new EditorRpcParameterDefinition("string", "Optional ContentSizeFitter.FitMode enum value.", false) },
                    { "fit_vertical", new EditorRpcParameterDefinition("string", "Optional ContentSizeFitter.FitMode enum value.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save the scene after the change.", false) }
                }));

            CachedMethods.Add(new EditorRpcMethodDefinition(
                "batch_ui_operations",
                "ui",
                "Run multiple UI create/update/reparent/layout operations in one editor RPC call.",
                new Dictionary<string, EditorRpcParameterDefinition>
                {
                    { "operations", new EditorRpcParameterDefinition("array<object>", "JSON array of UI operation objects. Each object must include method plus that method's normal parameters.", true) },
                    { "stop_on_error", new EditorRpcParameterDefinition("boolean", "Stop at the first failed operation. Default true.", false) },
                    { "save_scene", new EditorRpcParameterDefinition("boolean", "Save open scenes once after successful operations. Default false.", false) }
                }));
        }
    }

    public static partial class EditorRpcMethodExecutor
    {
        static partial void RegisterUiExecutors()
        {
            Register("list_ui_roots", ExecuteListUiRoots);
            Register("inspect_ui_controller", ExecuteInspectUiController);
            Register("map_ui_controller_bindings", ExecuteMapUiControllerBindings);
            Register("list_ui_subtree", ExecuteListUiSubtree);
            Register("inspect_unity_event_bindings", ExecuteInspectUnityEventBindings);
            Register("find_ui_prefab_references", ExecuteFindUiPrefabReferences);
            Register("ensure_canvas_root", ExecuteEnsureCanvasRoot);
            Register("create_ui_container", ExecuteCreateUiContainer);
            Register("create_ui_image", ExecuteCreateUiImage);
            Register("create_ui_text", ExecuteCreateUiText);
            Register("create_ui_button", ExecuteCreateUiButton);
            Register("create_ui_scroll_view", ExecuteCreateUiScrollView);
            Register("set_rect_transform", ExecuteSetRectTransform);
            Register("set_anchor_preset", ExecuteSetAnchorPreset);
            Register("set_ui_text", ExecuteSetUiText);
            Register("set_ui_graphic", ExecuteSetUiGraphic);
            Register("set_ui_interactable", ExecuteSetUiInteractable);
            Register("reparent_ui_node", ExecuteReparentUiNode);
            Register("duplicate_ui_node", ExecuteDuplicateUiNode);
            Register("add_ui_layout_group", ExecuteAddUiLayoutGroup);
            Register("batch_ui_operations", ExecuteBatchUiOperations);
        }

        private static EditorRpcMethodResult ExecuteListUiRoots(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var scenePath = GetString(args, "scene_path", string.Empty);
            var includeControllers = GetBool(args, "include_controllers", true);
            var limit = Mathf.Max(1, GetInt(args, "limit", 100));
            var scenes = GetSearchScenes(scenePath);
            if (scenes.Count == 0)
            {
                return Failure("No loaded scenes matched the requested scene_path.");
            }

            var map = new Dictionary<string, UiRootInfo>(StringComparer.Ordinal);
            for (int sceneIndex = 0; sceneIndex < scenes.Count; sceneIndex++)
            {
                var scene = scenes[sceneIndex];
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                var roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    var root = roots[rootIndex];
                    if (root == null)
                    {
                        continue;
                    }

                    var canvases = root.GetComponentsInChildren<Canvas>(true);
                    for (int canvasIndex = 0; canvasIndex < canvases.Length && map.Count < limit; canvasIndex++)
                    {
                        var canvas = canvases[canvasIndex];
                        if (canvas == null)
                        {
                            continue;
                        }

                        AddOrMergeUiRoot(map, canvas.gameObject, "canvas");
                    }

                    var eventSystems = root.GetComponentsInChildren<EventSystem>(true);
                    for (int eventIndex = 0; eventIndex < eventSystems.Length && map.Count < limit; eventIndex++)
                    {
                        var eventSystem = eventSystems[eventIndex];
                        if (eventSystem == null)
                        {
                            continue;
                        }

                        AddOrMergeUiRoot(map, eventSystem.gameObject, "event_system");
                    }

                    if (!includeControllers)
                    {
                        continue;
                    }

                    var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                    for (int behaviourIndex = 0; behaviourIndex < behaviours.Length && map.Count < limit; behaviourIndex++)
                    {
                        var behaviour = behaviours[behaviourIndex];
                        if (!IsUiControllerComponent(behaviour))
                        {
                            continue;
                        }

                        AddOrMergeUiRoot(map, behaviour.gameObject, "controller");
                    }
                }
            }

            var items = new List<UiRootInfo>(map.Values);
            items.Sort((left, right) => string.CompareOrdinal(left.path, right.path));
            if (items.Count > limit)
            {
                items.RemoveRange(limit, items.Count - limit);
            }

            return Success("UI roots listed.", new UiRootsPayload
            {
                scenePath = string.IsNullOrEmpty(scenePath) ? SceneManager.GetActiveScene().path : scenePath,
                returnedCount = items.Count,
                roots = items.ToArray()
            });
        }

        private static EditorRpcMethodResult ExecuteInspectUiController(string methodName, string argumentsJson)
        {
            var payload = BuildUiControllerPayload(argumentsJson, false, out var error);
            if (payload == null)
            {
                return Failure(error);
            }

            return Success("UI controller inspected.", payload);
        }

        private static EditorRpcMethodResult ExecuteMapUiControllerBindings(string methodName, string argumentsJson)
        {
            var payload = BuildUiControllerPayload(argumentsJson, false, out var error);
            if (payload == null)
            {
                return Failure(error);
            }

            return Success("UI controller bindings mapped.", payload);
        }

        private static EditorRpcMethodResult ExecuteListUiSubtree(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            var maxDepth = Mathf.Max(0, GetInt(args, "max_depth", 3));
            var limit = Mathf.Max(1, GetInt(args, "limit", 200));
            var includeInactive = GetBool(args, "include_inactive", true);
            var uiOnly = GetBool(args, "ui_only", true);
            if (string.IsNullOrEmpty(path))
            {
                return Failure("list_ui_subtree requires path.");
            }

            if (!TryFindGameObject(path, out var go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            var nodes = new List<UiTreeNodeInfo>();
            AppendUiTreeNode(go.transform, 0, maxDepth, includeInactive, uiOnly, nodes, limit);
            return Success("UI subtree listed.", new UiSubtreePayload
            {
                rootPath = path,
                returnedCount = nodes.Count,
                nodes = nodes.ToArray()
            });
        }

        private static EditorRpcMethodResult ExecuteInspectUnityEventBindings(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            var componentType = GetString(args, "component_type", string.Empty);
            var componentIndex = GetInt(args, "component_index", 0);
            if (string.IsNullOrEmpty(path))
            {
                return Failure("inspect_unity_event_bindings requires path.");
            }

            if (!TryFindGameObject(path, out var go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            var components = ResolveComponentsForEventInspection(go, componentType, componentIndex, out var error);
            if (components == null)
            {
                return Failure(error);
            }

            var events = new List<UiEventBindingInfo>();
            for (int i = 0; i < components.Count; i++)
            {
                CollectUnityEventBindings(components[i], events);
            }

            return Success("UnityEvent bindings inspected.", new UiEventBindingsPayload
            {
                path = path,
                returnedCount = events.Count,
                events = events.ToArray()
            });
        }

        private static EditorRpcMethodResult ExecuteFindUiPrefabReferences(string methodName, string argumentsJson)
        {
            var payload = BuildUiControllerPayload(argumentsJson, true, out var error);
            if (payload == null)
            {
                return Failure(error);
            }

            return Success("UI prefab references listed.", new UiPrefabReferencePayload
            {
                path = payload.path,
                componentType = payload.componentType,
                returnedCount = payload.bindingCount,
                references = payload.bindings
            });
        }

        private static EditorRpcMethodResult ExecuteEnsureCanvasRoot(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var name = GetRequiredString(args, "name");
            if (string.IsNullOrEmpty(name))
            {
                return Failure("ensure_canvas_root requires name.");
            }

            if (!TryResolveScene(GetString(args, "scene_path", string.Empty), out var scene))
            {
                return Failure("Target scene is not loaded.");
            }

            var root = FindRootGameObject(scene, name);
            if (root == null)
            {
                root = new GameObject(name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(root, "Editor RPC Ensure Canvas Root");
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            var rectTransform = root.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return Failure("Existing root with the requested name does not have a RectTransform. Rename it or choose a different Canvas root name.");
            }

            ApplyCanvasRootRect(rectTransform);

            var canvas = root.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = Undo.AddComponent<Canvas>(root);
            }

            var renderMode = GetString(args, "render_mode", "overlay");
            if (!TryApplyCanvasRenderMode(canvas, renderMode))
            {
                return Failure("Unsupported render_mode: " + renderMode);
            }

            if (TryGetOptionalInt(args, "sorting_order", out var sortingOrder))
            {
                canvas.sortingOrder = sortingOrder;
            }

            if (TryGetOptionalInt(args, "target_display", out var targetDisplay))
            {
                canvas.targetDisplay = targetDisplay;
            }

            if (GetBool(args, "add_scaler", true))
            {
                var scaler = root.GetComponent<CanvasScaler>();
                if (scaler == null)
                {
                    scaler = Undo.AddComponent<CanvasScaler>(root);
                }

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (GetBool(args, "add_graphic_raycaster", true) && root.GetComponent<GraphicRaycaster>() == null)
            {
                Undo.AddComponent<GraphicRaycaster>(root);
            }

            if (GetBool(args, "ensure_event_system", true))
            {
                EnsureSceneEventSystem(scene);
            }

            MarkDirty(root);
            SaveSceneIfRequested(args, root);
            return Success("Canvas root ensured.", BuildUiNodePayload(root));
        }

        private static EditorRpcMethodResult ExecuteCreateUiContainer(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            if (!TryCreateUiNode(args, out var go, out var error))
            {
                return Failure(error);
            }

            if (GetBool(args, "add_canvas_group", false) && go.GetComponent<CanvasGroup>() == null)
            {
                Undo.AddComponent<CanvasGroup>(go);
            }

            MarkDirty(go);
            SaveSceneIfRequested(args, go);
            return Success("UI container created.", BuildUiNodePayload(go));
        }

        private static EditorRpcMethodResult ExecuteCreateUiImage(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            if (!TryCreateUiNode(args, out var go, out var error))
            {
                return Failure(error);
            }

            EnsureCanvasRenderer(go);
            var image = go.GetComponent<Image>();
            if (image == null)
            {
                image = Undo.AddComponent<Image>(go);
            }

            ApplyGraphicArgs(args, image, out error);
            if (!string.IsNullOrEmpty(error))
            {
                return Failure(error);
            }

            MarkDirty(go);
            SaveSceneIfRequested(args, go);
            return Success("UI image created.", BuildUiNodePayload(go));
        }

        private static EditorRpcMethodResult ExecuteCreateUiText(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            if (!TryCreateUiNode(args, out var go, out var error))
            {
                return Failure(error);
            }

            EnsureCanvasRenderer(go);
            var text = go.GetComponent<Text>();
            if (text == null)
            {
                text = Undo.AddComponent<Text>(go);
            }

            text.font = GetDefaultFont();
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            ApplyTextArgs(args, text, out error);
            if (!string.IsNullOrEmpty(error))
            {
                return Failure(error);
            }

            MarkDirty(go);
            SaveSceneIfRequested(args, go);
            return Success("UI text created.", BuildUiNodePayload(go));
        }

        private static EditorRpcMethodResult ExecuteCreateUiButton(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            if (!TryCreateUiNode(args, out var go, out var error))
            {
                return Failure(error);
            }

            EnsureCanvasRenderer(go);
            var image = go.GetComponent<Image>();
            if (image == null)
            {
                image = Undo.AddComponent<Image>(go);
            }

            var button = go.GetComponent<Button>();
            if (button == null)
            {
                button = Undo.AddComponent<Button>(go);
            }

            image.color = new Color(1f, 1f, 1f, 1f);
            image.raycastTarget = GetBool(args, "raycast_target", true);
            button.targetGraphic = image;

            if (TryGetColorArg(args, "color", out var buttonColor))
            {
                image.color = buttonColor;
            }

            var label = FindDirectChild(go.transform, "Text");
            if (label == null)
            {
                label = new GameObject("Text", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(label, "Editor RPC Create Button Label");
                label.transform.SetParent(go.transform, false);
            }

            EnsureCanvasRenderer(label);
            var labelRect = label.GetComponent<RectTransform>();
            ApplyAnchorPreset(labelRect, "stretch_full", true);
            labelRect.offsetMin = new Vector2(8f, 6f);
            labelRect.offsetMax = new Vector2(-8f, -6f);

            var text = label.GetComponent<Text>();
            if (text == null)
            {
                text = Undo.AddComponent<Text>(label);
            }

            text.font = GetDefaultFont();
            text.alignment = TextAnchor.MiddleCenter;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.text = GetString(args, "text", go.name);
            text.fontSize = GetInt(args, "font_size", 24);
            text.color = Color.black;
            if (TryGetColorArg(args, "text_color", out var textColor))
            {
                text.color = textColor;
            }

            MarkDirty(go);
            SaveSceneIfRequested(args, go);
            return Success("UI button created.", BuildUiNodePayload(go));
        }

        private static EditorRpcMethodResult ExecuteCreateUiScrollView(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var name = GetRequiredString(args, "name");
            if (string.IsNullOrEmpty(name))
            {
                return Failure("create_ui_scroll_view requires name.");
            }

            var resources = new DefaultControls.Resources();
            var go = DefaultControls.CreateScrollView(resources);
            Undo.RegisterCreatedObjectUndo(go, "Editor RPC Create ScrollView");

            if (!TryParentOrMoveUiNode(go, args, out var error))
            {
                UnityEngine.Object.DestroyImmediate(go);
                return Failure(error);
            }

            go.name = name;
            ApplyRectTransformArgs(args, go.GetComponent<RectTransform>(), true, out error);
            if (!string.IsNullOrEmpty(error))
            {
                UnityEngine.Object.DestroyImmediate(go);
                return Failure(error);
            }

            MarkDirty(go);
            SaveSceneIfRequested(args, go);

            var scrollRect = go.GetComponent<ScrollRect>();
            var viewportPath = scrollRect != null && scrollRect.viewport != null ? GetGameObjectPath(scrollRect.viewport.gameObject) : string.Empty;
            var contentPath = scrollRect != null && scrollRect.content != null ? GetGameObjectPath(scrollRect.content.gameObject) : string.Empty;
            var horizontalScrollbarPath = scrollRect != null && scrollRect.horizontalScrollbar != null ? GetGameObjectPath(scrollRect.horizontalScrollbar.gameObject) : string.Empty;
            var verticalScrollbarPath = scrollRect != null && scrollRect.verticalScrollbar != null ? GetGameObjectPath(scrollRect.verticalScrollbar.gameObject) : string.Empty;

            return Success("UI ScrollView created.", new UiScrollViewPayload
            {
                path = GetGameObjectPath(go),
                viewportPath = viewportPath,
                contentPath = contentPath,
                horizontalScrollbarPath = horizontalScrollbarPath,
                verticalScrollbarPath = verticalScrollbarPath
            });
        }

        private static EditorRpcMethodResult ExecuteSetRectTransform(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            if (string.IsNullOrEmpty(path))
            {
                return Failure("set_rect_transform requires path.");
            }

            if (!TryGetRectTransform(path, out var go, out var rectTransform, out var error))
            {
                return Failure(error);
            }

            ApplyRectTransformArgs(args, rectTransform, false, out error);
            if (!string.IsNullOrEmpty(error))
            {
                return Failure(error);
            }

            MarkDirty(go);
            SaveSceneIfRequested(args, go);
            return Success("RectTransform updated.", BuildUiNodePayload(go));
        }

        private static EditorRpcMethodResult ExecuteSetAnchorPreset(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            var preset = GetRequiredString(args, "preset");
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(preset))
            {
                return Failure("set_anchor_preset requires path and preset.");
            }

            if (!TryGetRectTransform(path, out var go, out var rectTransform, out var error))
            {
                return Failure(error);
            }

            if (!ApplyAnchorPreset(rectTransform, preset, !GetBool(args, "preserve_offsets", false)))
            {
                return Failure("Unsupported preset: " + preset);
            }

            MarkDirty(go);
            SaveSceneIfRequested(args, go);
            return Success("Anchor preset applied.", BuildUiNodePayload(go));
        }

        private static EditorRpcMethodResult ExecuteSetUiText(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            var componentType = GetString(args, "component_type", string.Empty);
            var componentIndex = GetInt(args, "component_index", 0);
            if (string.IsNullOrEmpty(path))
            {
                return Failure("set_ui_text requires path.");
            }

            if (!TryFindGameObject(path, out var go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            if (!TryResolveComponent(go, componentType, componentIndex, typeof(Text), out var component, out var error))
            {
                return Failure(error);
            }

            var text = component as Text;
            if (text == null)
            {
                return Failure("Target component is not a UnityEngine.UI.Text.");
            }

            ApplyTextArgs(args, text, out error);
            if (!string.IsNullOrEmpty(error))
            {
                return Failure(error);
            }

            MarkDirty(go);
            SaveSceneIfRequested(args, go);
            return Success("UI text updated.", new UiTextPayload
            {
                path = GetGameObjectPath(go),
                componentType = text.GetType().FullName,
                text = text.text,
                fontSize = text.fontSize,
                color = FormatColor(text.color)
            });
        }

        private static EditorRpcMethodResult ExecuteSetUiGraphic(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            var componentType = GetString(args, "component_type", string.Empty);
            var componentIndex = GetInt(args, "component_index", 0);
            if (string.IsNullOrEmpty(path))
            {
                return Failure("set_ui_graphic requires path.");
            }

            if (!TryFindGameObject(path, out var go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            if (!TryResolveComponent(go, componentType, componentIndex, typeof(Graphic), out var component, out var error))
            {
                return Failure(error);
            }

            var graphic = component as Graphic;
            if (graphic == null)
            {
                return Failure("Target component is not a UnityEngine.UI.Graphic.");
            }

            ApplyGraphicArgs(args, graphic, out error);
            if (!string.IsNullOrEmpty(error))
            {
                return Failure(error);
            }

            var image = graphic as Image;
            MarkDirty(go);
            SaveSceneIfRequested(args, go);
            return Success("UI graphic updated.", new UiGraphicPayload
            {
                path = GetGameObjectPath(go),
                componentType = graphic.GetType().FullName,
                color = FormatColor(graphic.color),
                spritePath = image != null && image.sprite != null ? AssetDatabase.GetAssetPath(image.sprite) : string.Empty,
                raycastTarget = graphic.raycastTarget
            });
        }

        private static EditorRpcMethodResult ExecuteSetUiInteractable(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            var componentType = GetString(args, "component_type", string.Empty);
            var componentIndex = GetInt(args, "component_index", 0);
            if (string.IsNullOrEmpty(path))
            {
                return Failure("set_ui_interactable requires path.");
            }

            if (!TryFindGameObject(path, out var go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            Component component;
            string error;
            if (string.Equals(componentType, typeof(CanvasGroup).Name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(componentType, typeof(CanvasGroup).FullName, StringComparison.OrdinalIgnoreCase))
            {
                if (!TryResolveComponent(go, componentType, componentIndex, typeof(CanvasGroup), out component, out error))
                {
                    return Failure(error);
                }
            }
            else if (!TryResolveComponent(go, componentType, componentIndex, typeof(Selectable), out component, out error))
            {
                if (!TryResolveComponent(go, componentType, componentIndex, typeof(CanvasGroup), out component, out error))
                {
                    return Failure(error);
                }
            }

            var selectable = component as Selectable;
            var canvasGroup = component as CanvasGroup;
            if (selectable != null)
            {
                if (TryGetOptionalBool(args, "interactable", out var interactable))
                {
                    selectable.interactable = interactable;
                }
            }

            if (canvasGroup != null)
            {
                if (TryGetOptionalBool(args, "interactable", out var interactable))
                {
                    canvasGroup.interactable = interactable;
                }

                if (TryGetOptionalFloat(args, "alpha", out var alpha))
                {
                    canvasGroup.alpha = alpha;
                }

                if (TryGetOptionalBool(args, "blocks_raycasts", out var blocksRaycasts))
                {
                    canvasGroup.blocksRaycasts = blocksRaycasts;
                }

                if (TryGetOptionalBool(args, "ignore_parent_groups", out var ignoreParentGroups))
                {
                    canvasGroup.ignoreParentGroups = ignoreParentGroups;
                }
            }

            MarkDirty(go);
            SaveSceneIfRequested(args, go);
            return Success("UI interactable state updated.", new UiSelectablePayload
            {
                path = GetGameObjectPath(go),
                componentType = component.GetType().FullName,
                interactable = selectable != null ? selectable.interactable : canvasGroup != null && canvasGroup.interactable,
                alpha = canvasGroup != null ? canvasGroup.alpha : 1f,
                blocksRaycasts = canvasGroup == null || canvasGroup.blocksRaycasts
            });
        }

        private static EditorRpcMethodResult ExecuteReparentUiNode(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            if (string.IsNullOrEmpty(path))
            {
                return Failure("reparent_ui_node requires path.");
            }

            if (!TryFindGameObject(path, out var go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            Transform parent = null;
            var parentPath = GetString(args, "parent_path", string.Empty);
            if (!string.IsNullOrEmpty(parentPath))
            {
                if (!TryFindGameObject(parentPath, out var parentGo))
                {
                    return Failure("Parent GameObject not found at hierarchy path: " + parentPath);
                }

                parent = parentGo.transform;
            }

            go.transform.SetParent(parent, GetBool(args, "world_position_stays", false));
            if (TryGetOptionalInt(args, "sibling_index", out var siblingIndex))
            {
                go.transform.SetSiblingIndex(Mathf.Max(0, siblingIndex));
            }

            MarkDirty(go);
            SaveSceneIfRequested(args, go);
            return Success("UI node reparented.", BuildUiNodePayload(go));
        }

        private static EditorRpcMethodResult ExecuteDuplicateUiNode(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            if (string.IsNullOrEmpty(path))
            {
                return Failure("duplicate_ui_node requires path.");
            }

            if (!TryFindGameObject(path, out var source))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            GameObject duplicate;
            var parentPath = GetString(args, "parent_path", string.Empty);
            if (!string.IsNullOrEmpty(parentPath))
            {
                if (!TryFindGameObject(parentPath, out var parentGo))
                {
                    return Failure("Parent GameObject not found at hierarchy path: " + parentPath);
                }

                duplicate = UnityEngine.Object.Instantiate(source, parentGo.transform, false);
            }
            else if (source.transform.parent != null)
            {
                duplicate = UnityEngine.Object.Instantiate(source, source.transform.parent, false);
            }
            else
            {
                duplicate = UnityEngine.Object.Instantiate(source);
                SceneManager.MoveGameObjectToScene(duplicate, source.scene);
            }

            Undo.RegisterCreatedObjectUndo(duplicate, "Editor RPC Duplicate UI Node");
            var name = GetString(args, "name", string.Empty);
            if (!string.IsNullOrEmpty(name))
            {
                duplicate.name = name;
            }

            MarkDirty(duplicate);
            SaveSceneIfRequested(args, duplicate);
            return Success("UI node duplicated.", BuildUiNodePayload(duplicate));
        }

        private static EditorRpcMethodResult ExecuteAddUiLayoutGroup(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            var layoutType = GetRequiredString(args, "layout_type");
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(layoutType))
            {
                return Failure("add_ui_layout_group requires path and layout_type.");
            }

            if (!TryFindGameObject(path, out var go))
            {
                return Failure("GameObject not found at hierarchy path: " + path);
            }

            if (go.GetComponent<RectTransform>() == null)
            {
                return Failure("Target object does not have a RectTransform.");
            }

            layoutType = layoutType.Trim().ToLowerInvariant();
            string payloadSpacing;
            string payloadPadding;
            string payloadAlignment;
            switch (layoutType)
            {
                case "vertical":
                    {
                        var layout = GetOrAddComponent<VerticalLayoutGroup>(go);
                        ApplyLayoutGroupArgs(args, layout, out payloadSpacing, out payloadPadding, out payloadAlignment, out var error);
                        if (!string.IsNullOrEmpty(error))
                        {
                            return Failure(error);
                        }

                        MarkDirty(go);
                        SaveSceneIfRequested(args, go);
                        return Success("VerticalLayoutGroup configured.", new UiLayoutPayload
                        {
                            path = GetGameObjectPath(go),
                            layoutType = layout.GetType().Name,
                            spacing = payloadSpacing,
                            padding = payloadPadding,
                            childAlignment = payloadAlignment
                        });
                    }
                case "horizontal":
                    {
                        var layout = GetOrAddComponent<HorizontalLayoutGroup>(go);
                        ApplyLayoutGroupArgs(args, layout, out payloadSpacing, out payloadPadding, out payloadAlignment, out var error);
                        if (!string.IsNullOrEmpty(error))
                        {
                            return Failure(error);
                        }

                        MarkDirty(go);
                        SaveSceneIfRequested(args, go);
                        return Success("HorizontalLayoutGroup configured.", new UiLayoutPayload
                        {
                            path = GetGameObjectPath(go),
                            layoutType = layout.GetType().Name,
                            spacing = payloadSpacing,
                            padding = payloadPadding,
                            childAlignment = payloadAlignment
                        });
                    }
                case "grid":
                    {
                        var layout = GetOrAddComponent<GridLayoutGroup>(go);
                        if (TryGetVector2(args, "cell_size", out var cellSize))
                        {
                            layout.cellSize = cellSize;
                        }

                        if (TryGetVector2(args, "spacing", out var gridSpacing))
                        {
                            layout.spacing = gridSpacing;
                        }
                        else if (TryGetOptionalFloat(args, "spacing", out var sharedSpacing))
                        {
                            layout.spacing = new Vector2(sharedSpacing, sharedSpacing);
                        }

                        if (TryParsePadding(GetString(args, "padding", string.Empty), out var padding))
                        {
                            layout.padding = padding;
                        }

                        if (TryParseEnumValue<GridLayoutGroup.Constraint>(GetString(args, "constraint", string.Empty), out var constraint))
                        {
                            layout.constraint = constraint;
                        }

                        if (TryGetOptionalInt(args, "constraint_count", out var constraintCount))
                        {
                            layout.constraintCount = constraintCount;
                        }

                        if (TryParseEnumValue<TextAnchor>(GetString(args, "child_alignment", string.Empty), out var childAlignment))
                        {
                            layout.childAlignment = childAlignment;
                        }

                        MarkDirty(go);
                        SaveSceneIfRequested(args, go);
                        return Success("GridLayoutGroup configured.", new UiLayoutPayload
                        {
                            path = GetGameObjectPath(go),
                            layoutType = layout.GetType().Name,
                            spacing = FormatVector2(layout.spacing),
                            padding = FormatPadding(layout.padding),
                            childAlignment = layout.childAlignment.ToString()
                        });
                    }
                case "fitter":
                    {
                        var fitter = GetOrAddComponent<ContentSizeFitter>(go);
                        if (TryParseEnumValue<ContentSizeFitter.FitMode>(GetString(args, "fit_horizontal", string.Empty), out var fitHorizontal))
                        {
                            fitter.horizontalFit = fitHorizontal;
                        }

                        if (TryParseEnumValue<ContentSizeFitter.FitMode>(GetString(args, "fit_vertical", string.Empty), out var fitVertical))
                        {
                            fitter.verticalFit = fitVertical;
                        }

                        MarkDirty(go);
                        SaveSceneIfRequested(args, go);
                        return Success("ContentSizeFitter configured.", new UiLayoutPayload
                        {
                            path = GetGameObjectPath(go),
                            layoutType = fitter.GetType().Name,
                            spacing = string.Empty,
                            padding = string.Empty,
                            childAlignment = string.Empty
                        });
                    }
                default:
                    return Failure("Unsupported layout_type: " + layoutType);
            }
        }

        private static EditorRpcMethodResult ExecuteBatchUiOperations(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var rawOperations = GetRequiredString(args, "operations");
            if (!TrySplitJsonObjectArray(rawOperations, out var operations, out var error))
            {
                return Failure(error);
            }

            bool stopOnError = GetBool(args, "stop_on_error", true);
            var results = new List<UiBatchOperationResult>();
            int successCount = 0;
            int failedCount = 0;

            for (int i = 0; i < operations.Count; i++)
            {
                string operationJson = operations[i];
                var operationArgs = ParseArgs(operationJson);
                string operationMethod = GetString(operationArgs, "method", GetString(operationArgs, "operation", string.Empty));
                EditorRpcMethodResult result = ExecuteSingleUiBatchOperation(operationMethod, operationJson);
                bool operationSucceeded = result != null && result.success;
                if (operationSucceeded)
                {
                    successCount++;
                }
                else
                {
                    failedCount++;
                }

                results.Add(new UiBatchOperationResult
                {
                    index = i,
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

            if (successCount > 0 && GetBool(args, "save_scene", false))
            {
                EditorSceneManager.SaveOpenScenes();
            }

            return Success("UI batch operations completed.", new UiBatchOperationsPayload
            {
                requestedCount = operations.Count,
                succeededCount = successCount,
                failedCount = failedCount,
                results = results.ToArray()
            });
        }

        private static EditorRpcMethodResult ExecuteSingleUiBatchOperation(string operationMethod, string operationJson)
        {
            switch (NormalizeStringValue(operationMethod).Trim().ToLowerInvariant())
            {
                case "create_ui_container":
                    return ExecuteCreateUiContainer("create_ui_container", operationJson);
                case "create_ui_image":
                    return ExecuteCreateUiImage("create_ui_image", operationJson);
                case "create_ui_text":
                    return ExecuteCreateUiText("create_ui_text", operationJson);
                case "create_ui_button":
                    return ExecuteCreateUiButton("create_ui_button", operationJson);
                case "create_ui_scroll_view":
                    return ExecuteCreateUiScrollView("create_ui_scroll_view", operationJson);
                case "set_rect_transform":
                    return ExecuteSetRectTransform("set_rect_transform", operationJson);
                case "set_anchor_preset":
                    return ExecuteSetAnchorPreset("set_anchor_preset", operationJson);
                case "set_ui_text":
                    return ExecuteSetUiText("set_ui_text", operationJson);
                case "set_ui_graphic":
                    return ExecuteSetUiGraphic("set_ui_graphic", operationJson);
                case "set_ui_interactable":
                    return ExecuteSetUiInteractable("set_ui_interactable", operationJson);
                case "reparent_ui_node":
                    return ExecuteReparentUiNode("reparent_ui_node", operationJson);
                case "duplicate_ui_node":
                    return ExecuteDuplicateUiNode("duplicate_ui_node", operationJson);
                case "add_ui_layout_group":
                    return ExecuteAddUiLayoutGroup("add_ui_layout_group", operationJson);
                default:
                    return Failure("Unsupported UI batch operation method: " + operationMethod);
            }
        }

        private static UiControllerPayload BuildUiControllerPayload(string argumentsJson, bool prefabOnly, out string error)
        {
            error = string.Empty;
            var args = ParseArgs(argumentsJson);
            var path = GetRequiredString(args, "path");
            var componentType = GetString(args, "component_type", string.Empty);
            var componentIndex = GetInt(args, "component_index", 0);
            if (string.IsNullOrEmpty(path))
            {
                error = prefabOnly ? "find_ui_prefab_references requires path." : "inspect_ui_controller requires path.";
                return null;
            }

            if (!TryFindGameObject(path, out var go))
            {
                error = "GameObject not found at hierarchy path: " + path;
                return null;
            }

            if (!TryResolveUiControllerComponent(go, componentType, componentIndex, out var component, out error))
            {
                return null;
            }

            var bindings = CollectUiBindingInfos(component, prefabOnly);
            return new UiControllerPayload
            {
                path = GetGameObjectPath(go),
                componentType = component.GetType().FullName,
                scenePath = go.scene.path,
                activeSelf = go.activeSelf,
                bindingCount = bindings.Length,
                bindings = bindings
            };
        }

        private static void AddOrMergeUiRoot(Dictionary<string, UiRootInfo> map, GameObject go, string kind)
        {
            if (go == null)
            {
                return;
            }

            var path = GetGameObjectPath(go);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (!map.TryGetValue(path, out var info))
            {
                var canvas = go.GetComponent<Canvas>();
                var raycaster = go.GetComponent<GraphicRaycaster>();
                info = new UiRootInfo
                {
                    path = path,
                    name = go.name,
                    scenePath = go.scene.path,
                    kind = kind,
                    activeSelf = go.activeSelf,
                    isCanvasRoot = canvas != null,
                    hasGraphicRaycaster = raycaster != null,
                    renderMode = canvas != null ? canvas.renderMode.ToString() : string.Empty,
                    directChildCount = go.transform.childCount,
                    componentTypes = GetComponentTypeNames(go)
                };
                map.Add(path, info);
                return;
            }

            if (info.kind.IndexOf(kind, StringComparison.OrdinalIgnoreCase) < 0)
            {
                info.kind += "," + kind;
            }

            var existingCanvas = go.GetComponent<Canvas>();
            if (existingCanvas != null)
            {
                info.isCanvasRoot = true;
                info.renderMode = existingCanvas.renderMode.ToString();
            }

            info.hasGraphicRaycaster = info.hasGraphicRaycaster || go.GetComponent<GraphicRaycaster>() != null;
            info.componentTypes = GetComponentTypeNames(go);
        }

        private static void AppendUiTreeNode(Transform current, int depth, int maxDepth, bool includeInactive, bool uiOnly, List<UiTreeNodeInfo> nodes, int limit)
        {
            if (current == null || nodes.Count >= limit)
            {
                return;
            }

            var go = current.gameObject;
            if (!includeInactive && !go.activeSelf)
            {
                return;
            }

            var shouldInclude = !uiOnly || IsUiGameObject(go);
            if (shouldInclude)
            {
                nodes.Add(BuildUiTreeNodeInfo(go, depth));
            }

            if (depth >= maxDepth)
            {
                return;
            }

            for (int childIndex = 0; childIndex < current.childCount && nodes.Count < limit; childIndex++)
            {
                AppendUiTreeNode(current.GetChild(childIndex), depth + 1, maxDepth, includeInactive, uiOnly, nodes, limit);
            }
        }

        private static UiTreeNodeInfo BuildUiTreeNodeInfo(GameObject go, int depth)
        {
            var rectTransform = go != null ? go.GetComponent<RectTransform>() : null;
            var graphic = go != null ? go.GetComponent<Graphic>() : null;
            var selectable = go != null ? go.GetComponent<Selectable>() : null;
            var layout = go != null ? go.GetComponent<LayoutGroup>() : null;
            var fitter = go != null ? go.GetComponent<ContentSizeFitter>() : null;
            return new UiTreeNodeInfo
            {
                path = GetGameObjectPath(go),
                name = go != null ? go.name : string.Empty,
                depth = depth,
                childCount = go != null ? go.transform.childCount : 0,
                activeSelf = go != null && go.activeSelf,
                componentTypes = GetComponentTypeNames(go),
                anchorMin = rectTransform != null ? FormatVector2(rectTransform.anchorMin) : string.Empty,
                anchorMax = rectTransform != null ? FormatVector2(rectTransform.anchorMax) : string.Empty,
                anchoredPosition = rectTransform != null ? FormatVector2(rectTransform.anchoredPosition) : string.Empty,
                sizeDelta = rectTransform != null ? FormatVector2(rectTransform.sizeDelta) : string.Empty,
                pivot = rectTransform != null ? FormatVector2(rectTransform.pivot) : string.Empty,
                graphicType = graphic != null ? graphic.GetType().Name : string.Empty,
                textPreview = ExtractTextPreview(go),
                selectableType = selectable != null ? selectable.GetType().Name : string.Empty,
                layoutType = layout != null ? layout.GetType().Name : fitter != null ? fitter.GetType().Name : string.Empty
            };
        }

        private static void CollectUnityEventBindings(Component component, List<UiEventBindingInfo> events)
        {
            if (component == null || events == null)
            {
                return;
            }

            var fields = GetSerializableInstanceFields(component.GetType());
            for (int fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
            {
                var field = fields[fieldIndex];
                if (!typeof(UnityEventBase).IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                var unityEvent = field.GetValue(component) as UnityEventBase;
                if (unityEvent == null)
                {
                    continue;
                }

                var listeners = new List<UiEventListenerInfo>();
                for (int listenerIndex = 0; listenerIndex < unityEvent.GetPersistentEventCount(); listenerIndex++)
                {
                    var target = unityEvent.GetPersistentTarget(listenerIndex);
                    listeners.Add(new UiEventListenerInfo
                    {
                        target = target != null ? DescribeSelectionObject(target) : string.Empty,
                        methodName = unityEvent.GetPersistentMethodName(listenerIndex),
                        mode = target != null ? target.GetType().Name : string.Empty,
                        callState = unityEvent.GetPersistentListenerState(listenerIndex).ToString()
                    });
                }

                events.Add(new UiEventBindingInfo
                {
                    ownerPath = GetGameObjectPath(component.gameObject),
                    componentType = component.GetType().FullName,
                    eventName = NormalizeEventFieldName(field.Name),
                    listenerCount = listeners.Count,
                    listeners = listeners.ToArray()
                });
            }
        }

        private static UiBindingInfo[] CollectUiBindingInfos(Component component, bool prefabOnly)
        {
            var bindings = new List<UiBindingInfo>();
            if (component == null)
            {
                return bindings.ToArray();
            }

            var fields = GetSerializableInstanceFields(component.GetType());
            for (int fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
            {
                var field = fields[fieldIndex];
                var fieldValue = field.GetValue(component);
                AppendBindingInfos(bindings, field.Name, field.FieldType, fieldValue, prefabOnly);
            }

            bindings.Sort((left, right) => string.CompareOrdinal(left.fieldName, right.fieldName));
            return bindings.ToArray();
        }

        private static void AppendBindingInfos(List<UiBindingInfo> bindings, string fieldName, Type fieldType, object fieldValue, bool prefabOnly)
        {
            if (bindings == null || string.IsNullOrEmpty(fieldName) || fieldType == null || fieldValue == null)
            {
                return;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                AppendBindingInfo(bindings, fieldName, fieldType, fieldValue as UnityEngine.Object, prefabOnly);
                return;
            }

            if (fieldType == typeof(string))
            {
                return;
            }

            if (fieldType.IsArray && typeof(UnityEngine.Object).IsAssignableFrom(fieldType.GetElementType()))
            {
                var array = fieldValue as Array;
                if (array == null)
                {
                    return;
                }

                for (int i = 0; i < array.Length; i++)
                {
                    AppendBindingInfo(bindings, fieldName + "[" + i.ToString(CultureInfo.InvariantCulture) + "]", fieldType.GetElementType(), array.GetValue(i) as UnityEngine.Object, prefabOnly);
                }

                return;
            }

            if (!typeof(IList).IsAssignableFrom(fieldType) || !fieldType.IsGenericType)
            {
                return;
            }

            var elementType = fieldType.GetGenericArguments()[0];
            if (!typeof(UnityEngine.Object).IsAssignableFrom(elementType))
            {
                return;
            }

            var list = fieldValue as IList;
            if (list == null)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                AppendBindingInfo(bindings, fieldName + "[" + i.ToString(CultureInfo.InvariantCulture) + "]", elementType, list[i] as UnityEngine.Object, prefabOnly);
            }
        }

        private static void AppendBindingInfo(List<UiBindingInfo> bindings, string fieldName, Type memberType, UnityEngine.Object value, bool prefabOnly)
        {
            if (bindings == null || value == null)
            {
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(value);
            var isSceneReference = string.IsNullOrEmpty(assetPath);
            if (prefabOnly)
            {
                if (isSceneReference || string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            bindings.Add(new UiBindingInfo
            {
                fieldName = fieldName,
                memberType = memberType.FullName,
                valueType = value.GetType().FullName,
                target = DescribeSelectionObject(value),
                targetPath = GetReferencePath(value),
                isSceneReference = isSceneReference,
                isUiRelated = IsUiReference(value)
            });
        }

        private static string GetReferencePath(UnityEngine.Object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var go = value as GameObject;
            if (go != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(go);
                return string.IsNullOrEmpty(assetPath) ? GetGameObjectPath(go) : assetPath;
            }

            var component = value as Component;
            if (component != null)
            {
                var componentAssetPath = AssetDatabase.GetAssetPath(component);
                if (!string.IsNullOrEmpty(componentAssetPath))
                {
                    return componentAssetPath;
                }

                return GetGameObjectPath(component.gameObject) + "#" + component.GetType().Name;
            }

            return AssetDatabase.GetAssetPath(value);
        }

        private static bool IsUiReference(UnityEngine.Object value)
        {
            if (value == null)
            {
                return false;
            }

            if (value is Sprite || value is Font || value is Material)
            {
                return true;
            }

            var go = value as GameObject;
            if (go != null)
            {
                return IsUiGameObject(go);
            }

            var component = value as Component;
            if (component != null)
            {
                return IsUiGameObject(component.gameObject) || component is Graphic || component is Selectable || component is LayoutGroup || component is CanvasGroup;
            }

            return false;
        }

        private static bool TryResolveUiControllerComponent(GameObject go, string componentType, int componentIndex, out Component component, out string error)
        {
            error = string.Empty;
            component = null;
            if (go == null)
            {
                error = "GameObject is null.";
                return false;
            }

            if (!string.IsNullOrEmpty(componentType))
            {
                return TryResolveComponent(go, componentType, componentIndex, typeof(Component), out component, out error);
            }

            var behaviours = go.GetComponents<MonoBehaviour>();
            var matches = new List<Component>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (IsUiControllerComponent(behaviours[i]))
                {
                    matches.Add(behaviours[i]);
                }
            }

            if (matches.Count == 0)
            {
                error = "No UI controller component was found on the target object.";
                return false;
            }

            if (componentIndex < 0 || componentIndex >= matches.Count)
            {
                error = "component_index is out of range for the available UI controller components.";
                return false;
            }

            component = matches[componentIndex];
            return true;
        }

        private static List<Component> ResolveComponentsForEventInspection(GameObject go, string componentType, int componentIndex, out string error)
        {
            error = string.Empty;
            var results = new List<Component>();
            if (go == null)
            {
                error = "GameObject is null.";
                return null;
            }

            if (!string.IsNullOrEmpty(componentType))
            {
                if (!TryResolveComponent(go, componentType, componentIndex, typeof(Component), out var component, out error))
                {
                    return null;
                }

                results.Add(component);
                return results;
            }

            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                {
                    results.Add(components[i]);
                }
            }

            return results;
        }

        private static bool TryResolveComponent(GameObject go, string componentType, int componentIndex, Type baseType, out Component component, out string error)
        {
            error = string.Empty;
            component = null;
            if (go == null)
            {
                error = "GameObject is null.";
                return false;
            }

            Type resolvedType = null;
            if (!string.IsNullOrEmpty(componentType))
            {
                resolvedType = ResolveTypeByName(componentType, typeof(Component));
                if (resolvedType == null)
                {
                    error = "Could not resolve component type: " + componentType;
                    return false;
                }
            }
            else if (baseType == typeof(Text))
            {
                resolvedType = typeof(Text);
            }
            else if (baseType == typeof(Graphic))
            {
                resolvedType = typeof(Graphic);
            }
            else if (baseType == typeof(Selectable))
            {
                resolvedType = typeof(Selectable);
            }
            else if (baseType == typeof(CanvasGroup))
            {
                resolvedType = typeof(CanvasGroup);
            }
            else
            {
                resolvedType = typeof(Component);
            }

            var components = go.GetComponents<Component>();
            var matches = new List<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                var candidate = components[i];
                if (candidate == null)
                {
                    continue;
                }

                if (!resolvedType.IsAssignableFrom(candidate.GetType()))
                {
                    continue;
                }

                if (baseType != null && baseType != typeof(Component) && !baseType.IsAssignableFrom(candidate.GetType()))
                {
                    continue;
                }

                matches.Add(candidate);
            }

            if (matches.Count == 0)
            {
                error = "No matching component was found on the target object.";
                return false;
            }

            if (componentIndex < 0 || componentIndex >= matches.Count)
            {
                error = "component_index is out of range for the matching components.";
                return false;
            }

            component = matches[componentIndex];
            return true;
        }

        private static bool TryCreateUiNode(Dictionary<string, string> args, out GameObject go, out string error)
        {
            error = string.Empty;
            go = null;
            var name = GetRequiredString(args, "name");
            if (string.IsNullOrEmpty(name))
            {
                error = "UI creation methods require name.";
                return false;
            }

            go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Editor RPC Create UI Node");
            if (!TryParentOrMoveUiNode(go, args, out error))
            {
                UnityEngine.Object.DestroyImmediate(go);
                go = null;
                return false;
            }

            if (!ApplyRectTransformArgs(args, go.GetComponent<RectTransform>(), true, out error))
            {
                UnityEngine.Object.DestroyImmediate(go);
                go = null;
                return false;
            }

            return true;
        }

        private static bool TryParentOrMoveUiNode(GameObject go, Dictionary<string, string> args, out string error)
        {
            error = string.Empty;
            if (go == null)
            {
                error = "GameObject is null.";
                return false;
            }

            var parentPath = GetString(args, "parent_path", string.Empty);
            if (!string.IsNullOrEmpty(parentPath))
            {
                if (!TryFindGameObject(parentPath, out var parentGo))
                {
                    error = "Parent GameObject not found at hierarchy path: " + parentPath;
                    return false;
                }

                go.transform.SetParent(parentGo.transform, false);
                return true;
            }

            if (!TryResolveScene(GetString(args, "scene_path", string.Empty), out var scene))
            {
                error = "Target scene is not loaded.";
                return false;
            }

            SceneManager.MoveGameObjectToScene(go, scene);
            return true;
        }

        private static bool TryGetRectTransform(string path, out GameObject go, out RectTransform rectTransform, out string error)
        {
            error = string.Empty;
            rectTransform = null;
            go = null;
            if (!TryFindGameObject(path, out go))
            {
                error = "GameObject not found at hierarchy path: " + path;
                return false;
            }

            rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                error = "Target object does not have a RectTransform.";
                return false;
            }

            return true;
        }

        private static bool ApplyRectTransformArgs(Dictionary<string, string> args, RectTransform rectTransform, bool allowDefaultPreset, out string error)
        {
            error = string.Empty;
            if (rectTransform == null)
            {
                error = "RectTransform is null.";
                return false;
            }

            var anchorPreset = GetString(args, "anchor_preset", string.Empty);
            if (!string.IsNullOrEmpty(anchorPreset))
            {
                if (!ApplyAnchorPreset(rectTransform, anchorPreset, allowDefaultPreset))
                {
                    error = "Unsupported anchor_preset: " + anchorPreset;
                    return false;
                }
            }

            if (TryGetVector2(args, "anchor_min", out var vector2Value))
            {
                rectTransform.anchorMin = vector2Value;
            }

            if (TryGetVector2(args, "anchor_max", out vector2Value))
            {
                rectTransform.anchorMax = vector2Value;
            }

            if (TryGetVector2(args, "anchored_position", out vector2Value))
            {
                rectTransform.anchoredPosition = vector2Value;
            }

            if (TryGetVector2(args, "size_delta", out vector2Value))
            {
                rectTransform.sizeDelta = vector2Value;
            }

            if (TryGetVector2(args, "pivot", out vector2Value))
            {
                rectTransform.pivot = vector2Value;
            }

            if (TryGetVector2(args, "offset_min", out vector2Value))
            {
                rectTransform.offsetMin = vector2Value;
            }

            if (TryGetVector2(args, "offset_max", out vector2Value))
            {
                rectTransform.offsetMax = vector2Value;
            }

            if (TryGetVector3(args, "local_scale", out var vector3Value))
            {
                rectTransform.localScale = vector3Value;
            }

            if (TryGetVector3(args, "local_rotation_euler", out vector3Value))
            {
                rectTransform.localEulerAngles = vector3Value;
            }

            if (TryGetOptionalInt(args, "sibling_index", out var siblingIndex))
            {
                rectTransform.SetSiblingIndex(Mathf.Max(0, siblingIndex));
            }

            return true;
        }

        private static bool ApplyAnchorPreset(RectTransform rectTransform, string preset, bool resetOffsets)
        {
            if (rectTransform == null || string.IsNullOrEmpty(preset))
            {
                return false;
            }

            Vector2 anchorMin;
            Vector2 anchorMax;
            Vector2 pivot;
            switch (preset.Trim().ToLowerInvariant())
            {
                case "stretch_full":
                    anchorMin = new Vector2(0f, 0f);
                    anchorMax = new Vector2(1f, 1f);
                    pivot = new Vector2(0.5f, 0.5f);
                    break;
                case "top_left":
                    anchorMin = anchorMax = new Vector2(0f, 1f);
                    pivot = new Vector2(0f, 1f);
                    break;
                case "top_center":
                    anchorMin = anchorMax = new Vector2(0.5f, 1f);
                    pivot = new Vector2(0.5f, 1f);
                    break;
                case "top_right":
                    anchorMin = anchorMax = new Vector2(1f, 1f);
                    pivot = new Vector2(1f, 1f);
                    break;
                case "middle_left":
                    anchorMin = anchorMax = new Vector2(0f, 0.5f);
                    pivot = new Vector2(0f, 0.5f);
                    break;
                case "center":
                    anchorMin = anchorMax = new Vector2(0.5f, 0.5f);
                    pivot = new Vector2(0.5f, 0.5f);
                    break;
                case "middle_right":
                    anchorMin = anchorMax = new Vector2(1f, 0.5f);
                    pivot = new Vector2(1f, 0.5f);
                    break;
                case "bottom_left":
                    anchorMin = anchorMax = new Vector2(0f, 0f);
                    pivot = new Vector2(0f, 0f);
                    break;
                case "bottom_center":
                    anchorMin = anchorMax = new Vector2(0.5f, 0f);
                    pivot = new Vector2(0.5f, 0f);
                    break;
                case "bottom_right":
                    anchorMin = anchorMax = new Vector2(1f, 0f);
                    pivot = new Vector2(1f, 0f);
                    break;
                case "stretch_top":
                    anchorMin = new Vector2(0f, 1f);
                    anchorMax = new Vector2(1f, 1f);
                    pivot = new Vector2(0.5f, 1f);
                    break;
                case "stretch_middle":
                    anchorMin = new Vector2(0f, 0.5f);
                    anchorMax = new Vector2(1f, 0.5f);
                    pivot = new Vector2(0.5f, 0.5f);
                    break;
                case "stretch_bottom":
                    anchorMin = new Vector2(0f, 0f);
                    anchorMax = new Vector2(1f, 0f);
                    pivot = new Vector2(0.5f, 0f);
                    break;
                case "stretch_left":
                    anchorMin = new Vector2(0f, 0f);
                    anchorMax = new Vector2(0f, 1f);
                    pivot = new Vector2(0f, 0.5f);
                    break;
                case "stretch_center":
                    anchorMin = new Vector2(0.5f, 0f);
                    anchorMax = new Vector2(0.5f, 1f);
                    pivot = new Vector2(0.5f, 0.5f);
                    break;
                case "stretch_right":
                    anchorMin = new Vector2(1f, 0f);
                    anchorMax = new Vector2(1f, 1f);
                    pivot = new Vector2(1f, 0.5f);
                    break;
                default:
                    return false;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            if (!resetOffsets)
            {
                return true;
            }

            if (anchorMin != anchorMax)
            {
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }
            else
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }

            return true;
        }

        private static UiNodePayload BuildUiNodePayload(GameObject go)
        {
            var rectTransform = go != null ? go.GetComponent<RectTransform>() : null;
            return new UiNodePayload
            {
                path = GetGameObjectPath(go),
                name = go != null ? go.name : string.Empty,
                scenePath = go != null ? go.scene.path : string.Empty,
                activeSelf = go != null && go.activeSelf,
                componentTypes = GetComponentTypeNames(go),
                anchorMin = rectTransform != null ? FormatVector2(rectTransform.anchorMin) : string.Empty,
                anchorMax = rectTransform != null ? FormatVector2(rectTransform.anchorMax) : string.Empty,
                anchoredPosition = rectTransform != null ? FormatVector2(rectTransform.anchoredPosition) : string.Empty,
                sizeDelta = rectTransform != null ? FormatVector2(rectTransform.sizeDelta) : string.Empty,
                pivot = rectTransform != null ? FormatVector2(rectTransform.pivot) : string.Empty
            };
        }

        private static void ApplyTextArgs(Dictionary<string, string> args, Text text, out string error)
        {
            error = string.Empty;
            if (text == null)
            {
                error = "Text component is null.";
                return;
            }

            if (args.ContainsKey("text"))
            {
                text.text = GetString(args, "text", text.text);
            }

            if (TryGetOptionalInt(args, "font_size", out var fontSize))
            {
                text.fontSize = fontSize;
            }

            if (TryGetColorArg(args, "color", out var color))
            {
                text.color = color;
            }

            if (TryParseEnumValue<TextAnchor>(GetString(args, "alignment", string.Empty), out var alignment))
            {
                text.alignment = alignment;
            }

            if (TryGetOptionalBool(args, "raycast_target", out var raycastTarget))
            {
                text.raycastTarget = raycastTarget;
            }

            if (TryParseEnumValue<HorizontalWrapMode>(GetString(args, "horizontal_overflow", string.Empty), out var horizontalOverflow))
            {
                text.horizontalOverflow = horizontalOverflow;
            }

            if (TryParseEnumValue<VerticalWrapMode>(GetString(args, "vertical_overflow", string.Empty), out var verticalOverflow))
            {
                text.verticalOverflow = verticalOverflow;
            }
        }

        private static void ApplyGraphicArgs(Dictionary<string, string> args, Graphic graphic, out string error)
        {
            error = string.Empty;
            if (graphic == null)
            {
                error = "Graphic component is null.";
                return;
            }

            if (TryGetColorArg(args, "color", out var color))
            {
                graphic.color = color;
            }

            if (TryGetOptionalBool(args, "raycast_target", out var raycastTarget))
            {
                graphic.raycastTarget = raycastTarget;
            }

            var image = graphic as Image;
            if (image == null)
            {
                return;
            }

            var spriteAssetPath = GetString(args, "sprite_asset_path", string.Empty);
            if (spriteAssetPath.StartsWith("asset:", StringComparison.OrdinalIgnoreCase))
            {
                spriteAssetPath = spriteAssetPath.Substring("asset:".Length);
            }

            if (!string.IsNullOrEmpty(spriteAssetPath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spriteAssetPath);
                if (sprite == null)
                {
                    error = "Sprite not found at asset path: " + spriteAssetPath;
                    return;
                }

                image.sprite = sprite;
            }

            if (TryGetOptionalBool(args, "preserve_aspect", out var preserveAspect))
            {
                image.preserveAspect = preserveAspect;
            }

            if (TryParseEnumValue<Image.Type>(GetString(args, "image_type", string.Empty), out var imageType))
            {
                image.type = imageType;
            }

            if (TryGetOptionalFloat(args, "fill_amount", out var fillAmount))
            {
                image.fillAmount = fillAmount;
            }
        }

        private static void ApplyLayoutGroupArgs(Dictionary<string, string> args, HorizontalOrVerticalLayoutGroup layout, out string spacing, out string padding, out string childAlignment, out string error)
        {
            spacing = string.Empty;
            padding = string.Empty;
            childAlignment = string.Empty;
            error = string.Empty;
            if (layout == null)
            {
                error = "LayoutGroup is null.";
                return;
            }

            if (TryGetOptionalFloat(args, "spacing", out var spacingValue))
            {
                layout.spacing = spacingValue;
            }

            if (TryParsePadding(GetString(args, "padding", string.Empty), out var rectOffset))
            {
                layout.padding = rectOffset;
            }

            if (TryParseEnumValue<TextAnchor>(GetString(args, "child_alignment", string.Empty), out var parsedAlignment))
            {
                layout.childAlignment = parsedAlignment;
            }

            if (TryGetOptionalBool(args, "child_control_width", out var childControlWidth))
            {
                layout.childControlWidth = childControlWidth;
            }

            if (TryGetOptionalBool(args, "child_control_height", out var childControlHeight))
            {
                layout.childControlHeight = childControlHeight;
            }

            if (TryGetOptionalBool(args, "child_force_expand_width", out var childForceExpandWidth))
            {
                layout.childForceExpandWidth = childForceExpandWidth;
            }

            if (TryGetOptionalBool(args, "child_force_expand_height", out var childForceExpandHeight))
            {
                layout.childForceExpandHeight = childForceExpandHeight;
            }

            spacing = layout.spacing.ToString("0.###", CultureInfo.InvariantCulture);
            padding = FormatPadding(layout.padding);
            childAlignment = layout.childAlignment.ToString();
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(go);
        }

        private static bool TryGetColorArg(Dictionary<string, string> args, string key, out Color color)
        {
            color = Color.white;
            if (!args.TryGetValue(key, out var rawValue))
            {
                return false;
            }

            return TryParseColor(NormalizeStringValue(rawValue), out color);
        }

        private static bool TryGetOptionalInt(Dictionary<string, string> args, string key, out int value)
        {
            value = 0;
            if (!args.TryGetValue(key, out var rawValue))
            {
                return false;
            }

            return int.TryParse(NormalizeStringValue(rawValue), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseEnumValue<T>(string rawValue, out T value) where T : struct
        {
            value = default(T);
            if (string.IsNullOrEmpty(rawValue))
            {
                return false;
            }

            rawValue = NormalizeStringValue(rawValue);
            if (Enum.TryParse<T>(rawValue, true, out value))
            {
                return true;
            }

            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            {
                value = (T)Enum.ToObject(typeof(T), intValue);
                return true;
            }

            return false;
        }

        private static bool TrySplitJsonObjectArray(string rawValue, out List<string> objects, out string error)
        {
            objects = new List<string>();
            error = string.Empty;
            rawValue = NormalizeStringValue(rawValue).Trim();
            if (string.IsNullOrEmpty(rawValue))
            {
                error = "batch_ui_operations requires operations.";
                return false;
            }

            if (!rawValue.StartsWith("[", StringComparison.Ordinal) || !rawValue.EndsWith("]", StringComparison.Ordinal))
            {
                error = "operations must be a JSON array.";
                return false;
            }

            int index = 1;
            while (index < rawValue.Length - 1)
            {
                while (index < rawValue.Length - 1 && (char.IsWhiteSpace(rawValue[index]) || rawValue[index] == ','))
                {
                    index++;
                }

                if (index >= rawValue.Length - 1)
                {
                    break;
                }

                if (rawValue[index] != '{')
                {
                    error = "Each operations entry must be a JSON object.";
                    return false;
                }

                int start = index;
                int depth = 0;
                bool inString = false;
                bool escape = false;
                while (index < rawValue.Length)
                {
                    char current = rawValue[index];
                    if (escape)
                    {
                        escape = false;
                    }
                    else if (current == '\\')
                    {
                        escape = true;
                    }
                    else if (current == '"')
                    {
                        inString = !inString;
                    }
                    else if (!inString)
                    {
                        if (current == '{')
                        {
                            depth++;
                        }
                        else if (current == '}')
                        {
                            depth--;
                            if (depth == 0)
                            {
                                index++;
                                objects.Add(rawValue.Substring(start, index - start));
                                break;
                            }
                        }
                    }

                    index++;
                }

                if (depth != 0)
                {
                    error = "operations contains an unbalanced JSON object.";
                    return false;
                }
            }

            if (objects.Count == 0)
            {
                error = "operations is empty.";
                return false;
            }

            return true;
        }

        private static bool TryParsePadding(string rawValue, out RectOffset padding)
        {
            padding = new RectOffset();
            rawValue = NormalizeStringValue(rawValue);
            if (string.IsNullOrEmpty(rawValue))
            {
                return false;
            }

            rawValue = rawValue.Trim().Trim('[', ']', '(', ')');
            var parts = rawValue.Split(new[] { ',', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
            {
                return false;
            }

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var left) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var top) ||
                !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var right) ||
                !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var bottom))
            {
                return false;
            }

            padding = new RectOffset(left, right, top, bottom);
            return true;
        }

        private static string FormatColor(Color value)
        {
            return value.r.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   value.g.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   value.b.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   value.a.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatPadding(RectOffset padding)
        {
            if (padding == null)
            {
                return string.Empty;
            }

            return padding.left.ToString(CultureInfo.InvariantCulture) + "," +
                   padding.top.ToString(CultureInfo.InvariantCulture) + "," +
                   padding.right.ToString(CultureInfo.InvariantCulture) + "," +
                   padding.bottom.ToString(CultureInfo.InvariantCulture);
        }

        private static Font GetDefaultFont()
        {
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void EnsureCanvasRenderer(GameObject go)
        {
            if (go != null && go.GetComponent<CanvasRenderer>() == null)
            {
                Undo.AddComponent<CanvasRenderer>(go);
            }
        }

        private static void ApplyCanvasRootRect(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localEulerAngles = Vector3.zero;
        }

        private static bool TryApplyCanvasRenderMode(Canvas canvas, string rawValue)
        {
            if (canvas == null)
            {
                return false;
            }

            switch (NormalizeStringValue(rawValue).Trim().ToLowerInvariant())
            {
                case "":
                case "overlay":
                case "screen_space_overlay":
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    return true;
                case "screen_space_camera":
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    return true;
                case "world_space":
                    canvas.renderMode = RenderMode.WorldSpace;
                    return true;
                default:
                    return false;
            }
        }

        private static void EnsureSceneEventSystem(Scene scene)
        {
            if (!scene.IsValid())
            {
                return;
            }

            var roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                if (roots[rootIndex].GetComponentInChildren<EventSystem>(true) != null)
                {
                    return;
                }
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Editor RPC Ensure EventSystem");
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
        }

        private static GameObject FindRootGameObject(Scene scene, string name)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(name))
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (string.Equals(roots[i].name, name, StringComparison.Ordinal))
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static GameObject FindDirectChild(Transform parent, string name)
        {
            if (parent == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static string ExtractTextPreview(GameObject go)
        {
            if (go == null)
            {
                return string.Empty;
            }

            var text = go.GetComponent<Text>();
            if (text != null)
            {
                return TrimTextPreview(text.text);
            }

            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null)
                {
                    continue;
                }

                var property = component.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
                if (property == null || property.PropertyType != typeof(string) || !property.CanRead)
                {
                    continue;
                }

                try
                {
                    return TrimTextPreview(property.GetValue(component, null) as string);
                }
                catch
                {
                }
            }

            return string.Empty;
        }

        private static string TrimTextPreview(string value)
        {
            value = value ?? string.Empty;
            return value.Length <= 80 ? value : value.Substring(0, 80);
        }

        private static bool IsUiGameObject(GameObject go)
        {
            if (go == null)
            {
                return false;
            }

            return go.GetComponent<RectTransform>() != null ||
                   go.GetComponent<Canvas>() != null ||
                   go.GetComponent<Graphic>() != null ||
                   go.GetComponent<Selectable>() != null ||
                   go.GetComponent<CanvasGroup>() != null ||
                   go.GetComponent<LayoutGroup>() != null ||
                   go.GetComponent<ContentSizeFitter>() != null ||
                   go.GetComponent<Mask>() != null ||
                   go.GetComponent<RectMask2D>() != null ||
                   go.GetComponent<ScrollRect>() != null ||
                   go.GetComponent<GraphicRaycaster>() != null ||
                   go.GetComponent<EventSystem>() != null;
        }

        private static bool IsUiControllerComponent(Component component)
        {
            if (component == null || component is Transform || component is RectTransform)
            {
                return false;
            }

            if (component is Canvas || component is CanvasScaler || component is GraphicRaycaster || component is UIBehaviour || component is EventSystem || component is BaseInputModule)
            {
                return false;
            }

            var behaviour = component as MonoBehaviour;
            if (behaviour == null)
            {
                return false;
            }

            return behaviour.GetType().Name.IndexOf("UI", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static List<FieldInfo> GetSerializableInstanceFields(Type type)
        {
            var fields = new List<FieldInfo>();
            var current = type;
            while (current != null && current != typeof(MonoBehaviour) && current != typeof(Behaviour) && current != typeof(Component) && current != typeof(UnityEngine.Object))
            {
                var declaredFields = current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (int i = 0; i < declaredFields.Length; i++)
                {
                    var field = declaredFields[i];
                    if (!IsSerializedField(field))
                    {
                        continue;
                    }

                    fields.Add(field);
                }

                current = current.BaseType;
            }

            return fields;
        }

        private static bool IsSerializedField(FieldInfo field)
        {
            if (field == null || field.IsStatic || field.IsNotSerialized)
            {
                return false;
            }

            return field.IsPublic || field.GetCustomAttributes(typeof(SerializeField), true).Length > 0;
        }

        private static string NormalizeEventFieldName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return string.Empty;
            }

            if (fieldName.StartsWith("m_", StringComparison.Ordinal))
            {
                fieldName = fieldName.Substring(2);
            }

            return ObjectNames.NicifyVariableName(fieldName);
        }
    }
}
