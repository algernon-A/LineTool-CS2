// <copyright file="LineModeBase.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// </copyright>

namespace LineTool
{
    using System.Collections.Generic;
    using Game.Rendering;
    using Game.Simulation;
    using Unity.Mathematics;

    /// <summary>
    /// Line placement mode.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Protected fields")]
    public abstract class LineModeBase
    {
        /// <summary>
        /// Indicates whether a valid starting position has been recorded.
        /// </summary>
        protected bool m_validStart;

        /// <summary>
        /// Records the current selection start position.
        /// </summary>
        protected float3 m_startPos;

        /// <summary>
        /// Initializes a new instance of the <see cref="LineModeBase"/> class.
        /// </summary>
        public LineModeBase()
        {
            // Basic state.
            m_validStart = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineModeBase"/> class.
        /// </summary>
        /// <param name="mode">Mode to copy starting state from.</param>
        public LineModeBase(LineModeBase mode)
        {
            m_validStart = mode.m_validStart;
            m_startPos = mode.m_startPos;
        }

        /// <summary>
        /// Gets a value indicating whether a valid starting position has been recorded.
        /// </summary>
        public bool HasStart => m_validStart;

        /// <summary>
        /// Handles a mouse click.
        /// </summary>
        /// <param name="position">Click world position.</param>
        /// <returns><c>true</c> if items are to be placed as a result of this click, <c>false</c> otherwise.</returns>
        public virtual bool HandleClick(float3 position)
        {
            // If no valid start position is set, record it.
            if (!m_validStart)
            {
                m_validStart = true;
                m_startPos = position;

                // No placement at this stage (only the first click has been made).
                return false;
            }

            // Second click; we're placing items.
            return true;
        }

        /// <summary>
        /// Performs actions after items are placed on the current line, setting up for the next line to be set.
        /// </summary>
        /// <param name="position">Click world position.</param>
        public virtual void ItemsPlaced(float3 position)
        {
            // Update new starting location to the previous end point.
            m_startPos = position;
        }

        /// <summary>
        /// Calculates the points to use based on this mode.
        /// </summary>
        /// <param name="currentPos">Selection current position.</param>
        /// <param name="spacing">Spacing setting.</param>
        /// <param name="rotation">Rotation setting.</param>
        /// <param name="pointList">List of points to populate.</param>
        /// <param name="heightData">Terrain height data reference.</param>
        public virtual void CalculatePoints(float3 currentPos, float spacing, float rotation, List<PointData> pointList, ref TerrainHeightData heightData)
        {
            // Don't do anything if we don't have a valid start point.
            if (!m_validStart)
            {
                return;
            }

            // Calculate length.
            float length = math.length(currentPos - m_startPos);

            // Create points.
            float currentDistance = 0f;
            while (currentDistance < length)
            {
                // Calculate interpolated point.
                float3 thisPoint = math.lerp(m_startPos, currentPos, currentDistance / length);

                // Calculate terrain height.
                thisPoint.y = TerrainUtils.SampleHeight(ref heightData, thisPoint);

                // Add point to list.
                pointList.Add(new PointData { Position = thisPoint, Rotation = quaternion.identity, });
                currentDistance += spacing;
            }
        }

        /// <summary>
        /// Draws any applicable overlay.
        /// </summary>
        /// <param name="currentPos">Current cursor world position.</param>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        public virtual void DrawOverlay(float3 currentPos, OverlayRenderSystem.Buffer overlayBuffer)
        {
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public virtual void Reset()
        {
            m_validStart = false;
        }
    }
}
