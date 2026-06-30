using System;
using System.Collections.Generic;

namespace EditorRpc
{
    [Serializable]
    public class EditorRpcMethodResult
    {
        public bool success;
        public string message;
        public string payloadJson;
    }

    public sealed class EditorRpcMethodDefinition
    {
        public EditorRpcMethodDefinition(string name, string category, string description, Dictionary<string, EditorRpcParameterDefinition> parameters)
        {
            Name = name;
            Category = category;
            Description = description;
            Parameters = parameters ?? new Dictionary<string, EditorRpcParameterDefinition>();
        }

        public string Name { get; private set; }

        public string Category { get; private set; }

        public string Description { get; private set; }

        public Dictionary<string, EditorRpcParameterDefinition> Parameters { get; private set; }
    }

    public sealed class EditorRpcParameterDefinition
    {
        public EditorRpcParameterDefinition(string type, string description, bool required, List<string> enumValues = null)
        {
            Type = type;
            Description = description;
            Required = required;
            EnumValues = enumValues;
        }

        public string Type { get; private set; }

        public string Description { get; private set; }

        public bool Required { get; private set; }

        public List<string> EnumValues { get; private set; }
    }

    [Serializable]
    public class EditorRpcMethodListPayload
    {
        public int returnedCount;
        public EditorRpcMethodInfo[] methods;
    }

    [Serializable]
    public class EditorRpcMethodInfo
    {
        public string name;
        public string category;
        public string description;
        public EditorRpcParameterInfo[] parameters;
    }

    [Serializable]
    public class EditorRpcParameterInfo
    {
        public string name;
        public string type;
        public string description;
        public bool required;
        public string[] enumValues;
    }

    [Serializable]
    public class EditorStatePayload
    {
        public string projectPath;
        public string activeScenePath;
        public bool activeSceneDirty;
        public bool isPlaying;
        public bool isPaused;
        public bool isCompiling;
        public bool isUpdating;
        public string[] selection;
    }

    [Serializable]
    public class AssetSearchPayload
    {
        public string filter;
        public string[] folders;
        public int totalCount;
        public int returnedCount;
        public AssetInfo[] assets;
    }

    [Serializable]
    public class AssetInfo
    {
        public string guid;
        public string path;
        public string typeName;
    }

    [Serializable]
    public class AssetSubObjectInfo
    {
        public string name;
        public string typeName;
        public string reference;
        public bool isMainAsset;
    }

    [Serializable]
    public class AssetSubObjectListPayload
    {
        public string assetPath;
        public string typeFilter;
        public int totalCount;
        public int returnedCount;
        public AssetSubObjectInfo[] objects;
    }

    [Serializable]
    public class AssetPathPayload
    {
        public string path;
    }

    [Serializable]
    public class PrefabObjectPayload
    {
        public string assetPath;
        public string objectPath;
        public string name;
        public bool created;
        public string[] componentTypes;
    }

    [Serializable]
    public class RemovedComponentPayload
    {
        public string assetPath;
        public string objectPath;
        public string componentType;
        public int componentIndex;
        public string[] remainingComponentTypes;
    }

    [Serializable]
    public class MaterialAssignmentPayload
    {
        public string targetKind;
        public string materialPath;
        public bool dryRun;
        public bool includeInactive;
        public bool saveOpenScenes;
        public string slotMode;
        public int slotIndex;
        public int inputTargetCount;
        public int resolvedTargetCount;
        public int scannedRendererCount;
        public int matchedRendererCount;
        public int changedRendererCount;
        public int changedSlotCount;
        public int changedTargetCount;
        public int skippedTargetCount;
        public string[] changedTargets;
        public string[] messages;
    }

    [Serializable]
    public class SelectionPayload
    {
        public string[] selection;
    }

    [Serializable]
    public class ScenePayload
    {
        public string scenePath;
        public string sceneName;
        public bool isDirty;
    }

    [Serializable]
    public class LoadedScenesPayload
    {
        public int returnedCount;
        public SceneInfo[] scenes;
    }

    [Serializable]
    public class SceneInfo
    {
        public string scenePath;
        public string sceneName;
        public bool isDirty;
        public bool isLoaded;
        public bool isActive;
    }

    [Serializable]
    public class SavePayload
    {
        public bool saved;
    }

    [Serializable]
    public class HierarchyPayload
    {
        public string scenePath;
        public int returnedCount;
        public HierarchyNodeInfo[] nodes;
    }

