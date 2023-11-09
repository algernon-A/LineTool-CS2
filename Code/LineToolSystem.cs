// <copyright file="LineToolSystem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// </copyright>

namespace LineTool
{
    using System;
    using System.Collections.Generic;
    using cohtml.Net;
    using Game.Common;
    using Game.Input;
    using Game.Objects;
    using Game.Prefabs;
    using Game.SceneFlow;
    using Game.Simulation;
    using Game.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine.InputSystem;
    using Random = Unity.Mathematics.Random;
    using Transform = Game.Objects.Transform;
    using Tree = Game.Objects.Tree;

    /// <summary>
    /// Line tool system.
    /// </summary>
    public sealed class LineToolSystem : ToolBaseSystem
    {
        private ControlPoint m_RaycastPoint;

        // References.
        private TerrainSystem _terrainSystem;
        private TerrainHeightData _terrainHeightData;
        private ProxyAction _applyAction;

        // Prefab selection.
        private EntityQuery _prefabs;

        // Line position.
        private bool _validFirstPos = false;
        private float3 _firstPos;

        // Tool settings.
        private float _spacing = 20f;

        // UI View reference.
        private View _uiView;

        /// <summary>
        /// Line tool modes.
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// Straight line.
            /// </summary>
            Straight,

            /// <summary>
            /// Simple curve.
            /// </summary>
            SimpleCurve,
        }

        /// <summary>
        /// Gets the tool's ID string.
        /// Mimics the network tool to use its icons.
        /// </summary>
        public override string toolID => "Net Tool";

        /// <summary>
        /// Gets the GUI modes for the tool.
        /// </summary>
        /// <param name="modes">List of tool modes.</param>
        public override void GetUIModes(List<ToolMode> modes)
        {
            modes.Add(new ToolMode(Mode.Straight.ToString(), 0));
            modes.Add(new ToolMode(Mode.SimpleCurve.ToString(), 1));
        }

        /// <summary>
        /// Called when the raycast is initialized.
        /// </summary>
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();

