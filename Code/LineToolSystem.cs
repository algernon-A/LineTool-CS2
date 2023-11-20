﻿// <copyright file="LineToolSystem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// </copyright>

namespace LineTool
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Game.Common;
    using Game.Input;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Simulation;
    using Game.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine.InputSystem;
    using static Game.Rendering.GuideLinesSystem;
    using Transform = Game.Objects.Transform;
    using Tree = Game.Objects.Tree;

    /// <summary>
    /// Line tool system.
    /// </summary>
    public sealed partial class LineToolSystem : ObjectToolBaseSystem
    {
        // Previewing.
        private readonly NativeList<Entity> _previewEntities = new (Allocator.Persistent);
        private readonly NativeList<TooltipInfo> _tooltips = new (8, Allocator.Persistent);

        // Line calculations.
        private readonly NativeList<PointData> _points = new (Allocator.Persistent);

        // Cursor.
        private ControlPoint _raycastPoint;
        private Entity _cursorEntity = Entity.Null;
        private float2 _previousPos;

        // Prefab selection.
        private ObjectPrefab _selectedPrefab;
        private Entity _selectedEntity = Entity.Null;
        private int _originalXP;

        // References.
        private ILog _log;
        private TerrainSystem _terrainSystem;
        private TerrainHeightData _terrainHeightData;
        private ProxyAction _applyAction;
        private ProxyAction _cancelAction;
        private OverlayRenderSystem.Buffer _overlayBuffer;

        // Mode.
        private LineMode _currentMode;
        private LineModeBase _mode;

        // Tool settings.
        private float _spacing = 20f;

        /// <summary>
        /// Gets the tool's ID string.
        /// Mimics the network tool to use its icons.
        /// </summary>
        public override string toolID => "Net Tool";

        /// <summary>
        /// Gets or sets the line spacing.
        /// </summary>
        internal float Spacing { get => _spacing; set => _spacing = value; }

        /// <summary>
        /// Gets the tooltip list.
        /// </summary>
        internal NativeList<TooltipInfo> Tooltips => _tooltips;

        /// <summary>
        /// Gets or sets the current line mode.
        /// </summary>
        internal LineMode Mode
        {
            get => _currentMode;

            set
            {
                // Don't do anything if no change.
                if (value == _currentMode)
                {
                    return;
                }

                // Apply updated tool mode.
                switch (value)
                {
                    case LineMode.Straight:
                        _mode = new StraightMode(_mode);
                        break;
                    case LineMode.SimpleCurve:
                        _mode = new SimpleCurveMode(_mode);
                        break;
                    case LineMode.Circle:
                        _mode = new CircleMode(_mode);
                        break;
                }

                // Update mode.
                _currentMode = value;
            }
        }

        /// <summary>
        /// Sets the currently selected preab.
        /// </summary>
        private PrefabBase SelectedPrefab
        {
            set
            {
                _selectedPrefab = value as ObjectPrefab;

                // Update selected entity;
                if (_selectedPrefab is null)
                {
                    _selectedEntity = Entity.Null;
                }
                else
                {
                    // Get selected entity.
                    _selectedEntity = m_PrefabSystem.GetEntity(_selectedPrefab);

                    // Reduce any XP to zero while we're using the tool.
                    if (EntityManager.TryGetComponent(_selectedEntity, out PlaceableObjectData placeableData))
                    {
                        _originalXP = placeableData.m_XPReward;
                        placeableData.m_XPReward = 0;
                        EntityManager.SetComponentData(_selectedEntity, placeableData);
                    }
                    else
                    {
                        _originalXP = 0;
                    }
                }
            }
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
        /// <returns>C<c>null</c>.</returns>
        public override PrefabBase GetPrefab() => null; // TODO:_selectedPrefab;

        /// <summary>
        /// Sets the prefab selected by this tool.
        /// </summary>
        /// <param name="prefab">Prefab to set.</param>
        /// <returns><c>true</c> if a prefab is currently selected, otherwise <c>false</c>.</returns>
        public override bool TrySetPrefab(PrefabBase prefab)
        {
            // Check for eligible prefab.
            if (prefab is ObjectPrefab objectPrefab)
            {
                // Eligible - set it.
                SelectedPrefab = objectPrefab;
                return true;
            }

            // If we got here this isn't an eligible prefab.
            return false;
        }

        /// <summary>
        /// Called when the system is created.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();

            // Set log.
            _log = Mod.Log;

            // Get system references.
            _terrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
            _overlayBuffer = World.GetOrCreateSystemManaged<OverlayRenderSystem>().GetBuffer(out var _);

            // Set default mode.
            _currentMode = LineMode.Straight;
            _mode = new StraightMode();

            // Set actions.
            _applyAction = InputManager.instance.FindAction("Tool", "Apply");
            _cancelAction = InputManager.instance.FindAction("Tool", "Mouse Cancel");

            // Enable hotkey.
            InputAction hotKey = new ("LineTool");
            hotKey.AddCompositeBinding("ButtonWithOneModifier").With("Modifier", "<Keyboard>/ctrl").With("Button", "<Keyboard>/l");
            hotKey.performed += EnableTool;
            hotKey.Enable();
        }

        /// <summary>
        /// Called every tool update.
        /// </summary>
        /// <param name="inputDeps">Input dependencies.</param>
        /// <returns>Job handle.</returns>
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // Clear tooltips.
            _tooltips.Clear();

            // Check for and perform any cancellation.
            if (_cancelAction.WasPressedThisFrame())
            {
                // Reset current mode settings.
                _mode.Reset();

                // Revert previewing.
                foreach (Entity previewEntity in _previewEntities)
                {
                    EntityManager.AddComponent<Deleted>(previewEntity);
                }

                _previewEntities.Clear();

                return inputDeps;
            }

            // Don't do anything if no selected prefab.
            if (_selectedPrefab is null)
            {
                return inputDeps;
            }

            // Check for valid raycast.
            GetRaycastResult(out _raycastPoint);
            if (_raycastPoint.m_HitPosition.x == 0f && _raycastPoint.m_HitPosition.z == 0f)
            {
                // Invalid raycast.
                return inputDeps;
            }

            // Raycast is valid - get world position.
            float3 position = _raycastPoint.m_HitPosition;

            // Calculate terrain height.
            _terrainHeightData = _terrainSystem.GetHeightData();
            position.y = TerrainUtils.SampleHeight(ref _terrainHeightData, position);

            // Handle apply action.
            if (_applyAction.WasPressedThisFrame())
            {
                // Handle click.
                if (_mode.HandleClick(position))
                {
                    // We're placing items - remove highlighting.
                    foreach (Entity previewEntity in _previewEntities)
                    {
                        EntityManager.RemoveComponent<Highlighted>(previewEntity);
                        EntityManager.AddComponent<Updated>(previewEntity);
                    }

                    // Clear preview.
                    _previewEntities.Clear();

                    // Reset tool mode.
                    _mode.ItemsPlaced(position);

                    return inputDeps;
                }
            }

            // Update cursor entity if we haven't got an initial position set.
            if (!_mode.HasStart)
            {
                // Create cursor entity if none yet exists.
                if (_cursorEntity == Entity.Null)
                {
                    _cursorEntity = CreateEntity();

                    // Highlight cursor entity.
                    EntityManager.AddComponent<Highlighted>(_cursorEntity);
                }

                // Update cursor entity position.
                EntityManager.SetComponentData(_cursorEntity, new Transform { m_Position = position, m_Rotation = quaternion.identity, });
                EntityManager.AddComponent<Updated>(_cursorEntity);

                return inputDeps;
            }
            else if (_cursorEntity != Entity.Null)
            {
                // Cancel cursor entity.
                EntityManager.AddComponent<Deleted>(_cursorEntity);
                _cursorEntity = Entity.Null;
            }

            // Render any overlay.
            _mode.DrawOverlay(position, _overlayBuffer, _tooltips);

            // Check for position change.
            if (position.x == _previousPos.x && position.z == _previousPos.y)
            {
                // No position change.
                return inputDeps;
            }

            // Update stored position.
            _previousPos.x = position.x;
            _previousPos.y = position.z;

            // If we got here we're (re)calculating points.
            _points.Clear();
            _mode.CalculatePoints(position, Spacing, 0f, _points, ref _terrainHeightData);

            // Step along length and place objects.
            int count = 0;
            foreach (PointData thisPoint in _points)
            {
                // Create transform component.
                Transform transformData = new ()
                {
                    m_Position = thisPoint.Position,
                    m_Rotation = thisPoint.Rotation,
                };

                // Create new entity if required.
                if (count >= _previewEntities.Length)
                {
                    // Create new entity.
                    Entity newEntity = CreateEntity();

                    // Set entity location.
                    EntityManager.SetComponentData(newEntity, transformData);
                    EntityManager.AddComponent<Highlighted>(newEntity);
                    _previewEntities.Add(newEntity);
                }
                else
                {
                    // Otherwise, use existing entity.
                    EntityManager.SetComponentData(_previewEntities[count], transformData);
                    EntityManager.AddComponent<Updated>(_previewEntities[count]);

                    // Ensure any trees are still adults.
                    if (EntityManager.TryGetComponent<Tree>(_previewEntities[count], out Tree tree))
                    {
                        tree.m_State = TreeState.Adult;
                        tree.m_Growth = 128;
                        EntityManager.SetComponentData(_previewEntities[count], tree);
                    }
                }

                // Increment distance.
                ++count;
            }

            // Clear any excess entities.
            if (count < _previewEntities.Length)
            {
                int startCount = count;
                while (count < _previewEntities.Length)
                {
                    EntityManager.AddComponent<Deleted>(_previewEntities[count++]);
                }

                // Remove excess range from list
                _previewEntities.RemoveRange(startCount, count - startCount);
            }

            return inputDeps;
        }

        /// <summary>
        /// Called when the tool starts running.
        /// </summary>
        protected override void OnStartRunning()
        {
            _log.Debug("OnStartRunning");
            base.OnStartRunning();

            // Ensure apply action is enabled.
            _applyAction.shouldBeEnabled = true;
            _cancelAction.shouldBeEnabled = true;

            // Clear any previous raycast result.
            _raycastPoint = default;

            // Reset any previously-stored starting position.
            _mode.Reset();

            // Clear any applications.
            applyMode = ApplyMode.Clear;
        }

        /// <summary>
        /// Called when the tool stops running.
        /// </summary>
        protected override void OnStopRunning()
        {
            _log.Debug("OnStopRunning");

            // Disable apply action.
            _applyAction.shouldBeEnabled = false;
            _cancelAction.shouldBeEnabled = false;

            // Cancel cursor entity.
            if (_cursorEntity != Entity.Null)
            {
                EntityManager.AddComponent<Deleted>(_cursorEntity);
                _cursorEntity = Entity.Null;
            }

            // Revert previewing.
            foreach (Entity previewEntity in _previewEntities)
            {
                EntityManager.AddComponent<Deleted>(previewEntity);
            }

            // Restore original prefab XP, if we changed it.
            if (_originalXP != 0 && EntityManager.TryGetComponent(_selectedEntity, out PlaceableObjectData placeableData))
            {
                placeableData.m_XPReward = _originalXP;
                EntityManager.SetComponentData(_selectedEntity, placeableData);
                _originalXP = 0;
            }

            base.OnStopRunning();
        }

        /// <summary>
        /// Called when the system is destroyed.
        /// </summary>
        protected override void OnDestroy()
        {
            // Dispose of unmanaged lists.
            _previewEntities.Dispose();
            _points.Dispose();
            _tooltips.Dispose();

            base.OnDestroy();
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
                // Check for valid prefab selection before continuing.
                SelectedPrefab = World.GetOrCreateSystemManaged<ObjectToolSystem>().prefab;
                if (_selectedPrefab != null)
                {
                    // Valid prefab selected - switch to this tool.
                    m_ToolSystem.selected = Entity.Null;
                    m_ToolSystem.activeTool = this;
                }
            }
        }

        /// <summary>
        /// Creates a new copy of the currently selected entity.
        /// </summary>
        /// <returns>New entity.</returns>
        private Entity CreateEntity()
        {
            // Create new entity.
            ObjectData componentData = EntityManager.GetComponentData<ObjectData>(_selectedEntity);
            Entity newEntity = EntityManager.CreateEntity(componentData.m_Archetype);

            // Set prefab and transform.
            EntityManager.SetComponentData(newEntity, new PrefabRef(_selectedEntity));

            // Set tree growth to adult if this is a tree.
            if (EntityManager.HasComponent<Tree>(newEntity))
            {
                Tree treeData = new ()
                {
                    m_State = TreeState.Adult,
                    m_Growth = 128,
                };

                EntityManager.SetComponentData(newEntity, treeData);
            }

            return newEntity;
        }
    }
}