    [Serializable]
    public class HierarchyNodeInfo
    {
        public string path;
        public string name;
        public int depth;
        public bool activeSelf;
        public string[] componentTypes;
    }

    [Serializable]
    public class GameObjectSearchPayload
    {
        public string nameContains;
        public int returnedCount;
        public GameObjectInfo[] objects;
    }

    [Serializable]
    public class GameObjectInfo
    {
        public string path;
        public string name;
        public string scenePath;
        public bool activeSelf;
    }

    [Serializable]
    public class GameObjectPayload
    {
        public string path;
        public string name;
        public string scenePath;
        public bool activeSelf;
        public string position;
        public string localPosition;
        public string localRotationEuler;
        public string localScale;
    }

    [Serializable]
    public class ComponentPayload
    {
        public string gameObjectPath;
        public string componentType;
    }

    [Serializable]
    public class SerializedTargetPayload
    {
        public string targetPath;
        public string targetType;
        public string[] componentTypes;
        public int totalPropertyCount;
        public int returnedPropertyCount;
        public SerializedPropertyInfo[] properties;
    }

    [Serializable]
    public class SerializedPropertyInfo
    {
        public string propertyPath;
        public string displayName;
        public string propertyType;
        public bool isArray;
        public bool editable;
        public string valuePreview;
    }

    [Serializable]
    public class PropertySetPayload
    {
        public string targetPath;
        public string targetType;
        public string propertyPath;
        public string propertyType;
        public string valuePreview;
    }

    [Serializable]
    public class UiRootsPayload
    {
        public string scenePath;
        public int returnedCount;
        public UiRootInfo[] roots;
    }

    [Serializable]
    public class UiRootInfo
    {
        public string path;
        public string name;
        public string scenePath;
        public string kind;
        public bool activeSelf;
        public bool isCanvasRoot;
        public bool hasGraphicRaycaster;
        public string renderMode;
        public int directChildCount;
        public string[] componentTypes;
    }

    [Serializable]
    public class UiNodePayload
    {
        public string path;
        public string name;
        public string scenePath;
        public bool activeSelf;
        public string[] componentTypes;
        public string anchorMin;
        public string anchorMax;
        public string anchoredPosition;
        public string sizeDelta;
        public string pivot;
    }

    [Serializable]
    public class UiSubtreePayload
    {
        public string rootPath;
        public int returnedCount;
        public UiTreeNodeInfo[] nodes;
    }

    [Serializable]
    public class UiTreeNodeInfo
    {
        public string path;
        public string name;
        public int depth;
        public int childCount;
        public bool activeSelf;
        public string[] componentTypes;
        public string anchorMin;
        public string anchorMax;
        public string anchoredPosition;
        public string sizeDelta;
        public string pivot;
        public string graphicType;
        public string textPreview;
        public string selectableType;
        public string layoutType;
    }

    [Serializable]
    public class UiControllerPayload
    {
        public string path;
        public string componentType;
        public string scenePath;
        public bool activeSelf;
        public int bindingCount;
        public UiBindingInfo[] bindings;
    }

    [Serializable]
    public class UiBindingInfo
    {
        public string fieldName;
        public string memberType;
        public string valueType;
        public string target;
        public string targetPath;
        public bool isSceneReference;
        public bool isUiRelated;
    }

    [Serializable]
    public class UiEventBindingsPayload
    {
        public string path;
        public int returnedCount;
        public UiEventBindingInfo[] events;
    }

    [Serializable]
    public class UiEventBindingInfo
    {
        public string ownerPath;
        public string componentType;
        public string eventName;
        public int listenerCount;
        public UiEventListenerInfo[] listeners;
    }

    [Serializable]
    public class UiEventListenerInfo
    {
        public string target;
        public string methodName;
        public string mode;
        public string callState;
    }

    [Serializable]
    public class UiPrefabReferencePayload
    {
        public string path;
        public string componentType;
        public int returnedCount;
        public UiBindingInfo[] references;
    }

    [Serializable]
    public class UiScrollViewPayload
    {
        public string path;
        public string viewportPath;
        public string contentPath;
        public string horizontalScrollbarPath;
        public string verticalScrollbarPath;
    }

    [Serializable]
    public class UiTextPayload
    {
        public string path;
        public string componentType;
        public string text;
        public int fontSize;
        public string color;
    }

    [Serializable]
    public class UiGraphicPayload
    {
        public string path;
        public string componentType;
        public string color;
        public string spritePath;
        public bool raycastTarget;
    }

