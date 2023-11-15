// <copyright file="LineToolSystem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// </copyright>

namespace LineTool
{
    using System.Collections.Generic;
    using Colossal.Logging;
    using Game.Common;
    using Game.Input;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Simulation;
    using Game.Tools;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine.InputSystem;
    using Transform = Game.Objects.Transform;
    using Tree = Game.Objects.Tree;

    /// <summary>
    /// Line tool system.
    /// </summary>
    public sealed partial class LineToolSystem : ObjectToolBaseSystem
    {
        // Previewing.
        private readonly List<Entity> _previewEntities = new ();

        // Cursor.
        private ControlPoint _raycastPoint;
        private Entity _cursorEntity = Entity.Null;
        private float2 _previousPos;

        // Prefab selection.
        private ObjectPrefab _selectedPrefab;
        private Entity _selectedEntity = Entity.Null;

        // Line position.
        private bool _validFirstPos = false;
        private float3 _firstPos;

        // References.
        private ILog _log;
        private TerrainSystem _terrainSystem;
        private TerrainHeightData _terrainHeightData;
        private ProxyAction _applyAction;
        private ProxyAction _cancelAction;

        // Tool settings.
        private float _spacing = 20f;

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
        /// Gets or sets the line spacing.
        /// </summary>
        internal float Spacing { get => _spacing; set => _spacing = value; }

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
                }
            }
        }

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
        /// Called every tool update.
        /// </summary>
        /// <param name="inputDeps">Input dependencies.</param>
        /// <returns>Job handle.</returns>
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // Check for and perform any cancellation.
            if (_cancelAction.WasPressedThisFrame())
            {
                _validFirstPos = false;

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
                _validFirstPos = true;
                _firstPos = position;

                // Remove highlighting.
                foreach (Entity previewEntity in _previewEntities)
                {
                    EntityManager.RemoveComponent<Highlighted>(previewEntity);
                    EntityManager.AddComponent<Updated>(previewEntity);
                }

                _previewEntities.Clear();

                return inputDeps;
            }

            // Update cursor entity if we haven't got an initial position set.
            if (!_validFirstPos)
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

            // Check for position change.
            if (position.x == _previousPos.x && position.z == _previousPos.y)
            {
                // No position change.
                return inputDeps;
            }

            // Update stored position.
            _previousPos.x = position.x;
            _previousPos.y = position.z;

            // If we got here we've got two valid points - calculate distance between them.
            float length = math.length(position - _firstPos);

            if (length < 0)
            {
                length = 0 - length;
            }

            // Step along length and place objects.
            int count = 0;
            float currentDistance = 0f;
            while (currentDistance < length)
            {
                // Calculate interpolated point.
                float3 thisPoint = math.lerp(_firstPos, position, currentDistance / length);

                // Get height for this point.
                thisPoint.y = TerrainUtils.SampleHeight(ref _terrainHeightData, thisPoint);

                // Create transform component.
                Transform transformData = new ()
                {
                    m_Position = thisPoint,
                    m_Rotation = quaternion.identity,
                };

                // Create new entity if required.
                if (count >= _previewEntities.Count)
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
                }

                // Increment distance.
                count++;
                currentDistance += _spacing;
            }

            // Clear any excess entities.
            if (count < _previewEntities.Count)
            {
                int startCount = count;
                while (count < _previewEntities.Count)
                {
                    EntityManager.AddComponent<Deleted>(_previewEntities[count++]);
                }

                // Remove excess range from list
                _previewEntities.RemoveRange(startCount, count - startCount);
            }

            return inputDeps;
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
            _validFirstPos = false;

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
                _log.Debug("enabling tool");

                if (World.GetOrCreateSystemManaged<ObjectToolSystem>().prefab is ObjectPrefab prefab)
                {
                    _log.Info("selected prefab was found: " + prefab.name);
                    SelectedPrefab = prefab;
                }

                m_ToolSystem.selected = Entity.Null;
                m_ToolSystem.activeTool = this;
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
