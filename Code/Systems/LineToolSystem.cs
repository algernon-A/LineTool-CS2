// <copyright file="LineToolSystem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the Apache Licence, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineTool
{
    using System;
    using System.Collections.Generic;
    using Colossal.Logging;
    using Colossal.Mathematics;
    using Game;
    using Game.Areas;
    using Game.Audio;
    using Game.Buildings;
    using Game.City;
    using Game.Common;
    using Game.Input;
    using Game.Net;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Simulation;
    using Game.Tools;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine.InputSystem;
    using static Game.Rendering.GuideLinesSystem;
    using AgeMask = Game.Tools.AgeMask;
    using Random = Unity.Mathematics.Random;
    using Transform = Game.Objects.Transform;

    /// <summary>
    /// Line tool system.
    /// </summary>
    public sealed partial class LineToolSystem : ObjectToolBaseSystem
    {
        // Native buffers.
        private List<TooltipInfo> _tooltips;
        private List<PointData> _points;

        // Line calculations.
        private bool _fixedPreview = false;
        private float3 _fixedPos;

        // Cursor.
        private ControlPoint _raycastPoint;
        private float3 _previousPos;

        // Prefab selection.
        private ToolBaseSystem _previousTool = null;
        private ObjectGeometryPrefab _selectedPrefab;
        private Entity _selectedEntity = Entity.Null;
        private Bounds1 _xBounds;
        private Bounds1 _zBounds;

        // Randomization.
        private Random _random = new ();
        private List<RandomSeed> _randomSeeds;
        private float _previousSeedTime = 0f;
        private int _fixedRandomSeed = 0;

        // References.
        private ILog _log;
        private TerrainSystem _terrainSystem;
        private TerrainHeightData _terrainHeightData;
        private OverlayRenderSystem.Buffer _overlayBuffer;
        private CityConfigurationSystem _cityConfigurationSystem;
        private ObjectToolSystem _objectToolSystem;
        private AudioManager _audioManager;

        // Game settings queries.
        private EntityQuery _renderingSettingsQuery;
        private EntityQuery _soundEffectsQuery;

        // Input actions.
        private InputAction _fixedPreviewAction;
        private InputAction _keepBuildingAction;

        // Mode.
        private LineBase _mode;
        private LineMode _currentMode = LineMode.Point;
        private DragMode _dragMode = DragMode.None;

        // Tool settings.
        private SpacingMode _spacingMode = SpacingMode.Manual;
        private RotationMode _rotationMode = RotationMode.Relative;
        private float _spacing = 20f;
        private int _rotation = 0;
        private float _randomSpacing = 0f;
        private float _randomOffset = 0f;
        private bool _dirty = false;

        /// <summary>
        /// Point dragging mode.
        /// </summary>
        internal enum DragMode
        {
            /// <summary>
            /// No dragging.
            /// </summary>
            None = 0,

            /// <summary>
            /// Dragging the line's start position.
            /// </summary>
            StartPos,

            /// <summary>
            /// Dragging the line's end position.
            /// </summary>
            EndPos,

            /// <summary>
            /// Dragging the line's elbow position.
            /// </summary>
            ElbowPos,
        }

        /// <summary>
        /// Gets the tool's ID string.
        /// </summary>
        public override string toolID => "Line Tool";

        /// <summary>
        /// Gets the active instance.
        /// </summary>
        internal static LineToolSystem Instance { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether snap-to-length is enabled.
        /// </summary>
        internal bool LengthSnapEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the random seed should be randomised <c>true</c> or kept constant <c>false</c>.
        /// </summary>
        internal bool RandomizationEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the effective line spacing.
        /// </summary>
        internal float Spacing
        {
            get => _spacing;

            set
            {
                // Don't allow spacing to be set smaller than the smallest side of zBounds.
                _spacing = (float)Math.Round(math.max(value, math.max(math.abs(_zBounds.max), math.abs(_zBounds.min) + 0.1f)), 1);
                _dirty = true;
            }
        }

        /// <summary>
        /// Gets the effective spacing value, taking into account fence mode.
        /// </summary>
        internal float EffectiveSpacing
        {
            get
            {
                // Check for fence or wall-to-wall modes, ensuring that they're valid options for this prefab.
                if (FenceModeValid && _spacingMode == SpacingMode.FenceMode)
                {
                    return _zBounds.max - _zBounds.min;
                }
                else if (W2WModeValid && _spacingMode == SpacingMode.W2WMode)
                {
                    return _xBounds.max - _xBounds.min;
                }

                // If we got here we're not in fence or wall-to-wall mode; just return the spacing value.
                return _spacing;
            }
        }

        /// <summary>
        /// Gets a value indicating whether fence mode is valid for the selected prefab.
        /// </summary>
        internal bool FenceModeValid => _selectedPrefab is not null && (_zBounds.min != 0 || _zBounds.max != 0);

        /// <summary>
        /// Gets a value indicating whether wall-to-wall mode is valid for the selected prefab.
        /// </summary>
        internal bool W2WModeValid => _selectedPrefab is not null && _xBounds.min != 0 && _xBounds.max != 0;

        /// <summary>
        /// Gets or sets the current spacing mode.
        /// </summary>
        internal SpacingMode CurrentSpacingMode
        {
            get
            {
                // Use normal spacing mode if fence or wall-to-wall is selected but that mode isn't currently a valid option.
                if ((!FenceModeValid && _spacingMode == SpacingMode.FenceMode) || (!W2WModeValid && _spacingMode == SpacingMode.W2WMode))
                {
                    return SpacingMode.Manual;
                }

                return _spacingMode;
            }

            set
            {
                // Don't do anything if no change.
                if (_spacingMode != value)
                {
                    // Don't do anything if trying to set either fence or wall-to-wall mode if that mode isn't currently a valid option.
                    if ((!FenceModeValid && value == SpacingMode.FenceMode) || (!W2WModeValid && value == SpacingMode.W2WMode))
                    {
                        return;
                    }

                    _spacingMode = value;
                    _dirty = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the current rotation mode.
        /// </summary>
        internal RotationMode CurrentRotationMode
        {
            get
            {
                // Always use relative rotation for fence or wall-to-wall mode.
                if (CurrentSpacingMode == SpacingMode.FenceMode || CurrentSpacingMode == SpacingMode.W2WMode)
                {
                    return RotationMode.Relative;
                }

                return _rotationMode;
            }

            set
            {
                // Don't do anything if no change.
                if (_rotationMode != value)
                {
                    _rotationMode = value;
                    _dirty = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the random spacing offset maximum.
        /// </summary>
        internal float RandomSpacing
        {
            get => _randomSpacing;
            set
            {
                // Don't do anything if no change.
                if (_randomSpacing != value)
                {
                    // Don't allow negative numbers.
                    _randomSpacing = math.max(value, 0f);
                    _dirty = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the random lateral offset maximum.
        /// </summary>
        internal float RandomOffset
        {
            get => _randomOffset;
            set
            {
                // Don't do anything if no change.
                if (_randomOffset != value)
                {
                    // Don't allow negative numbers.
                    _randomOffset = math.max(value, 0f);
                    _dirty = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the rotation setting.
        /// </summary>
        internal int Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;

                // Bounds checks.
                if (_rotation >= 360)
                {
                    _rotation -= 360;
                }

                if (_rotation < 0)
                {
                    _rotation += 360;
                }

                _dirty = true;
            }
        }

        /// <summary>
        /// Gets the tooltip list.
        /// </summary>
        internal List<TooltipInfo> Tooltips => _tooltips;

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

                // Get current game guideline settings.
                GuideLineSettingsData guideLineSettings = _renderingSettingsQuery.GetSingleton<GuideLineSettingsData>();

                // Apply updated tool mode.
                switch (value)
                {
                    case LineMode.Straight:
                        _mode = new StraightLine(_mode, guideLineSettings.m_HighPriorityColor, guideLineSettings.m_MediumPriorityColor, _objectToolSystem.distanceScale);
                        break;
                    case LineMode.SimpleCurve:
                        _mode = new SimpleCurve(_mode, guideLineSettings.m_HighPriorityColor, guideLineSettings.m_MediumPriorityColor, _objectToolSystem.distanceScale);
                        break;
                    case LineMode.Circle:
                        _mode = new Circle(_mode, guideLineSettings.m_HighPriorityColor, guideLineSettings.m_MediumPriorityColor, _objectToolSystem.distanceScale);
                        break;
                    case LineMode.Grid:
                        _mode = new GridLines(_mode, guideLineSettings.m_HighPriorityColor, guideLineSettings.m_MediumPriorityColor, _objectToolSystem.distanceScale);
                        break;
                }

                // Update mode.
                _currentMode = value;
            }
        }

        /// <summary>
        /// Gets the currently selected entity.
        /// </summary>
        internal Entity SelectedEntity => _selectedEntity;

        /// <summary>
        /// Gets a value indicating whether a tree prefab (with age stages) is currently selected.
        /// </summary>
        internal bool TreeSelected => m_PrefabSystem.HasComponent<TreeData>(_selectedPrefab);

        /// <summary>
        /// Gets or sets the active tree age mask.
        /// </summary>
        internal AgeMask AgeMask { get; set; }

        /// <summary>
        /// Gets or sets guideline transparency (inverse alpha) for guideline drawing.
        /// </summary>
        internal float GuidelineTransparency { get; set; }

        /// <summary>
        /// Sets the currently selected prefab.
        /// </summary>
        private PrefabBase SelectedPrefab
        {
            set
            {
                _selectedPrefab = value as ObjectGeometryPrefab;

                // Update selected entity.
                if (_selectedPrefab is null)
                {
                    // No valid entity selected.
                    _selectedEntity = Entity.Null;
                }
                else
                {
                    // Get selected entity.
                    _selectedEntity = m_PrefabSystem.GetEntity(_selectedPrefab);

                    // Check bounds.
                    _zBounds.min = 0f;
                    _zBounds.max = 0f;
                    _xBounds.min = 0f;
                    _xBounds.max = 0f;
                    foreach (ObjectMeshInfo mesh in _selectedPrefab.m_Meshes)
                    {
                        if (mesh.m_Mesh is RenderPrefab renderPrefab)
                        {
                            // Update bounds if either of the relevant extents of this mesh exceed the previous extent.
                            _xBounds.min = math.min(_xBounds.min, renderPrefab.bounds.x.min);
                            _xBounds.max = math.max(_xBounds.max, renderPrefab.bounds.x.max);
                            _zBounds.min = math.min(_zBounds.min, renderPrefab.bounds.z.min);
                            _zBounds.max = math.max(_zBounds.max, renderPrefab.bounds.z.max);
                        }
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
        public override PrefabBase GetPrefab() => _selectedPrefab;

        /// <summary>
        /// Sets the prefab selected by this tool.
        /// </summary>
        /// <param name="prefab">Prefab to set.</param>
        /// <returns><c>true</c> if Line Tool supports this prefab, <c>false</c> otherwise.</returns>
        public override bool TrySetPrefab(PrefabBase prefab)
        {
            if (m_ToolSystem.activeTool == this && prefab is ObjectGeometryPrefab objectGeometryPrefab)
            {
                SelectedPrefab = objectGeometryPrefab;
                return true;
            }

            // If we got here, the prefab isn't supported by Line Tool.
            return false;
        }

        /// <summary>
        /// Elevation-up key handler; used to increment spacing.
        /// </summary>
        public override void ElevationUp() => Spacing = _spacing + 1;

        /// <summary>
        /// Elevation-down key handler; used to decrement spacing.
        /// </summary>
        public override void ElevationDown() => Spacing = _spacing - 1;

        /// <summary>
        /// Gets the snap mask for this tool.
        /// </summary>
        /// <param name="onMask">Snap on mask.</param>
        /// <param name="offMask">Snap off mask.</param>
        public override void GetAvailableSnapMask(out Snap onMask, out Snap offMask)
        {
            base.GetAvailableSnapMask(out onMask, out offMask);
            onMask |= Snap.ContourLines;
            offMask |= Snap.ContourLines;
        }

        /// <summary>
        /// Enables the tool (called by hotkey action).
        /// </summary>
        internal void EnableTool()
        {
            // Activate this tool if it isn't already active.
            if (m_ToolSystem.activeTool != this)
            {
                _previousTool = m_ToolSystem.activeTool;

                // Check for valid prefab selection before continuing.
                ObjectToolSystem objectToolSystem = World.GetOrCreateSystemManaged<ObjectToolSystem>();
                SelectedPrefab = objectToolSystem.prefab;
                if (_selectedPrefab != null)
                {
                    // Valid prefab selected - switch to this tool.
                    m_ToolSystem.selected = Entity.Null;
                    m_ToolSystem.activeTool = this;

                    // Update the current tree age mask from the game's object tool.
                    AgeMask = objectToolSystem.actualAgeMask;
                }
            }
        }

        /// <summary>
        /// Restores the previously-used tool.
        /// </summary>
        internal void RestorePreviousTool()
        {
            if (_previousTool is not null)
            {
                m_ToolSystem.activeTool = _previousTool;
                _currentMode = LineMode.Point;
            }
            else
            {
                _log.Error("null tool set when restoring previous tool");
            }
        }

        /// <summary>
        /// Updates the internal random seed.
        /// </summary>
        internal void UpdateRandomSeed()
        {
            ++_fixedRandomSeed;

            // Reset seed timer to force update.
            _previousSeedTime = 0f;
        }

        /// <summary>
        /// Called when the system is created.
        /// </summary>
        protected override void OnCreate()
        {
            // Set instance reference.
            Instance = this;

            base.OnCreate();

            // Set log.
            _log = Mod.Instance.Log;

            // Try to find Line Tool reference from tool system tool list.
            List<ToolBaseSystem> toolList = World.GetOrCreateSystemManaged<ToolSystem>().tools;
            ToolBaseSystem thisSystem = null;
            foreach (ToolBaseSystem tool in toolList)
            {
                _log.Debug($"Got tool {tool.toolID} ({tool.GetType().FullName})");
                if (tool == this)
                {
                    _log.Debug("Found Line Tool reference in tool list");
                    thisSystem = tool;
                    continue;
                }
            }

            // Remove existing tool reference.
            if (thisSystem is not null)
            {
                toolList.Remove(this);
            }

            // Insert Line Tool at the start of the tool list, unless Tree Controller is there, in which case insert it at position 1.
            if (toolList[0].toolID.Equals("Tree Controller Tool"))
            {
                _log.Info("Found Tree Controller reference in tool list at position 0");
                toolList.Insert(1, this);
            }
            else
            {
                toolList.Insert(0, this);
            }

#if DEBUG
            foreach (ToolBaseSystem tool in toolList)
            {
                _log.Debug($"Got tool {tool.toolID} {tool.GetType().FullName}");
            }
#endif

            // Get system references.
            _terrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
            _overlayBuffer = World.GetOrCreateSystemManaged<OverlayRenderSystem>().GetBuffer(out var _);
            _cityConfigurationSystem = World.GetOrCreateSystemManaged<CityConfigurationSystem>();
            _objectToolSystem = World.GetOrCreateSystemManaged<ObjectToolSystem>();
            _audioManager = World.GetOrCreateSystemManaged<AudioManager>();

            // Set up settings queries.
            _renderingSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<GuideLineSettingsData>());
            _soundEffectsQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());

            // Create buffers.
            _tooltips = new (8);
            _points = new ();

            // Create random seed list with one initial default starting randomizer.
            _randomSeeds = new () { default };

            // Set straight line mode as initial mode, using current game settings.
            GuideLineSettingsData guideLineSettings = _renderingSettingsQuery.GetSingleton<GuideLineSettingsData>();
            _mode = new StraightLine(guideLineSettings.m_HighPriorityColor, guideLineSettings.m_MediumPriorityColor, _objectToolSystem.distanceScale);

            // Enable fixed preview control.
            _fixedPreviewAction = new ("LineTool-FixPreview");
            _fixedPreviewAction.AddCompositeBinding("ButtonWithOneModifier").With("Modifier", "<Keyboard>/ctrl").With("Button", "<Mouse>/leftButton");
            _fixedPreviewAction.Enable();

            // Enable keep building action.
            _keepBuildingAction = new ("LineTool-KeepBuilding");
            _keepBuildingAction.AddCompositeBinding("ButtonWithOneModifier").With("Modifier", "<Keyboard>/shift").With("Button", "<Mouse>/leftButton");
            _keepBuildingAction.Enable();

            // Set guideline transparency.
            GuidelineTransparency = Mod.Instance.ActiveSettings.GuidelineTransparency;
        }

        /// <summary>
        /// Called every tool update.
        /// </summary>
        /// <param name="inputDeps">Input dependencies.</param>
        /// <returns>Job handle.</returns>
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // Clear state.
            applyMode = ApplyMode.Clear;
            _tooltips.Clear();

            // Don't do anything if no selected prefab or we're in point mode.
            if (_selectedPrefab is null || _currentMode == LineMode.Point)
            {
                return inputDeps;
            }

            // Check for valid raycast.
            float3 position = _fixedPreview ? _fixedPos : _previousPos;
            if (GetRaycastResult(out _raycastPoint))
            {
                // Valid raycast - update position, applying any constraints.
                position = _fixedPreview ? _fixedPos : _mode.ConstrainPos(_raycastPoint.m_HitPosition, EffectiveSpacing, LengthSnapEnabled);

                // Calculate terrain height.
                _terrainHeightData = _terrainSystem.GetHeightData();
                position.y = TerrainUtils.SampleHeight(ref _terrainHeightData, position);

                // Handle any dragging.
                if (_dragMode != DragMode.None)
                {
                    if (applyAction.WasReleasedThisFrame() || _fixedPreviewAction.WasReleasedThisFrame())
                    {
                        // Cancel dragging.
                        _dragMode = DragMode.None;
                    }
                    else
                    {
                        // Drag end point.
                        if (_dragMode == DragMode.EndPos)
                        {
                            position = _raycastPoint.m_HitPosition;
                            _fixedPos = position;
                        }
                        else
                        {
                            // Handle dragging for other points via line mode instance.
                            _mode.HandleDrag(_dragMode, _raycastPoint.m_HitPosition);
                        }

                        _dirty = true;
                    }
                }

                // Check for and perform any cancellation.
                if (cancelAction.WasPressedThisFrame())
                {
                    // Reset current mode settings.
                    _mode.Reset();
                    _dragMode = DragMode.None;
                    _fixedPreview = false;

                    return inputDeps;
                }
                else
                {
                    // Check for apply action initiation.
                    bool applyWasPressed = applyAction.WasPressedThisFrame();

                    // If no cancellation, handle any fixed preview action if we're ready to place.
                    if (applyWasPressed && Keyboard.current.ctrlKey.isPressed && _mode.HasAllPoints)
                    {
                        // Are we already in fixed preview mode?
                        if (_fixedPreview)
                        {
                            // Already in fixed preview mode - check for dragging hits.
                            _dragMode = _mode.CheckDragHit(_raycastPoint.m_HitPosition);
                            if (_dragMode != DragMode.None)
                            {
                                // If dragging, has started, then we're done here.
                                return inputDeps;
                            }
                        }
                        else
                        {
                            // Activate fixed preview mode and fix current position.
                            _fixedPreview = true;
                            _fixedPos = position;
                        }
                    }

                    // Handle basic apply action if no other actions.
                    else if (applyWasPressed || _keepBuildingAction.WasPressedThisFrame())
                    {
                        // Were we in fixed state?
                        if (_fixedPreview)
                        {
                            // Check for dragging hits.
                            _dragMode = _mode.CheckDragHit(_raycastPoint.m_HitPosition);
                            if (_dragMode != DragMode.None)
                            {
                                // If dragging, has started, then we're done here.
                                return inputDeps;
                            }

                            // Yes - cancel fixed preview.
                            _fixedPreview = false;
                        }

                        // Handle click.
                        switch (_mode.HandleClick(position))
                        {
                            // Initial click.
                            case ClickMode.Initial:
                                _audioManager.PlayUISound(_soundEffectsQuery.GetSingleton<ToolUXSoundSettingsData>().m_NetStartSound);
                                break;

                            // Placing items.
                            case ClickMode.Placed:
                                applyMode = ApplyMode.Apply;

                                // Play relevant sound effect for placement (if any).
                                if (_selectedPrefab is BuildingPrefab)
                                {
                                    _audioManager.PlayUISound(_soundEffectsQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlaceBuildingSound);
                                }
                                else if (_selectedPrefab is StaticObjectPrefab || m_ToolSystem.actionMode.IsEditor())
                                {
                                    _audioManager.PlayUISound(_soundEffectsQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlacePropSound);
                                }

                                // Perform post-placement.
                                _mode.ItemsPlaced();

                                // Reset tool mode if we're not building continuously.
                                if (!Keyboard.current.shiftKey.isPressed)
                                {
                                    _mode.Reset();
                                }

                                return inputDeps;

                            // Midpoint click.
                            case ClickMode.Midpoint:
                                _audioManager.PlayUISound(_soundEffectsQuery.GetSingleton<ToolUXSoundSettingsData>().m_NetNodeSound);
                                break;
                        }
                    }
                }

                // Update cursor entity if we haven't got an initial position set.
                if (!_mode.HasStart)
                {
                    // Don't update if the cursor hasn't moved.
                    if (position.x != _previousPos.x || position.z != _previousPos.z)
                    {
                        // Create cursor entity.
                        CreateDefinitions(
                            _selectedEntity,
                            position,
                            GetEffectiveRotation(position),
                            GetRandomSeed(0));

                        // Update previous position.
                        _previousPos = position;
                    }
                    else
                    {
                        // Cursor hasn't moved - just keep what we've got.
                        applyMode = ApplyMode.None;
                    }

                    return inputDeps;
                }
            }

            // Render any overlay (inverting transparency to alpha).
            _mode.DrawOverlay(1f - GuidelineTransparency, _overlayBuffer, _tooltips);

            // Overlay control points.
            if (_fixedPreview)
            {
                _mode.DrawPointOverlays(_overlayBuffer);
            }

            // Check for position change or update needed.
            if (!_dirty && position.x == _previousPos.x && position.z == _previousPos.y)
            {
                // No update needed.
                applyMode = ApplyMode.None;
                return inputDeps;
            }

            // Update stored position and clear dirty flag.
            _previousPos = position;
            _dirty = false;

            // If we got here we're (re)calculating points.
            _points.Clear();
            _mode.CalculatePoints(position, CurrentSpacingMode, CurrentRotationMode, EffectiveSpacing, RandomSpacing, RandomOffset, _rotation, _zBounds, _points, ref _terrainHeightData);

            // Initialize randomization for this run.
            RandomSeed randomSeed = GetRandomSeed(0);
            int seedIndex = 0;
            while (_randomSeeds.Count < _points.Count)
            {
                _randomSeeds.Add(RandomSeed.Next());
            }

            // Step along length and place preview objects.
            foreach (PointData thisPoint in _points)
            {
                UnityEngine.Random.InitState((int)(thisPoint.Position.x + thisPoint.Position.y + thisPoint.Position.z));

                // Create transform component.
                Transform transformData = new ()
                {
                    m_Position = thisPoint.Position,
                    m_Rotation = CurrentRotationMode == RotationMode.Random ? GetEffectiveRotation(thisPoint.Position) : thisPoint.Rotation,
                };

                // Create entity.
                CreateDefinitions(
                    _selectedEntity,
                    thisPoint.Position,
                    CurrentRotationMode == RotationMode.Random ? GetEffectiveRotation(thisPoint.Position) : thisPoint.Rotation,
                    CurrentSpacingMode == SpacingMode.FenceMode ? randomSeed : RandomizationEnabled ? GetRandomSeed(seedIndex++) : GetRandomSeed(0));
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

            // Ensure relevant base actions are enabled.
            applyAction.enabled = true;
            cancelAction.enabled = true;

            // Clear any previous raycast result.
            _raycastPoint = default;

            // Reset any previously-stored starting position.
            _mode.Reset();

            // Clear any applications.
            applyMode = ApplyMode.Clear;

            // Manually enable base action if in editor mode.
            if (m_ToolSystem.actionMode.IsEditor())
            {
               applyAction.enabled = true;
            }
        }

        /// <summary>
        /// Called when the tool stops running.
        /// </summary>
        protected override void OnStopRunning()
        {
            _log.Debug("OnStopRunning");

            // Clear tooltips.
            _tooltips.Clear();

            // Reset state.
            _mode.Reset();

            // Disable base action if in editor mode.
            if (m_ToolSystem.actionMode.IsEditor())
            {
                applyAction.enabled = false;
            }

            base.OnStopRunning();
        }

        /// <summary>
        /// Gets the effective object rotation depending on current settings.
        /// </summary>
        /// <param name="position">Object position (to seed random number generator).</param>
        /// <returns>Effective rotation quaternion according to current settings.</returns>
        private quaternion GetEffectiveRotation(float3 position)
        {
            int rotation = _rotation;

            // Override fixed rotation with a random value if we're using random rotation and fence mode isn't selected.
            if (CurrentRotationMode == RotationMode.Random)
            {
                // Use position to init RNG.
                _random.InitState((uint)(math.abs(position.x) + math.abs(position.y) + math.abs(position.z)) * 10000);
                rotation = _random.NextInt(360);
            }

            // Generate return quaternion.
            return quaternion.Euler(0, math.radians(rotation), 0);
        }

        /// <summary>
        /// Gets a current random seed in a sequence with seed updates limited to one every two seconds.
        /// </summary>
        /// <param name="seedIndex">Index of seed to use.</param>
        /// <returns>Current random seed for the given index.</returns>
        private RandomSeed GetRandomSeed(int seedIndex)
        {
            int listMax = _randomSeeds.Count - 1;
            if (RandomizationEnabled)
            {
                // Update the stored random seeds if at least two seconds have passed since the last update.
                float currentTime = UnityEngine.Time.time;
                if (currentTime - _previousSeedTime > 2f)
                {
                    // Update seeds.
                    for (int i = 0; i <= listMax; ++i)
                    {
                        _randomSeeds[i] = RandomSeed.Next();
                    }

                    // Store new 'last updated' time.
                    _previousSeedTime = currentTime;
                }
            }

            // Bounds check index before returning the seed index.
            return _randomSeeds[UnityEngine.Mathf.Clamp(seedIndex, 0, listMax)];
        }

        /// <summary>
        /// Creates temporary object definitions for previewing.
        /// </summary>
        /// <param name="objectPrefab">Object prefab entity.</param>
        /// <param name="position">Entity position.</param>
        /// <param name="rotation">Entity rotation.</param>
        /// <param name="randomSeed">Random seed to use.</param>
        private void CreateDefinitions(Entity objectPrefab, float3 position, quaternion rotation, RandomSeed randomSeed)
        {
            CreateDefinitions definitions = default;
            definitions.m_RandomizationEnabled = RandomizationEnabled;
            definitions.m_FixedRandomSeed = RandomizationEnabled ? 0 : _fixedRandomSeed;
            definitions.m_EditorMode = m_ToolSystem.actionMode.IsEditor();
            definitions.m_LefthandTraffic = _cityConfigurationSystem.leftHandTraffic;
            definitions.m_ObjectPrefab = objectPrefab;
            definitions.m_Theme = _cityConfigurationSystem.defaultTheme;
            definitions.m_RandomSeed = randomSeed;
            definitions.m_AgeMask = AgeMask;
            definitions.m_ControlPoint = new () { m_Position = position, m_Rotation = rotation };
            definitions.m_AttachmentPrefab = default;
            definitions.m_OwnerData = SystemAPI.GetComponentLookup<Owner>(true);
            definitions.m_TransformData = SystemAPI.GetComponentLookup<Transform>(true);
            definitions.m_AttachedData = SystemAPI.GetComponentLookup<Attached>(true);
            definitions.m_LocalTransformCacheData = SystemAPI.GetComponentLookup<LocalTransformCache>(true);
            definitions.m_ElevationData = SystemAPI.GetComponentLookup<Game.Objects.Elevation>(true);
            definitions.m_BuildingData = SystemAPI.GetComponentLookup<Building>(true);
            definitions.m_LotData = SystemAPI.GetComponentLookup<Game.Buildings.Lot>(true);
            definitions.m_EdgeData = SystemAPI.GetComponentLookup<Edge>(true);
            definitions.m_NodeData = SystemAPI.GetComponentLookup<Game.Net.Node>(true);
            definitions.m_CurveData = SystemAPI.GetComponentLookup<Curve>(true);
            definitions.m_NetElevationData = SystemAPI.GetComponentLookup<Game.Net.Elevation>(true);
            definitions.m_OrphanData = SystemAPI.GetComponentLookup<Orphan>(true);
            definitions.m_UpgradedData = SystemAPI.GetComponentLookup<Upgraded>(true);
            definitions.m_CompositionData = SystemAPI.GetComponentLookup<Composition>(true);
            definitions.m_AreaClearData = SystemAPI.GetComponentLookup<Clear>(true);
            definitions.m_AreaSpaceData = SystemAPI.GetComponentLookup<Space>(true);
            definitions.m_AreaLotData = SystemAPI.GetComponentLookup<Game.Areas.Lot>(true);
            definitions.m_EditorContainerData = SystemAPI.GetComponentLookup<Game.Tools.EditorContainer>(true);
            definitions.m_PrefabRefData = SystemAPI.GetComponentLookup<PrefabRef>(true);
            definitions.m_PrefabNetObjectData = SystemAPI.GetComponentLookup<NetObjectData>(true);
            definitions.m_PrefabBuildingData = SystemAPI.GetComponentLookup<BuildingData>(true);
            definitions.m_PrefabAssetStampData = SystemAPI.GetComponentLookup<AssetStampData>(true);
            definitions.m_PrefabBuildingExtensionData = SystemAPI.GetComponentLookup<BuildingExtensionData>(true);
            definitions.m_PrefabSpawnableObjectData = SystemAPI.GetComponentLookup<SpawnableObjectData>(true);
            definitions.m_PrefabObjectGeometryData = SystemAPI.GetComponentLookup<ObjectGeometryData>(true);
            definitions.m_PrefabPlaceableObjectData = SystemAPI.GetComponentLookup<PlaceableObjectData>(true);
            definitions.m_PrefabAreaGeometryData = SystemAPI.GetComponentLookup<AreaGeometryData>(true);
            definitions.m_PrefabBuildingTerraformData = SystemAPI.GetComponentLookup<BuildingTerraformData>(true);
            definitions.m_PrefabCreatureSpawnData = SystemAPI.GetComponentLookup<CreatureSpawnData>(true);
            definitions.m_PlaceholderBuildingData = SystemAPI.GetComponentLookup<PlaceholderBuildingData>(true);
            definitions.m_PrefabNetGeometryData = SystemAPI.GetComponentLookup<NetGeometryData>(true);
            definitions.m_PrefabCompositionData = SystemAPI.GetComponentLookup<NetCompositionData>(true);
            definitions.m_SubObjects = SystemAPI.GetBufferLookup<Game.Objects.SubObject>(true);
            definitions.m_CachedNodes = SystemAPI.GetBufferLookup<LocalNodeCache>(true);
            definitions.m_InstalledUpgrades = SystemAPI.GetBufferLookup<InstalledUpgrade>(true);
            definitions.m_SubNets = SystemAPI.GetBufferLookup<Game.Net.SubNet>(true);
            definitions.m_ConnectedEdges = SystemAPI.GetBufferLookup<ConnectedEdge>(true);
            definitions.m_SubAreas = SystemAPI.GetBufferLookup<Game.Areas.SubArea>(true);
            definitions.m_AreaNodes = SystemAPI.GetBufferLookup<Game.Areas.Node>(true);
            definitions.m_AreaTriangles = SystemAPI.GetBufferLookup<Triangle>(true);
            definitions.m_PrefabSubObjects = SystemAPI.GetBufferLookup<Game.Prefabs.SubObject>(true);
            definitions.m_PrefabSubNets = SystemAPI.GetBufferLookup<Game.Prefabs.SubNet>(true);
            definitions.m_PrefabSubLanes = SystemAPI.GetBufferLookup<Game.Prefabs.SubLane>(true);
            definitions.m_PrefabSubAreas = SystemAPI.GetBufferLookup<Game.Prefabs.SubArea>(true);
            definitions.m_PrefabSubAreaNodes = SystemAPI.GetBufferLookup<SubAreaNode>(true);
            definitions.m_PrefabPlaceholderElements = SystemAPI.GetBufferLookup<PlaceholderObjectElement>(true);
            definitions.m_PrefabRequirementElements = SystemAPI.GetBufferLookup<ObjectRequirementElement>(true);
            definitions.m_PrefabServiceUpgradeBuilding = SystemAPI.GetBufferLookup<ServiceUpgradeBuilding>(true);
            definitions.m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out var _);
            definitions.m_TerrainHeightData = m_TerrainSystem.GetHeightData();
            definitions.m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
            definitions.Execute();
        }
    }
}