    [Serializable]
    public class UiSelectablePayload
    {
        public string path;
        public string componentType;
        public bool interactable;
        public float alpha;
        public bool blocksRaycasts;
    }

    [Serializable]
    public class UiLayoutPayload
    {
        public string path;
        public string layoutType;
        public string spacing;
        public string padding;
        public string childAlignment;
    }

    [Serializable]
    public class UiBatchOperationsPayload
    {
        public int requestedCount;
        public int succeededCount;
        public int failedCount;
        public UiBatchOperationResult[] results;
    }

    [Serializable]
    public class UiBatchOperationResult
    {
        public int index;
        public string method;
        public bool success;
        public string message;
        public string payloadJson;
    }

    [Serializable]
    public class MenuPayload
    {
        public string menuPath;
    }

    [Serializable]
    public class EditorMethodPayload
    {
        public string typeName;
        public string methodName;
    }

    [Serializable]
    public class MethodInvocationPayload
    {
        public string targetPath;
        public string targetType;
        public string methodName;
        public int argumentCount;
        public string returnType;
        public string returnValuePreview;
    }

    [Serializable]
    public class ConsolePayload
    {
        public int returnedCount;
        public ConsoleEntryInfo[] entries;
    }

    [Serializable]
    public class ConsoleEntryInfo
    {
        public int index;
        public string message;
        public int mode;
        public string file;
        public int line;
    }

    [Serializable]
    public class BatchOperationsPayload
    {
        public int requestedCount;
        public int succeededCount;
        public int failedCount;
        public BatchOperationResult[] results;
    }

    [Serializable]
    public class BatchOperationResult
    {
        public int index;
        public string method;
        public bool success;
        public string message;
        public string payloadJson;
    }

    [Serializable]
    public class ValidationPayload
    {
        public EditorStatePayload editorState;
        public int consoleCount;
        public int errorCount;
        public int warningCount;
        public ConsoleEntryInfo[] consoleEntries;
        public LoadedScenesPayload loadedScenes;
        public HierarchyPayload hierarchy;
    }

    [Serializable]
    public class TypeSearchPayload
    {
        public string query;
        public string assignableTo;
        public int returnedCount;
        public TypeSearchInfo[] types;
    }

    [Serializable]
    public class TypeSearchInfo
    {
        public string assemblyName;
        public string fullName;
        public string name;
        public bool isAbstract;
        public bool isEnum;
        public bool isValueType;
        public bool isUnityObject;
        public bool isComponent;
    }

    [Serializable]
    public class MethodSearchPayload
    {
        public string typeName;
        public string methodNameFilter;
        public bool includeInherited;
        public int returnedCount;
        public MethodSearchInfo[] methods;
    }

    [Serializable]
    public class MethodSearchInfo
    {
        public string declaringType;
        public string methodName;
        public bool isStatic;
        public bool isPublic;
        public bool isGeneric;
        public string returnType;
        public int requiredParameterCount;
        public string[] parameters;
    }

    [Serializable]
    public class MenuSearchPayload
    {
        public string query;
        public int returnedCount;
        public string[] menuItems;
    }

    [Serializable]
    public class ComponentSearchPayload
    {
        public string path;
        public string assetPath;
        public int returnedCount;
        public ComponentSearchInfo[] components;
    }

    [Serializable]
    public class ComponentSearchInfo
    {
        public int index;
        public string typeName;
        public string fullTypeName;
        public string targetPath;
        public int totalPropertyCount;
        public int returnedPropertyCount;
        public SerializedPropertyInfo[] properties;
    }

    [Serializable]
    public class SceneRenderingAnalysisPayload
    {
        public string path;
        public string scenePath;
        public bool activeSelf;
        public bool activeInHierarchy;
        public bool includeInactive;
        public int directChildCount;
        public SceneRenderingTotals totals;
        public SceneRenderingChildSummary[] topChildrenByRendererCount;
        public SceneRenderingChildSummary[] topChildrenByTriangleCount;
        public SceneRenderingCounterInfo[] topMaterials;
        public SceneRenderingCounterInfo[] topMeshes;
        public SceneRenderingCounterInfo[] topMeshColliders;
        public SceneRenderingNameCount[] topComponentTypes;
    }