            // Set raycast mask.
            m_ToolRaycastSystem.typeMask = TypeMask.Terrain;
        }

        /// <summary>
        /// Gets the prefab selected by this tool.
        /// </summary>
        /// <returns>Always <c>null</c>.</returns>
        public override PrefabBase GetPrefab() => null;

        /// <summary>
        /// Sets the prefab selected by this tool.
        /// </summary>
        /// <param name="prefab">Prefab to set.</param>
        /// <returns>Always <c>false</c>.</returns>
        public override bool TrySetPrefab(PrefabBase prefab) => false;

        /// <summary>
        /// Called every tool update.
        /// </summary>
        /// <param name="inputDeps">Input dependencies.</param>
        /// <returns>Job handle.</returns>
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // Handle apply action.
            if (_applyAction.WasPressedThisFrame())
            {
                Logging.LogDebug("action pressed");

                // Check for valid raycast.
                GetRaycastResult(out m_RaycastPoint);
                if (m_RaycastPoint.m_HitPosition.x != 0f || m_RaycastPoint.m_HitPosition.z != 0f)
                {
                    // Raycast is valid - get world position.
                    float3 position = m_RaycastPoint.m_HitPosition;

                    // Calculate terrain height.
                    _terrainHeightData = _terrainSystem.GetHeightData();
                    position.y = TerrainUtils.SampleHeight(ref _terrainHeightData, position);

                    // Record line start position and return if this is the first action.
                    if (!_validFirstPos)
                    {
                        Logging.LogDebug("setting first position");
                        _validFirstPos = true;
                        _firstPos = position;
                        return inputDeps;
                    }

                    // If we got here we've got two valid points - calculate distance between them.
                    float length = math.length(position - _firstPos);

                    // Set up entity selection.
                    NativeArray<Entity> nativeArray = _prefabs.ToEntityArray(Allocator.TempJob);

                    // Choose random prefab.
                    Logging.LogDebug("selecting random prefab");
                    Random random = new ((uint)DateTime.Now.Ticks);
                    Entity entity = nativeArray[random.NextInt(nativeArray.Length)];

                    try
                    {
                        // Step along length and place objects.
                        float currentDistance = 0f;
                        while (currentDistance < length)
                        {
                            // Calculate interpolated point.
                            float3 thisPoint = math.lerp(_firstPos, position, currentDistance / length);

                            // Get height for this point.
                            thisPoint.y = TerrainUtils.SampleHeight(ref _terrainHeightData, thisPoint);

                            // Create components.
                            Transform transformData = new () { m_Position = thisPoint };
                            Tree treeData = new ()
                            {
                                m_State = TreeState.Adult,
                                m_Growth = 128,
                            };

                            // Create new tree.
                            ObjectData componentData = EntityManager.GetComponentData<ObjectData>(entity);
                            Entity newEntity = EntityManager.CreateEntity(componentData.m_Archetype);
                            EntityManager.SetComponentData(newEntity, new PrefabRef(entity));
                            EntityManager.SetComponentData(newEntity, transformData);
                            EntityManager.SetComponentData(newEntity, treeData);

                            // Increment distance.
                            currentDistance += _spacing;
                        }
                    }
                    finally
                    {
                        // Ensure disposal of native array.
                        nativeArray.Dispose();
                    }

                    // Invalidate first position now that we've placed this line.
                    _validFirstPos = false;
                }
                else
                {
                    Logging.LogDebug("invalid raycast");
                }
            }

            return inputDeps;
        }

        /// <summary>
        /// Called when the system is created.
        /// </summary>
        protected override void OnCreate()
        {
            Logging.LogInfo("OnCreate");
            base.OnCreate();

            // Initialize tree prefab query.
            _prefabs = GetEntityQuery(ComponentType.ReadOnly<TreeData>(), ComponentType.Exclude<PlaceholderObjectElement>());
            RequireForUpdate(_prefabs);

            // Get system references.
            _terrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();

            // Set actions.
            _applyAction = InputManager.instance.FindAction("Tool", "Apply");

            // Enable hotkey.
            InputAction hotKey = new ("LineTool");
            hotKey.AddCompositeBinding("ButtonWithOneModifier").With("Modifier", "<Keyboard>/ctrl").With("Button", "<Keyboard>/l");
            hotKey.performed += EnableTool;
            hotKey.Enable();

            // Load tool UI.
            LoadUI();
        }

        /// <summary>
        /// Called when the tool starts running.
        /// </summary>
        protected override void OnStartRunning()
        {
            Logging.LogDebug("OnStartRunning");
            base.OnStartRunning();

            // Ensure apply action is enabled.
            _applyAction.shouldBeEnabled = true;

            // Clear any previous raycast result.
            m_RaycastPoint = default;

            // Reset any previously-stored starting position.
            _validFirstPos = false;

            // Show UI.
            SetUIVisibility(true);
        }

        /// <summary>
        /// Called when the tool stops running.
        /// </summary>
        protected override void OnStopRunning()
        {
            Logging.LogDebug("OnStopRunning");

            // Hide UI.
            SetUIVisibility(false);

            // Disable apply action.
            _applyAction.shouldBeEnabled = false;

            base.OnStopRunning();
        }

        /// <summary>
        /// Enables the tool (called by hotkey action).
        /// </summary>
        /// <param name="context">Callback context.</param>
        private void EnableTool(InputAction.CallbackContext context)
        {
            // Activate this tool if it isn't already active.
            if (m_ToolSystem.activeTool != this)
            {
                Logging.LogDebug("enabling tool");

                m_ToolSystem.selected = Entity.Null;
                m_ToolSystem.activeTool = this;
            }
        }

        /// <summary>
        /// Loads the UI.
        /// </summary>
        private void LoadUI()
        {
            Logging.LogInfo("loading UI files");

            // Ensure we can get the UI view before proceeding.
            _uiView = GameManager.instance.userInterface.view.View;
            if (_uiView != null)
            {
                // Register event callbacks.
                _uiView.RegisterForEvent("AdjustSpacing", (Action<float>)SetSpacing);

                // Load UI files.
                UIFileUtils.ReadUIFiles(_uiView, "UI");

                // Custom panel is hidden to start.
                SetUIVisibility(false);
            }
        }

        /// <summary>
        /// Sets the tool UI's visibility.
        /// </summary>
        /// <param name="isVisible">Visibility status to set.</param>
        private void SetUIVisibility(bool isVisible) => UIFileUtils.ExecuteScript(_uiView, $"setVisibility('line-tool-window', {(isVisible ? "true" : "false")});");

        /// <summary>
        /// Sets the current spacing.
        /// </summary>
        /// <param name="spacing">Spacing to set.</param>
        private void SetSpacing(float spacing) => _spacing = spacing;
    }
}
