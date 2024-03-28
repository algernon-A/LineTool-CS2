// <copyright file="LineToolUISystem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the Apache Licence, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineTool
{
    using System;
    using Colossal.Logging;
    using Colossal.UI.Binding;
    using Game.Prefabs;
    using Game.Tools;
    using Game.UI;
    using Unity.Entities;
    using UnityEngine.InputSystem;

    /// <summary>
    /// A tool UI system for LineTool.
    /// </summary>
    public sealed partial class LineToolUISystem : UISystemBase
    {
        // Cached references.
        private ToolSystem _toolSystem;
        private LineToolSystem _lineToolSystem;
        private ILog _log;

        // Internal status.
        private bool _toolIsActive = false;
        private ToolBaseSystem _previousSystem = null;

        /// <summary>
        /// Called when the system is created.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();

            // Set log.
            _log = Mod.Instance.Log;

            // Set references.
            _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            _lineToolSystem = World.GetOrCreateSystemManaged<LineToolSystem>();

            if (_lineToolSystem is null)
            {
                _log.Error("Fatal error: unable to get LineToolSystem reference");
                Enabled = false;
                return;
            }

            _toolSystem.EventPrefabChanged = (Action<PrefabBase>)Delegate.Combine(_toolSystem.EventPrefabChanged, new Action<PrefabBase>(OnPrefabChanged));

            // Mode UI bindings.
            AddUpdateBinding(new GetterValueBinding<bool>("LineTool", "ShowModeRow", () => _toolSystem.activeTool == _lineToolSystem || _toolSystem.activeTool is ObjectToolSystem));
            AddUpdateBinding(new GetterValueBinding<bool>("LineTool", "PointModeEnabled", () => _lineToolSystem.Mode == LineMode.Point || _toolSystem.activeTool != _lineToolSystem));
            AddUpdateBinding(new GetterValueBinding<bool>("LineTool", "StraightLineEnabled", () => _toolSystem.activeTool == _lineToolSystem && _lineToolSystem.Mode == LineMode.Straight));
            AddUpdateBinding(new GetterValueBinding<bool>("LineTool", "SimpleCurveEnabled", () => _toolSystem.activeTool == _lineToolSystem && _lineToolSystem.Mode == LineMode.SimpleCurve));
            AddUpdateBinding(new GetterValueBinding<bool>("LineTool", "CircleEnabled", () => _toolSystem.activeTool == _lineToolSystem && _lineToolSystem.Mode == LineMode.Circle));
            AddBinding(new TriggerBinding("LineTool", "SetPointMode", SetPointMode));
            AddBinding(new TriggerBinding("LineTool", "SetStraightLineMode", SetStraightMode));
            AddBinding(new TriggerBinding("LineTool", "SetSimpleCurveMode", SetSimpleCurveMode));
            AddBinding(new TriggerBinding("LineTool", "SetCircleMode", SetCircleMode));

            // Options UI bindings.
            AddUpdateBinding(new GetterValueBinding<bool>("LineTool", "FenceModeEnabled", () => _lineToolSystem.CurrentSpacingMode == SpacingMode.FenceMode));
            AddUpdateBinding(new GetterValueBinding<bool>("LineTool", "W2WModeEnabled", () => _lineToolSystem.CurrentSpacingMode == SpacingMode.W2WMode));
            AddBinding(new TriggerBinding("LineTool", "ToggleFenceMode", ToggleFenceMode));
            AddBinding(new TriggerBinding("LineTool", "ToggleW2WMode", ToggleW2WMode));

            // Spacing UI bindings.
            AddUpdateBinding(new GetterValueBinding<bool>("LineTool", "FullLengthEnabled", () => _lineToolSystem.CurrentSpacingMode == SpacingMode.FullLength));
            AddUpdateBinding(new GetterValueBinding<float>("LineTool", "Spacing", () => _lineToolSystem.Spacing));
            AddBinding(new TriggerBinding("LineTool", "ToggleFullLength", ToggleFullLength));
            AddBinding(new TriggerBinding("LineTool", "IncreaseSpacing", IncreaseSpacing));
            AddBinding(new TriggerBinding("LineTool", "DecreaseSpacing", DecreaseSpacing));

            // Rotation UI bindings.
            AddUpdateBinding(new GetterValueBinding<bool>("LineTool", "RandomRotationEnabled", () => _lineToolSystem.RandomRotation));
            AddUpdateBinding(new GetterValueBinding<float>("LineTool", "Rotation", () => _lineToolSystem.Rotation));
            AddBinding(new TriggerBinding("LineTool", "ToggleRandomRotation", ToggleRandomRotation));
            AddBinding(new TriggerBinding("LineTool", "IncreaseRotation", IncreaseRotation));
            AddBinding(new TriggerBinding("LineTool", "DecreaseRotation", DecreaseRotation));

            // Random spacing.
            AddUpdateBinding(new GetterValueBinding<float>("LineTool", "SpacingVariation", () => _lineToolSystem.RandomSpacing));
            AddBinding(new TriggerBinding("LineTool", "IncreaseSpacingVariation", IncreaseSpacingVariation));
            AddBinding(new TriggerBinding("LineTool", "DecreaseSpacingVariation", DecreaseSpacingVariation));

            // Random offset.
            AddUpdateBinding(new GetterValueBinding<float>("LineTool", "OffsetVariation", () => _lineToolSystem.RandomOffset));
            AddBinding(new TriggerBinding("LineTool", "IncreaseOffsetVariation", IncreaseOffsetVariation));
            AddBinding(new TriggerBinding("LineTool", "DecreaseOffsetVariation", DecreaseOffsetVariation));
        }

        /// <summary>
        /// Called every UI update.
        /// </summary>
        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Check for line tool activation.
            if (_toolSystem.activeTool == _lineToolSystem)
            {
                // Activate tool.
                if (!_toolIsActive)
                {
                    // Tool is now active but previously wasn't; update previous tool system record.
                    _previousSystem = _lineToolSystem;

                    // Record current tool state.
                    _toolIsActive = true;
                }
            }
            else
            {
                // Line tool not active - clean up if this is the first update after deactivation.
                if (_toolIsActive)
                {
                    // Record current tool state.
                    _toolIsActive = false;
                }
                else
                {
                    // Check to see if another tool change has occurred.
                    if (_toolSystem.activeTool != _previousSystem)
                    {
                        // Active tool has changed - record new tool.
                        _previousSystem = _toolSystem.activeTool;
                    }
                }
            }
        }

        /// <summary>
        /// Handles changes in the selected prefab.
        /// </summary>
        /// <param name="prefab">New selected prefab.</param>
        private void OnPrefabChanged(PrefabBase prefab)
        {
            // If the line tool is currently activated and the new prefab is a placeable object, reactivate it (the game will reset the tool to the relevant object tool).
            if (_toolSystem.activeTool == _lineToolSystem && prefab is StaticObjectPrefab)
            {
                _lineToolSystem.EnableTool();
            }
        }

        /// <summary>
        /// Event callback to set single item mode.
        /// </summary>
        private void SetPointMode()
        {
            // Restore previously-used tool.
            _lineToolSystem.RestorePreviousTool();
            _lineToolSystem.Mode = LineMode.Point;
        }

        /// <summary>
        /// Event callback to set straight line mode.
        /// </summary>
        private void SetStraightMode()
        {
            // Ensure tool is activated.
            _lineToolSystem.Mode = LineMode.Straight;
            _lineToolSystem.EnableTool();
        }

        /// <summary>
        /// Event callback to set simple curve mode.
        /// </summary>
        private void SetSimpleCurveMode()
        {
            // Ensure tool is activated.
            _lineToolSystem.Mode = LineMode.SimpleCurve;
            _lineToolSystem.EnableTool();
        }

        /// <summary>
        /// Event callback to set circle mode.
        /// </summary>
        private void SetCircleMode()
        {
            // Ensure tool is activated.
            _lineToolSystem.Mode = LineMode.Circle;
            _lineToolSystem.EnableTool();
        }

        /// <summary>
        /// Event callback to toggle fence mode.
        /// </summary>
        private void ToggleFenceMode()
        {
            if (_lineToolSystem.CurrentSpacingMode != SpacingMode.FenceMode)
            {
                _lineToolSystem.CurrentSpacingMode = SpacingMode.FenceMode;
            }
            else
            {
                _lineToolSystem.CurrentSpacingMode = SpacingMode.Manual;
            }
        }

        /// <summary>
        /// Event callback to toggle wall-to-wall mode.
        /// </summary>
        private void ToggleW2WMode()
        {
            if (_lineToolSystem.CurrentSpacingMode != SpacingMode.W2WMode)
            {
                _lineToolSystem.CurrentSpacingMode = SpacingMode.W2WMode;
            }
            else
            {
                _lineToolSystem.CurrentSpacingMode = SpacingMode.Manual;
            }
        }

        /// <summary>
        /// Event callback to toggle full-length mode.
        /// </summary>
        private void ToggleFullLength()
        {
            if (_lineToolSystem.CurrentSpacingMode != SpacingMode.FullLength)
            {
                _lineToolSystem.CurrentSpacingMode = SpacingMode.FullLength;
            }
            else
            {
                _lineToolSystem.CurrentSpacingMode = SpacingMode.Manual;
            }
        }

        /// <summary>
        /// Event callback to increase spacing by one step.
        /// </summary>
        private void IncreaseSpacing() => _lineToolSystem.Spacing += GetSpacingStep();

        /// <summary>
        /// Event callback to increase spacing by one step.
        /// </summary>
        private void DecreaseSpacing() => _lineToolSystem.Spacing -= GetSpacingStep();

        /// <summary>
        /// Event callback to toggle random rotation.
        /// </summary>
        private void ToggleRandomRotation() => _lineToolSystem.RandomRotation = !_lineToolSystem.RandomRotation;

        /// <summary>
        /// Event callback to increase rotation by one step.
        /// </summary>
        private void IncreaseRotation() => _lineToolSystem.Rotation += GetRotationStep();

        /// <summary>
        /// Event callback to decrease rotation by one step.
        /// </summary>
        private void DecreaseRotation() => _lineToolSystem.Rotation -= GetRotationStep();

        /// <summary>
        /// Event callback to increase random spacing variation by one step.
        /// </summary>
        private void IncreaseSpacingVariation() => _lineToolSystem.RandomSpacing += GetSpacingStep();

        /// <summary>
        /// Event callback to decrease random spacing variation by one step.
        /// </summary>
        private void DecreaseSpacingVariation() => _lineToolSystem.RandomSpacing -= GetSpacingStep();

        /// <summary>
        /// Event callback to increase random spacing variation by one step.
        /// </summary>
        private void IncreaseOffsetVariation() => _lineToolSystem.RandomOffset += GetSpacingStep();

        /// <summary>
        /// Event callback to decrease random spacing variation by one step.
        /// </summary>
        private void DecreaseOffsetVariation() => _lineToolSystem.RandomOffset -= GetSpacingStep();

        /// <summary>
        /// Gets the spacing step value to apply, including effects of shift- (x10) or control- (x0.1) modifiers.
        /// </summary>
        /// <returns>10 if the shift key is pressed, 0.1 if the control key is pressed, and 1 otherwise.</returns>
        private float GetSpacingStep()
        {
            if (Keyboard.current.shiftKey.isPressed)
            {
                // Shift; 10m.
                return 10f;
            }
            else if (Keyboard.current.ctrlKey.isPressed)
            {
                // Control; 0.1m.
                return 0.1f;
            }

            // No modifiers pressed; just return the standard step (1m).
            return 1f;
        }

        /// <summary>
        /// Gets the rotation step value to apply, including effects of shift- (x10) or control- (x0.1) modifiers.
        /// </summary>
        /// <returns>90 if the shift key is pressed, 1 if the control key is pressed, and 1 otherwise.</returns>
        private int GetRotationStep()
        {
            if (Keyboard.current.shiftKey.isPressed)
            {
                // Shift; 90 degrees.
                return 90;
            }
            else if (Keyboard.current.ctrlKey.isPressed)
            {
                // Control; 1 degree.
                return 1;
            }

            // No modifiers pressed; just return the standard step (10 degrees).
            return 10;
        }
    }
}
