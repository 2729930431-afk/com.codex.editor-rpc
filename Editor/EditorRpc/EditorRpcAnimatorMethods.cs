using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace EditorRpc
{
    public static partial class EditorRpcMethodExecutor
    {
        private struct AnimatorConditionDescriptor
        {
            public AnimatorConditionMode mode;
            public string parameter;
            public float threshold;
        }

        private static EditorRpcMethodResult ExecuteInspectAnimatorController(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var assetPath = GetRequiredString(args, "asset_path");
            AnimatorController controller;
            string error;
            if (!TryLoadAnimatorController(assetPath, out controller, out error))
            {
                return Failure(error);
            }

            return Success("AnimatorController inspected.", BuildAnimatorControllerPayload(controller, assetPath));
        }

        private static EditorRpcMethodResult ExecuteAddAnimatorParameter(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var assetPath = GetRequiredString(args, "asset_path");
            var name = GetRequiredString(args, "name");
            var typeRaw = GetRequiredString(args, "type");
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(typeRaw))
            {
                return Failure("add_animator_parameter requires asset_path, name, and type.");
            }

            AnimatorController controller;
            string error;
            if (!TryLoadAnimatorController(assetPath, out controller, out error))
            {
                return Failure(error);
            }

            for (int i = 0; i < controller.parameters.Length; i++)
            {
                if (string.Equals(controller.parameters[i].name, name, StringComparison.Ordinal))
                {
                    return Failure("Animator parameter already exists: " + name);
                }
            }

            AnimatorControllerParameterType parameterType;
            if (!TryParseAnimatorParameterType(typeRaw, out parameterType))
            {
                return Failure("Unsupported animator parameter type: " + typeRaw);
            }

            var parameter = new AnimatorControllerParameter
            {
                name = name,
                type = parameterType
            };

            var defaultValue = GetString(args, "default_value", string.Empty);
            if (!string.IsNullOrEmpty(defaultValue))
            {
                ApplyAnimatorParameterDefault(parameter, defaultValue);
            }

            controller.AddParameter(parameter);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return Success("Animator parameter added.", new AnimatorParameterPayload
            {
                assetPath = assetPath,
                parameterName = name,
                parameterType = parameter.type.ToString()
            });
        }

        private static EditorRpcMethodResult ExecuteAddAnimatorState(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var assetPath = GetRequiredString(args, "asset_path");
            var stateName = GetRequiredString(args, "state_name");
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(stateName))
            {
                return Failure("add_animator_state requires asset_path and state_name.");
            }

            AnimatorController controller;
            string error;
            if (!TryLoadAnimatorController(assetPath, out controller, out error))
            {
                return Failure(error);
            }

            AnimatorStateMachine stateMachine;
            AnimatorControllerLayer layer;
            if (!TryResolveAnimatorStateMachine(controller, GetString(args, "layer_name", string.Empty), GetString(args, "state_machine_path", string.Empty), out layer, out stateMachine, out error))
            {
                return Failure(error);
            }

            AnimatorState existingState;
            if (TryFindAnimatorState(stateMachine, stateName, out existingState))
            {
                return Failure("Animator state already exists: " + stateName);
            }

            Motion motion = null;
            var motionAssetPath = GetString(args, "motion_asset_path", string.Empty);
            var motionName = GetString(args, "motion_name", string.Empty);
            if (!string.IsNullOrEmpty(motionAssetPath))
            {
                if (!TryLoadMotionAsset(motionAssetPath, motionName, out motion, out error))
                {
                    return Failure(error);
                }
            }

            Vector2 position2;
            var position = TryGetVector2(args, "position", out position2) ? new Vector3(position2.x, position2.y, 0f) : new Vector3(300f, 80f, 0f);
            var state = stateMachine.AddState(stateName, position);
            state.motion = motion;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return Success("Animator state added.", new AnimatorStatePayload
            {
                assetPath = assetPath,
                layerName = layer.name,
                stateMachinePath = GetString(args, "state_machine_path", string.Empty),
                stateName = state.name
            });
        }

        private static EditorRpcMethodResult ExecuteSetAnimatorDefaultState(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var assetPath = GetRequiredString(args, "asset_path");
            var stateName = GetRequiredString(args, "state_name");
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(stateName))
            {
                return Failure("set_animator_default_state requires asset_path and state_name.");
            }

            AnimatorController controller;
            string error;
            if (!TryLoadAnimatorController(assetPath, out controller, out error))
            {
                return Failure(error);
            }

            AnimatorStateMachine stateMachine;
            AnimatorControllerLayer layer;
            if (!TryResolveAnimatorStateMachine(controller, GetString(args, "layer_name", string.Empty), GetString(args, "state_machine_path", string.Empty), out layer, out stateMachine, out error))
            {
                return Failure(error);
            }

            AnimatorState state;
            if (!TryFindAnimatorState(stateMachine, stateName, out state))
            {
                return Failure("Animator state not found: " + stateName);
            }

            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return Success("Animator default state updated.", new AnimatorStatePayload
            {
                assetPath = assetPath,
                layerName = layer.name,
                stateMachinePath = GetString(args, "state_machine_path", string.Empty),
                stateName = state.name
            });
        }

        private static EditorRpcMethodResult ExecuteSetAnimatorStateMotion(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var assetPath = GetRequiredString(args, "asset_path");
            var stateName = GetRequiredString(args, "state_name");
            var motionAssetPath = GetRequiredString(args, "motion_asset_path");
            var motionName = GetString(args, "motion_name", string.Empty);
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(stateName) || string.IsNullOrEmpty(motionAssetPath))
            {
                return Failure("set_animator_state_motion requires asset_path, state_name, and motion_asset_path.");
            }

            AnimatorController controller;
            string error;
            if (!TryLoadAnimatorController(assetPath, out controller, out error))
            {
                return Failure(error);
            }

            AnimatorStateMachine stateMachine;
            AnimatorControllerLayer layer;
            if (!TryResolveAnimatorStateMachine(controller, GetString(args, "layer_name", string.Empty), GetString(args, "state_machine_path", string.Empty), out layer, out stateMachine, out error))
            {
                return Failure(error);
            }

            AnimatorState state;
            if (!TryFindAnimatorState(stateMachine, stateName, out state))
            {
                return Failure("Animator state not found: " + stateName);
            }

            Motion motion;
            if (!TryLoadMotionAsset(motionAssetPath, motionName, out motion, out error))
            {
                return Failure(error);
            }

            state.motion = motion;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return Success("Animator state motion updated.", new AnimatorStatePayload
            {
                assetPath = assetPath,
                layerName = layer.name,
                stateMachinePath = GetString(args, "state_machine_path", string.Empty),
                stateName = state.name
            });
        }

        private static EditorRpcMethodResult ExecuteAddAnimatorTransition(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var assetPath = GetRequiredString(args, "asset_path");
            var toStateName = GetRequiredString(args, "to_state");
            var fromAnyState = GetBool(args, "from_any_state", false);
            var fromStateName = fromAnyState ? string.Empty : GetRequiredString(args, "from_state");
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(toStateName) || (!fromAnyState && string.IsNullOrEmpty(fromStateName)))
            {
                return Failure("add_animator_transition requires asset_path, to_state, and either from_state or from_any_state.");
            }

            AnimatorController controller;
            string error;
            if (!TryLoadAnimatorController(assetPath, out controller, out error))
            {
                return Failure(error);
            }

            AnimatorStateMachine stateMachine;
            AnimatorControllerLayer layer;
            if (!TryResolveAnimatorStateMachine(controller, GetString(args, "layer_name", string.Empty), GetString(args, "state_machine_path", string.Empty), out layer, out stateMachine, out error))
            {
                return Failure(error);
            }

            AnimatorState toState;
            if (!TryFindAnimatorState(stateMachine, toStateName, out toState))
            {
                return Failure("Destination animator state not found: " + toStateName);
            }

            AnimatorStateTransition transition;
            if (fromAnyState)
            {
                transition = stateMachine.AddAnyStateTransition(toState);
            }
            else
            {
                AnimatorState fromState;
                if (!TryFindAnimatorState(stateMachine, fromStateName, out fromState))
                {
                    return Failure("Source animator state not found: " + fromStateName);
                }

                transition = fromState.AddTransition(toState);
            }

            transition.hasExitTime = GetBool(args, "has_exit_time", false);
            transition.exitTime = GetFloat(args, "exit_time", 0f);
            transition.duration = GetFloat(args, "duration", 0.1f);

            float transitionOffset;
            if (TryGetOptionalFloat(args, "offset", out transitionOffset))
            {
                transition.offset = transitionOffset;
            }

            bool fixedDuration;
            if (TryGetOptionalBool(args, "fixed_duration", out fixedDuration))
            {
                transition.hasFixedDuration = fixedDuration;
            }

            bool mute;
            if (TryGetOptionalBool(args, "mute", out mute))
            {
                transition.mute = mute;
            }

            bool solo;
            if (TryGetOptionalBool(args, "solo", out solo))
            {
                transition.solo = solo;
            }

            bool canTransitionToSelf;
            if (TryGetOptionalBool(args, "can_transition_to_self", out canTransitionToSelf))
            {
                transition.canTransitionToSelf = canTransitionToSelf;
            }

            bool orderedInterruption;
            if (TryGetOptionalBool(args, "ordered_interruption", out orderedInterruption))
            {
                transition.orderedInterruption = orderedInterruption;
            }

            string interruptionSourceRaw;
            if (TryGetOptionalString(args, "interruption_source", out interruptionSourceRaw))
            {
                TransitionInterruptionSource interruptionSource;
                if (!TryParseAnimatorInterruptionSource(interruptionSourceRaw, out interruptionSource))
                {
                    return Failure("Unsupported animator interruption source: " + interruptionSourceRaw);
                }

                transition.interruptionSource = interruptionSource;
            }

            List<AnimatorConditionDescriptor> conditions;
            if (!TryCollectAnimatorTransitionConditions(args, controller, out conditions, out error))
            {
                return Failure(error);
            }

            for (int i = 0; i < conditions.Count; i++)
            {
                transition.AddCondition(conditions[i].mode, conditions[i].threshold, conditions[i].parameter);
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return Success("Animator transition added.", new AnimatorTransitionPayload
            {
                assetPath = assetPath,
                layerName = layer.name,
                stateMachinePath = GetString(args, "state_machine_path", string.Empty),
                fromStateName = fromAnyState ? string.Empty : fromStateName,
                toStateName = toState.name,
                fromAnyState = fromAnyState
            });
        }

        private static EditorRpcMethodResult ExecuteSetAnimatorTransitionProperties(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var assetPath = GetRequiredString(args, "asset_path");
            if (string.IsNullOrEmpty(assetPath))
            {
                return Failure("set_animator_transition_properties requires asset_path.");
            }

            AnimatorController controller;
            string error;
            if (!TryLoadAnimatorController(assetPath, out controller, out error))
            {
                return Failure(error);
            }

            AnimatorControllerLayer layer;
            AnimatorStateMachine stateMachine;
            AnimatorStateTransition transition;
            string fromStateName;
            string toStateName;
            bool fromAnyState;
            if (!TryResolveAnimatorTransition(args, controller, out layer, out stateMachine, out transition, out fromStateName, out toStateName, out fromAnyState, out error))
            {
                return Failure(error);
            }

            bool updated;
            if (!TryApplyAnimatorTransitionProperties(args, controller, transition, out updated, out error))
            {
                return Failure(error);
            }

            if (!updated)
            {
                return Failure("No transition properties were supplied to update.");
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return Success("Animator transition updated.", new AnimatorTransitionPayload
            {
                assetPath = assetPath,
                layerName = layer.name,
                stateMachinePath = GetString(args, "state_machine_path", string.Empty),
                fromStateName = fromStateName,
                toStateName = toStateName,
                fromAnyState = fromAnyState
            });
        }

        private static EditorRpcMethodResult ExecuteRemoveAnimatorTransition(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var assetPath = GetRequiredString(args, "asset_path");
            if (string.IsNullOrEmpty(assetPath))
            {
                return Failure("remove_animator_transition requires asset_path.");
            }

            AnimatorController controller;
            string error;
            if (!TryLoadAnimatorController(assetPath, out controller, out error))
            {
                return Failure(error);
            }

            AnimatorControllerLayer layer;
            AnimatorStateMachine stateMachine;
            AnimatorStateTransition transition;
            string fromStateName;
            string toStateName;
            bool fromAnyState;
            if (!TryResolveAnimatorTransition(args, controller, out layer, out stateMachine, out transition, out fromStateName, out toStateName, out fromAnyState, out error))
            {
                return Failure(error);
            }

            if (fromAnyState)
            {
                stateMachine.RemoveAnyStateTransition(transition);
            }
            else
            {
                AnimatorState fromState;
                if (!TryFindAnimatorState(stateMachine, fromStateName, out fromState))
                {
                    return Failure("Source animator state not found: " + fromStateName);
                }

                fromState.RemoveTransition(transition);
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return Success("Animator transition removed.", new AnimatorTransitionPayload
            {
                assetPath = assetPath,
                layerName = layer.name,
                stateMachinePath = GetString(args, "state_machine_path", string.Empty),
                fromStateName = fromStateName,
                toStateName = toStateName,
                fromAnyState = fromAnyState
            });
        }

        private static bool TryLoadAnimatorController(string assetPath, out AnimatorController controller, out string error)
        {
            controller = null;
            error = string.Empty;
            if (string.IsNullOrEmpty(assetPath))
            {
                error = "AnimatorController asset path is required.";
                return false;
            }

            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            if (controller == null)
            {
                error = "AnimatorController not found at path: " + assetPath;
                return false;
            }

            return true;
        }

        private static bool TryLoadMotionAsset(string motionAssetPath, string motionName, out Motion motion, out string error)
        {
            motion = null;
            error = string.Empty;
            if (string.IsNullOrEmpty(motionAssetPath))
            {
                error = "Motion asset path is required.";
                return false;
            }

            if (string.IsNullOrEmpty(motionName))
            {
                motion = AssetDatabase.LoadAssetAtPath<Motion>(motionAssetPath);
                if (motion == null)
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(motionAssetPath);
                    for (int i = 0; i < assets.Length; i++)
                    {
                        motion = assets[i] as Motion;
                        if (motion != null)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(motionAssetPath);
                for (int i = 0; i < assets.Length; i++)
                {
                    var candidateMotion = assets[i] as Motion;
                    if (candidateMotion != null && string.Equals(candidateMotion.name, motionName, StringComparison.Ordinal))
                    {
                        motion = candidateMotion;
                        break;
                    }
                }
            }

            if (motion == null)
            {
                error = string.IsNullOrEmpty(motionName)
                    ? "Motion asset not found: " + motionAssetPath
                    : "Motion asset not found at path/name: " + motionAssetPath + " / " + motionName;
                return false;
            }

            return true;
        }

        private static bool TryParseAnimatorParameterType(string rawValue, out AnimatorControllerParameterType parameterType)
        {
            return Enum.TryParse(rawValue, true, out parameterType);
        }

        private static bool TryParseAnimatorConditionMode(string rawValue, out AnimatorConditionMode conditionMode)
        {
            return Enum.TryParse(rawValue, true, out conditionMode);
        }

        private static bool TryParseAnimatorInterruptionSource(string rawValue, out TransitionInterruptionSource interruptionSource)
        {
            return Enum.TryParse(rawValue, true, out interruptionSource);
        }

        private static void ApplyAnimatorParameterDefault(AnimatorControllerParameter parameter, string defaultValue)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Bool:
                    parameter.defaultBool = string.Equals(defaultValue, "true", StringComparison.OrdinalIgnoreCase) || defaultValue == "1";
                    break;
                case AnimatorControllerParameterType.Float:
                    float defaultFloat;
                    if (float.TryParse(defaultValue, NumberStyles.Float, CultureInfo.InvariantCulture, out defaultFloat))
                    {
                        parameter.defaultFloat = defaultFloat;
                    }

                    break;
                case AnimatorControllerParameterType.Int:
                    int defaultInt;
                    if (int.TryParse(defaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out defaultInt))
                    {
                        parameter.defaultInt = defaultInt;
                    }

                    break;
            }
        }

        private static bool TryResolveAnimatorStateMachine(AnimatorController controller, string layerName, string stateMachinePath, out AnimatorControllerLayer layer, out AnimatorStateMachine stateMachine, out string error)
        {
            layer = null;
            stateMachine = null;
            error = string.Empty;
            if (controller == null)
            {
                error = "AnimatorController is null.";
                return false;
            }

            if (controller.layers == null || controller.layers.Length == 0)
            {
                error = "AnimatorController has no layers.";
                return false;
            }

            layer = controller.layers[0];
            if (!string.IsNullOrEmpty(layerName))
            {
                var foundLayer = false;
                for (int i = 0; i < controller.layers.Length; i++)
                {
                    if (string.Equals(controller.layers[i].name, layerName, StringComparison.Ordinal))
                    {
                        layer = controller.layers[i];
                        foundLayer = true;
                        break;
                    }
                }

                if (!foundLayer)
                {
                    error = "Animator layer not found: " + layerName;
                    return false;
                }
            }

            stateMachine = layer.stateMachine;
            if (stateMachine == null)
            {
                error = "Animator layer has no state machine: " + layer.name;
                return false;
            }

            if (string.IsNullOrEmpty(stateMachinePath))
            {
                return true;
            }

            var parts = stateMachinePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int partIndex = 0; partIndex < parts.Length; partIndex++)
            {
                var matched = false;
                for (int stateMachineIndex = 0; stateMachineIndex < stateMachine.stateMachines.Length; stateMachineIndex++)
                {
                    var childStateMachine = stateMachine.stateMachines[stateMachineIndex].stateMachine;
                    if (childStateMachine != null && string.Equals(childStateMachine.name, parts[partIndex], StringComparison.Ordinal))
                    {
                        stateMachine = childStateMachine;
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    error = "Animator state machine path not found: " + stateMachinePath;
                    return false;
                }
            }

            return true;
        }

        private static bool TryFindAnimatorState(AnimatorStateMachine stateMachine, string stateName, out AnimatorState state)
        {
            state = null;
            if (stateMachine == null || string.IsNullOrEmpty(stateName))
            {
                return false;
            }

            for (int i = 0; i < stateMachine.states.Length; i++)
            {
                if (stateMachine.states[i].state != null && string.Equals(stateMachine.states[i].state.name, stateName, StringComparison.Ordinal))
                {
                    state = stateMachine.states[i].state;
                    return true;
                }
            }

            return false;
        }

        private static bool HasAnimatorParameter(AnimatorController controller, string parameterName)
        {
            if (controller == null || string.IsNullOrEmpty(parameterName))
            {
                return false;
            }

            for (int i = 0; i < controller.parameters.Length; i++)
            {
                if (string.Equals(controller.parameters[i].name, parameterName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static AnimatorControllerPayload BuildAnimatorControllerPayload(AnimatorController controller, string assetPath)
        {
            var parameterInfos = new List<AnimatorParameterInfo>();
            for (int parameterIndex = 0; parameterIndex < controller.parameters.Length; parameterIndex++)
            {
                var parameter = controller.parameters[parameterIndex];
                parameterInfos.Add(new AnimatorParameterInfo
                {
                    name = parameter.name,
                    type = parameter.type.ToString(),
                    defaultValue = GetAnimatorParameterDefaultValue(parameter)
                });
            }

            var layers = new List<AnimatorLayerInfo>();
            for (int layerIndex = 0; layerIndex < controller.layers.Length; layerIndex++)
            {
                var layer = controller.layers[layerIndex];
                var states = new List<AnimatorStateInfo>();
                CollectAnimatorStateInfos(layer.stateMachine, string.Empty, states);
                var rootStateMachine = BuildAnimatorStateMachineInfo(layer.stateMachine, string.Empty);

                layers.Add(new AnimatorLayerInfo
                {
                    name = layer.name,
                    defaultStateName = layer.stateMachine != null && layer.stateMachine.defaultState != null ? layer.stateMachine.defaultState.name : string.Empty,
                    states = states.ToArray(),
                    rootStateMachine = rootStateMachine
                });
            }

            return new AnimatorControllerPayload
            {
                assetPath = assetPath,
                controllerName = controller.name,
                parameters = parameterInfos.ToArray(),
                layers = layers.ToArray()
            };
        }

        private static AnimatorStateMachineInfo BuildAnimatorStateMachineInfo(AnimatorStateMachine stateMachine, string stateMachinePath)
        {
            if (stateMachine == null)
            {
                return null;
            }

            var states = new List<AnimatorStateInfo>();
            for (int stateIndex = 0; stateIndex < stateMachine.states.Length; stateIndex++)
            {
                states.Add(BuildAnimatorStateInfo(stateMachine.states[stateIndex], stateMachine, stateMachinePath));
            }

            var childStateMachines = new List<AnimatorStateMachineInfo>();
            for (int childIndex = 0; childIndex < stateMachine.stateMachines.Length; childIndex++)
            {
                var child = stateMachine.stateMachines[childIndex].stateMachine;
                if (child == null)
                {
                    continue;
                }

                var childPath = string.IsNullOrEmpty(stateMachinePath) ? child.name : stateMachinePath + "/" + child.name;
                childStateMachines.Add(BuildAnimatorStateMachineInfo(child, childPath));
            }

            return new AnimatorStateMachineInfo
            {
                name = stateMachine.name,
                path = stateMachinePath,
                defaultStateName = stateMachine.defaultState != null ? stateMachine.defaultState.name : string.Empty,
                states = states.ToArray(),
                anyStateTransitions = BuildAnimatorTransitionInfos(stateMachine.anyStateTransitions, string.Empty, true),
                childStateMachines = childStateMachines.ToArray()
            };
        }

        private static void CollectAnimatorStateInfos(AnimatorStateMachine stateMachine, string stateMachinePath, List<AnimatorStateInfo> states)
        {
            if (stateMachine == null)
            {
                return;
            }

            for (int stateIndex = 0; stateIndex < stateMachine.states.Length; stateIndex++)
            {
                states.Add(BuildAnimatorStateInfo(stateMachine.states[stateIndex], stateMachine, stateMachinePath));
            }

            for (int childIndex = 0; childIndex < stateMachine.stateMachines.Length; childIndex++)
            {
                var child = stateMachine.stateMachines[childIndex].stateMachine;
                if (child == null)
                {
                    continue;
                }

                var childPath = string.IsNullOrEmpty(stateMachinePath) ? child.name : stateMachinePath + "/" + child.name;
                CollectAnimatorStateInfos(child, childPath, states);
            }
        }

        private static AnimatorStateInfo BuildAnimatorStateInfo(ChildAnimatorState childState, AnimatorStateMachine ownerStateMachine, string stateMachinePath)
        {
            var animatorState = childState.state;
            if (animatorState == null)
            {
                return new AnimatorStateInfo
                {
                    stateMachinePath = stateMachinePath,
                    name = string.Empty,
                    motionPath = string.Empty,
                    position = FormatVector2(childState.position),
                    tag = string.Empty,
                    speed = 1f,
                    writeDefaultValues = true,
                    mirror = false,
                    isDefaultState = false,
                    motion = null,
                    transitions = new AnimatorTransitionInfo[0]
                };
            }

            var motionInfo = BuildAnimatorMotionInfo(animatorState.motion);
            return new AnimatorStateInfo
            {
                stateMachinePath = stateMachinePath,
                name = animatorState.name,
                motionPath = motionInfo != null && !string.IsNullOrEmpty(motionInfo.assetPath) ? motionInfo.assetPath : GetMotionPath(animatorState.motion),
                position = FormatVector2(childState.position),
                tag = animatorState.tag,
                speed = animatorState.speed,
                writeDefaultValues = animatorState.writeDefaultValues,
                mirror = animatorState.mirror,
                isDefaultState = ownerStateMachine != null && ownerStateMachine.defaultState == animatorState,
                motion = motionInfo,
                transitions = BuildAnimatorTransitionInfos(animatorState.transitions, animatorState.name, false)
            };
        }

        private static AnimatorTransitionInfo[] BuildAnimatorTransitionInfos(AnimatorStateTransition[] transitions, string sourceStateName, bool fromAnyState)
        {
            if (transitions == null || transitions.Length == 0)
            {
                return new AnimatorTransitionInfo[0];
            }

            var results = new AnimatorTransitionInfo[transitions.Length];
            for (int i = 0; i < transitions.Length; i++)
            {
                results[i] = BuildAnimatorTransitionInfo(transitions[i], sourceStateName, fromAnyState);
            }

            return results;
        }

        private static AnimatorTransitionInfo BuildAnimatorTransitionInfo(AnimatorStateTransition transition, string sourceStateName, bool fromAnyState)
        {
            if (transition == null)
            {
                return new AnimatorTransitionInfo
                {
                    sourceStateName = sourceStateName,
                    fromAnyState = fromAnyState,
                    destinationStateName = string.Empty,
                    hasExitTime = false,
                    exitTime = 0f,
                    duration = 0f,
                    offset = 0f,
                    hasFixedDuration = true,
                    mute = false,
                    solo = false,
                    canTransitionToSelf = false,
                    interruptionSource = string.Empty,
                    orderedInterruption = false,
                    conditions = new AnimatorConditionInfo[0]
                };
            }

            return new AnimatorTransitionInfo
            {
                sourceStateName = sourceStateName,
                fromAnyState = fromAnyState,
                destinationStateName = transition.destinationState != null ? transition.destinationState.name : string.Empty,
                hasExitTime = transition.hasExitTime,
                exitTime = transition.exitTime,
                duration = transition.duration,
                offset = transition.offset,
                hasFixedDuration = transition.hasFixedDuration,
                mute = transition.mute,
                solo = transition.solo,
                canTransitionToSelf = transition.canTransitionToSelf,
                interruptionSource = transition.interruptionSource.ToString(),
                orderedInterruption = transition.orderedInterruption,
                conditions = BuildAnimatorConditionInfos(transition.conditions)
            };
        }

        private static AnimatorConditionInfo[] BuildAnimatorConditionInfos(AnimatorCondition[] conditions)
        {
            if (conditions == null || conditions.Length == 0)
            {
                return new AnimatorConditionInfo[0];
            }

            var infos = new AnimatorConditionInfo[conditions.Length];
            for (int i = 0; i < conditions.Length; i++)
            {
                infos[i] = new AnimatorConditionInfo
                {
                    mode = conditions[i].mode.ToString(),
                    parameter = conditions[i].parameter,
                    threshold = conditions[i].threshold
                };
            }

            return infos;
        }

        private static AnimatorMotionInfo BuildAnimatorMotionInfo(Motion motion)
        {
            if (motion == null)
            {
                return null;
            }

            var assetPath = AssetDatabase.GetAssetPath(motion);
            var blendTree = motion as BlendTree;
            var childInfos = new List<AnimatorBlendTreeChildInfo>();
            if (blendTree != null)
            {
                for (int i = 0; i < blendTree.children.Length; i++)
                {
                    var child = blendTree.children[i];
                    var childMotion = child.motion;
                    var childAssetPath = childMotion != null ? AssetDatabase.GetAssetPath(childMotion) : string.Empty;
                    childInfos.Add(new AnimatorBlendTreeChildInfo
                    {
                        motionName = childMotion != null ? childMotion.name : string.Empty,
                        motionType = childMotion != null ? childMotion.GetType().Name : string.Empty,
                        motionAssetPath = string.IsNullOrEmpty(childAssetPath) ? string.Empty : childAssetPath,
                        isSubAsset = childMotion != null && AssetDatabase.IsSubAsset(childMotion),
                        threshold = child.threshold,
                        position = FormatVector2(child.position),
                        timeScale = child.timeScale,
                        cycleOffset = child.cycleOffset,
                        mirror = child.mirror,
                        directBlendParameter = child.directBlendParameter
                    });
                }
            }

            return new AnimatorMotionInfo
            {
                name = motion.name,
                motionType = motion.GetType().Name,
                assetPath = string.IsNullOrEmpty(assetPath) ? string.Empty : assetPath,
                isSubAsset = AssetDatabase.IsSubAsset(motion),
                blendType = blendTree != null ? blendTree.blendType.ToString() : string.Empty,
                blendParameter = blendTree != null ? blendTree.blendParameter : string.Empty,
                blendParameterY = blendTree != null ? blendTree.blendParameterY : string.Empty,
                useAutomaticThresholds = blendTree != null && blendTree.useAutomaticThresholds,
                children = childInfos.ToArray()
            };
        }

        private static string GetMotionPath(Motion motion)
        {
            if (motion == null)
            {
                return string.Empty;
            }

            var assetPath = AssetDatabase.GetAssetPath(motion);
            return string.IsNullOrEmpty(assetPath) ? motion.name : assetPath;
        }

        private static string GetAnimatorParameterDefaultValue(AnimatorControllerParameter parameter)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Bool:
                    return parameter.defaultBool ? "true" : "false";
                case AnimatorControllerParameterType.Float:
                    return parameter.defaultFloat.ToString("0.###", CultureInfo.InvariantCulture);
                case AnimatorControllerParameterType.Int:
                    return parameter.defaultInt.ToString(CultureInfo.InvariantCulture);
                default:
                    return string.Empty;
            }
        }

        private static bool TryResolveAnimatorTransition(
            Dictionary<string, string> args,
            AnimatorController controller,
            out AnimatorControllerLayer layer,
            out AnimatorStateMachine stateMachine,
            out AnimatorStateTransition transition,
            out string fromStateName,
            out string toStateName,
            out bool fromAnyState,
            out string error)
        {
            layer = null;
            stateMachine = null;
            transition = null;
            fromStateName = string.Empty;
            toStateName = string.Empty;
            fromAnyState = GetBool(args, "from_any_state", false);
            error = string.Empty;

            if (!TryResolveAnimatorStateMachine(controller, GetString(args, "layer_name", string.Empty), GetString(args, "state_machine_path", string.Empty), out layer, out stateMachine, out error))
            {
                return false;
            }

            toStateName = GetRequiredString(args, "to_state");
            if (string.IsNullOrEmpty(toStateName))
            {
                error = "Animator destination state name is required.";
                return false;
            }

            var transitionIndex = GetInt(args, "transition_index", 0);
            if (transitionIndex < 0)
            {
                error = "Animator transition_index must be 0 or greater.";
                return false;
            }

            var matches = new List<AnimatorStateTransition>();
            if (fromAnyState)
            {
                for (int i = 0; i < stateMachine.anyStateTransitions.Length; i++)
                {
                    if (DoesTransitionMatchDestination(stateMachine.anyStateTransitions[i], toStateName))
                    {
                        matches.Add(stateMachine.anyStateTransitions[i]);
                    }
                }
            }
            else
            {
                fromStateName = GetRequiredString(args, "from_state");
                if (string.IsNullOrEmpty(fromStateName))
                {
                    error = "Animator source state name is required when from_any_state is false.";
                    return false;
                }

                AnimatorState fromState;
                if (!TryFindAnimatorState(stateMachine, fromStateName, out fromState))
                {
                    error = "Source animator state not found: " + fromStateName;
                    return false;
                }

                for (int i = 0; i < fromState.transitions.Length; i++)
                {
                    if (DoesTransitionMatchDestination(fromState.transitions[i], toStateName))
                    {
                        matches.Add(fromState.transitions[i]);
                    }
                }
            }

            if (matches.Count == 0)
            {
                error = fromAnyState
                    ? "Any State transition not found for destination: " + toStateName
                    : "Animator transition not found: " + fromStateName + " -> " + toStateName;
                return false;
            }

            if (transitionIndex >= matches.Count)
            {
                error = "Animator transition_index out of range. Matched transitions: " + matches.Count;
                return false;
            }

            transition = matches[transitionIndex];
            return true;
        }

        private static bool DoesTransitionMatchDestination(AnimatorStateTransition transition, string destinationStateName)
        {
            return transition != null &&
                   transition.destinationState != null &&
                   string.Equals(transition.destinationState.name, destinationStateName, StringComparison.Ordinal);
        }

        private static bool TryApplyAnimatorTransitionProperties(
            Dictionary<string, string> args,
            AnimatorController controller,
            AnimatorStateTransition transition,
            out bool updated,
            out string error)
        {
            updated = false;
            error = string.Empty;
            if (transition == null)
            {
                error = "Animator transition is null.";
                return false;
            }

            bool boolValue;
            float floatValue;
            string stringValue;

            if (TryGetOptionalBool(args, "has_exit_time", out boolValue))
            {
                transition.hasExitTime = boolValue;
                updated = true;
            }

            if (TryGetOptionalFloat(args, "exit_time", out floatValue))
            {
                transition.exitTime = floatValue;
                updated = true;
            }

            if (TryGetOptionalFloat(args, "duration", out floatValue))
            {
                transition.duration = floatValue;
                updated = true;
            }

            if (TryGetOptionalFloat(args, "offset", out floatValue))
            {
                transition.offset = floatValue;
                updated = true;
            }

            if (TryGetOptionalBool(args, "fixed_duration", out boolValue))
            {
                transition.hasFixedDuration = boolValue;
                updated = true;
            }

            if (TryGetOptionalBool(args, "mute", out boolValue))
            {
                transition.mute = boolValue;
                updated = true;
            }

            if (TryGetOptionalBool(args, "solo", out boolValue))
            {
                transition.solo = boolValue;
                updated = true;
            }

            if (TryGetOptionalBool(args, "can_transition_to_self", out boolValue))
            {
                transition.canTransitionToSelf = boolValue;
                updated = true;
            }

            if (TryGetOptionalBool(args, "ordered_interruption", out boolValue))
            {
                transition.orderedInterruption = boolValue;
                updated = true;
            }

            if (TryGetOptionalString(args, "interruption_source", out stringValue))
            {
                TransitionInterruptionSource interruptionSource;
                if (!TryParseAnimatorInterruptionSource(stringValue, out interruptionSource))
                {
                    error = "Unsupported animator interruption source: " + stringValue;
                    return false;
                }

                transition.interruptionSource = interruptionSource;
                updated = true;
            }

            var clearConditions = GetBool(args, "clear_conditions", false);
            var replaceConditions = GetBool(args, "replace_conditions", false);
            var hasConditionDescriptors = false;
            List<AnimatorConditionDescriptor> conditionDescriptors = null;
            if (TryGetOptionalString(args, "conditions", out stringValue))
            {
                if (!TryParseAnimatorConditionDescriptors(stringValue, out conditionDescriptors, out error))
                {
                    return false;
                }

                hasConditionDescriptors = true;
            }
            else
            {
                conditionDescriptors = new List<AnimatorConditionDescriptor>();
            }

            if (clearConditions || replaceConditions)
            {
                ClearAnimatorConditions(transition);
                updated = true;
            }

            if (hasConditionDescriptors)
            {
                for (int i = 0; i < conditionDescriptors.Count; i++)
                {
                    if (!HasAnimatorParameter(controller, conditionDescriptors[i].parameter))
                    {
                        error = "Animator parameter not found: " + conditionDescriptors[i].parameter;
                        return false;
                    }

                    transition.AddCondition(conditionDescriptors[i].mode, conditionDescriptors[i].threshold, conditionDescriptors[i].parameter);
                }

                updated = true;
            }

            return true;
        }

        private static bool TryCollectAnimatorTransitionConditions(
            Dictionary<string, string> args,
            AnimatorController controller,
            out List<AnimatorConditionDescriptor> conditions,
            out string error)
        {
            conditions = new List<AnimatorConditionDescriptor>();
            error = string.Empty;

            var conditionModeRaw = GetString(args, "condition_mode", string.Empty);
            var conditionParameter = GetString(args, "condition_parameter", string.Empty);
            var hasConditionMode = !string.IsNullOrEmpty(conditionModeRaw);
            var hasConditionParameter = !string.IsNullOrEmpty(conditionParameter);
            if (hasConditionMode != hasConditionParameter)
            {
                error = "Animator transition requires condition_mode and condition_parameter together.";
                return false;
            }

            if (hasConditionMode)
            {
                AnimatorConditionMode conditionMode;
                if (!TryParseAnimatorConditionMode(conditionModeRaw, out conditionMode))
                {
                    error = "Unsupported animator condition mode: " + conditionModeRaw;
                    return false;
                }

                if (!HasAnimatorParameter(controller, conditionParameter))
                {
                    error = "Animator parameter not found: " + conditionParameter;
                    return false;
                }

                conditions.Add(new AnimatorConditionDescriptor
                {
                    mode = conditionMode,
                    parameter = conditionParameter,
                    threshold = GetFloat(args, "condition_threshold", 0f)
                });
            }

            string conditionDescriptorsRaw;
            if (TryGetOptionalString(args, "conditions", out conditionDescriptorsRaw))
            {
                List<AnimatorConditionDescriptor> parsedConditions;
                if (!TryParseAnimatorConditionDescriptors(conditionDescriptorsRaw, out parsedConditions, out error))
                {
                    return false;
                }

                for (int i = 0; i < parsedConditions.Count; i++)
                {
                    if (!HasAnimatorParameter(controller, parsedConditions[i].parameter))
                    {
                        error = "Animator parameter not found: " + parsedConditions[i].parameter;
                        return false;
                    }

                    conditions.Add(parsedConditions[i]);
                }
            }

            return true;
        }

        private static bool TryParseAnimatorConditionDescriptors(string raw, out List<AnimatorConditionDescriptor> conditions, out string error)
        {
            conditions = new List<AnimatorConditionDescriptor>();
            error = string.Empty;
            raw = NormalizeStringValue(raw);
            if (string.IsNullOrEmpty(raw))
            {
                return true;
            }

            var descriptors = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < descriptors.Length; i++)
            {
                var descriptor = descriptors[i].Trim();
                if (string.IsNullOrEmpty(descriptor))
                {
                    continue;
                }

                var parts = descriptor.Split(new[] { ':' }, StringSplitOptions.None);
                if (parts.Length < 2 || parts.Length > 3)
                {
                    error = "Invalid animator condition descriptor: " + descriptor;
                    return false;
                }

                AnimatorConditionMode conditionMode;
                if (!TryParseAnimatorConditionMode(parts[0].Trim(), out conditionMode))
                {
                    error = "Unsupported animator condition mode: " + parts[0].Trim();
                    return false;
                }

                var parameter = parts[1].Trim();
                if (string.IsNullOrEmpty(parameter))
                {
                    error = "Animator condition parameter is required: " + descriptor;
                    return false;
                }

                var threshold = 0f;
                if (parts.Length == 3 && !float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out threshold))
                {
                    error = "Animator condition threshold is invalid: " + descriptor;
                    return false;
                }

                conditions.Add(new AnimatorConditionDescriptor
                {
                    mode = conditionMode,
                    parameter = parameter,
                    threshold = threshold
                });
            }

            return true;
        }

        private static void ClearAnimatorConditions(AnimatorStateTransition transition)
        {
            if (transition == null || transition.conditions == null)
            {
                return;
            }

            for (int i = transition.conditions.Length - 1; i >= 0; i--)
            {
                transition.RemoveCondition(transition.conditions[i]);
            }
        }

        private static bool TryGetOptionalBool(Dictionary<string, string> args, string key, out bool value)
        {
            value = false;
            string rawValue;
            if (!args.TryGetValue(key, out rawValue))
            {
                return false;
            }

            rawValue = NormalizeStringValue(rawValue);
            if (string.IsNullOrEmpty(rawValue))
            {
                return false;
            }

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

            return false;
        }

        private static bool TryGetOptionalFloat(Dictionary<string, string> args, string key, out float value)
        {
            value = 0f;
            string rawValue;
            if (!args.TryGetValue(key, out rawValue))
            {
                return false;
            }

            rawValue = NormalizeStringValue(rawValue);
            if (string.IsNullOrEmpty(rawValue))
            {
                return false;
            }

            return float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryGetOptionalString(Dictionary<string, string> args, string key, out string value)
        {
            value = string.Empty;
            string rawValue;
            if (!args.TryGetValue(key, out rawValue))
            {
                return false;
            }

            value = NormalizeStringValue(rawValue);
            return !string.IsNullOrEmpty(value);
        }

        private static string FormatVector2(Vector2 value)
        {
            return value.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   value.y.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
