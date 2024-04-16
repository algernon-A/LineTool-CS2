// <copyright file="Circle.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the Apache Licence, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineTool
{
    using System.Collections.Generic;
    using Colossal.Mathematics;
    using Game.Net;
    using Game.Rendering;
    using Game.Simulation;
    using Unity.Mathematics;
    using static Game.Rendering.GuideLinesSystem;

    /// <summary>
    ///  Circle placement mode.
    /// </summary>
    public class Circle : LineBase
    {
        // Calculated circle Bezier parts.
        private Bezier4x3[] _thisCircleBeziers = new Bezier4x3[4];

        /// <summary>
        /// Initializes a new instance of the <see cref="Circle"/> class.
        /// </summary>
        /// <param name="mode">Mode to copy starting state from.</param>
        public Circle(LineBase mode)
            : base(mode)
        {
        }

        /// <summary>
        /// Calculates the points to use based on this mode.
        /// </summary>
        /// <param name="currentPos">Selection current position.</param>
        /// <param name="spacingMode">Active spacing mode.</param>
        /// <param name="rotationMode">Active rotation mode.</param>
        /// <param name="spacing">Spacing distance.</param>
        /// <param name="randomSpacing">Random spacing offset maximum.</param>
        /// <param name="randomOffset">Random lateral offset maximum.</param>
        /// <param name="rotation">Rotation setting.</param>
        /// <param name="zBounds">Prefab zBounds.</param>
        /// <param name="pointList">List of points to populate.</param>
        /// <param name="heightData">Terrain height data reference.</param>
        public override void CalculatePoints(float3 currentPos, SpacingMode spacingMode, RotationMode rotationMode, float spacing, float randomSpacing, float randomOffset, int rotation, Bounds1 zBounds, List<PointData> pointList, ref TerrainHeightData heightData)
        {
            // Don't do anything if we don't have valid start.
            if (!m_validStart)
            {
                return;
            }

            // Calculate length.
            float3 difference = currentPos - m_startPos;
            float radius = math.length(difference);

            // Calculate circle bezier by combining 4 curved parts.
            _thisCircleBeziers[0] = NetUtils.CircleCurve(m_startPos, radius, radius);
            _thisCircleBeziers[1] = NetUtils.CircleCurve(m_startPos, radius * -1f, radius);
            _thisCircleBeziers[2] = NetUtils.CircleCurve(m_startPos, radius, radius * -1f);
            _thisCircleBeziers[3] = NetUtils.CircleCurve(m_startPos, radius * -1f, radius * -1f);

            // Calculate spacing.
            float circumference = radius * math.PI * 2f;
            float numPoints = spacingMode == SpacingMode.FullLength ? math.round(circumference / spacing) : math.floor(circumference / spacing);
            float increment = (math.PI * 2f) / numPoints;
            float startAngle = math.atan2(difference.z, difference.x);
            System.Random random = new ((int)circumference * 1000);

            // Create points.
            for (float i = startAngle; i < startAngle + (math.PI * 2f); i += increment)
            {
                // Apply spacing adjustment.
                float adjustedAngle = i;
                if (randomSpacing > 0f && spacingMode != SpacingMode.FenceMode)
                {
                    float distanceAdjustment = (float)(random.NextDouble() * randomSpacing * 2f) - randomSpacing;
                    adjustedAngle += (distanceAdjustment * math.PI * 2f) / circumference;
                }

                // Calculate point.
                float xPos = radius * math.cos(adjustedAngle);
                float yPos = radius * math.sin(adjustedAngle);
                float3 thisPoint = new (m_startPos.x + xPos, m_startPos.y, m_startPos.z + yPos);

                // Apply offset adjustment.
                if (randomOffset > 0f && spacingMode != SpacingMode.FenceMode)
                {
                    thisPoint += math.normalize(thisPoint - m_startPos) * ((float)(randomOffset * random.NextDouble() * 2f) - randomOffset);
                }

                thisPoint.y = TerrainUtils.SampleHeight(ref heightData, thisPoint);

                // Calculate effective rotation.
                float effectiveRotation = rotationMode == RotationMode.Absolute ? rotation : math.radians(rotation) - i;

                // Add point to list.
                pointList.Add(new PointData { Position = thisPoint, Rotation = quaternion.Euler(0f, effectiveRotation, 0f), });
            }

            // Record end position for overlays.
            m_endPos = currentPos;
        }

        /// <summary>
        /// Draws any applicable overlay.
        /// </summary>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        /// <param name="tooltips">Tooltip list.</param>
        /// <param name="cameraController">Active camera controller instance.</param>
        public override void DrawOverlay(OverlayRenderSystem.Buffer overlayBuffer, List<TooltipInfo> tooltips, CameraUpdateSystem cameraController)
        {
            // If points haven't been calculated yet, fallback to straight line.
            if (_thisCircleBeziers[0].a.x != 0f)
            {
                for (int i = 0; i < _thisCircleBeziers.Length; i++)
                {
                    DrawCurvedDashedLine(_thisCircleBeziers[i], overlayBuffer, cameraController);
                }

                // Keep straight line with distance tooltip.
                base.DrawOverlay(overlayBuffer, tooltips, cameraController);
            }
            else
            {
                // Initial position only; just draw a straight line (constrained if required).
                base.DrawOverlay(overlayBuffer, tooltips, cameraController);
            }
        }

        /// <summary>
        /// Performs actions after items are placed on the current line, setting up for the next line to be set.
        /// </summary>
        /// <param name="location">Click world location.</param>
        public override void ItemsPlaced(float3 location)
        {
            // Empty, to retain original start position (centre of circle).
        }
    }
}