    [Serializable]
    public class SceneRenderingTotals
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
        public int uniqueMaterialCount;
        public int instancingMaterialCount;
        public int nonInstancingMaterialCount;
        public int shadowCastingRendererCount;
        public int shadowReceivingRendererCount;
        public int rendererMeshInstanceCount;
        public int uniqueMeshCount;
        public long rendererTriangleCount;
        public long rendererVertexCount;
        public int colliderCount;
        public int meshColliderCount;
        public long meshColliderTriangleCount;
        public int uniqueMeshColliderMeshCount;
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
        public string activeRendererBoundsCenter;
        public string activeRendererBoundsSize;
    }

    [Serializable]
    public class SceneRenderingChildSummary
    {
        public string path;
        public string name;
        public int childIndex;
        public bool activeSelf;
        public bool activeInHierarchy;
        public int gameObjectCount;
        public int rendererCount;
        public int activeEnabledRendererCount;
        public int materialSlotCount;
        public int uniqueMaterialCount;
        public long rendererTriangleCount;
        public long rendererVertexCount;
        public int colliderCount;
        public int meshColliderCount;
        public long meshColliderTriangleCount;
        public int lodGroupCount;
        public int lightCount;
        public int activeEnabledLightCount;
        public int realtimeLightCount;
        public int shadowCastingLightCount;
        public int particleSystemCount;
        public int monoBehaviourCount;
        public int maxDepth;
    }

    [Serializable]
    public class SceneRenderingCounterInfo
    {
        public string name;
        public string path;
        public int count;
        public bool instancingEnabled;
        public long triangleCount;
        public long vertexCount;
    }

    [Serializable]
    public class SceneRenderingNameCount
    {
        public string name;
        public int count;
    }

    [Serializable]
    public class AnimatorControllerPayload
    {
        public string assetPath;
        public string controllerName;
        public AnimatorParameterInfo[] parameters;
        public AnimatorLayerInfo[] layers;
    }

    [Serializable]
    public class AnimatorParameterInfo
    {
        public string name;
        public string type;
        public string defaultValue;
    }

    [Serializable]
    public class AnimatorLayerInfo
    {
        public string name;
        public string defaultStateName;
        public AnimatorStateInfo[] states;
        public AnimatorStateMachineInfo rootStateMachine;
    }

    [Serializable]
    public class AnimatorStateInfo
    {
        public string stateMachinePath;
        public string name;
        public string motionPath;
        public string position;
        public string tag;
        public float speed;
        public bool writeDefaultValues;
        public bool mirror;
        public bool isDefaultState;
        public AnimatorMotionInfo motion;
        public AnimatorTransitionInfo[] transitions;
    }

    [Serializable]
    public class AnimatorTransitionInfo
    {
        public string sourceStateName;
        public bool fromAnyState;
        public string destinationStateName;
        public bool hasExitTime;
        public float exitTime;
        public float duration;
        public float offset;
        public bool hasFixedDuration;
        public bool mute;
        public bool solo;
        public bool canTransitionToSelf;
        public string interruptionSource;
        public bool orderedInterruption;
        public AnimatorConditionInfo[] conditions;
    }

    [Serializable]
    public class AnimatorStateMachineInfo
    {
        public string name;
        public string path;
        public string defaultStateName;
        public AnimatorStateInfo[] states;
        public AnimatorTransitionInfo[] anyStateTransitions;
        public AnimatorStateMachineInfo[] childStateMachines;
    }

    [Serializable]
    public class AnimatorMotionInfo
    {
        public string name;
        public string motionType;
        public string assetPath;
        public bool isSubAsset;
        public string blendType;
        public string blendParameter;
        public string blendParameterY;
        public bool useAutomaticThresholds;
        public AnimatorBlendTreeChildInfo[] children;
    }

    [Serializable]
    public class AnimatorBlendTreeChildInfo
    {
        public string motionName;
        public string motionType;
        public string motionAssetPath;
        public bool isSubAsset;
        public float threshold;
        public string position;
        public float timeScale;
        public float cycleOffset;
        public bool mirror;
        public string directBlendParameter;
    }

    [Serializable]
    public class AnimatorConditionInfo
    {
        public string mode;
        public string parameter;
        public float threshold;
    }

    [Serializable]
    public class AnimatorParameterPayload
    {
        public string assetPath;
        public string parameterName;
        public string parameterType;
    }

    [Serializable]
    public class AnimatorStatePayload
    {
        public string assetPath;
        public string layerName;
        public string stateMachinePath;
        public string stateName;
    }

    [Serializable]
    public class AnimatorTransitionPayload
    {
        public string assetPath;
        public string layerName;
        public string stateMachinePath;
        public string fromStateName;
        public string toStateName;
        public bool fromAnyState;
    }
}
