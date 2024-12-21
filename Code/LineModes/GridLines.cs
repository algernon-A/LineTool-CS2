// <copyright file="GridLines.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the Apache Licence, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineTool
{
    using System.Collections.Generic;
    using Colossal.Mathematics;
    using Game.Rendering;
    using Game.Simulation;
    using Unity.Mathematics;
    using UnityEngine;
    using static Game.Rendering.GuideLinesSystem;

    /// <summary>
    /// Grid placement mode.
    /// </summary>
    public class GridLines : ElbowBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridLines"/> class.
        /// </summary>
        public GridLines()
        {
            // Basic state.
            m_validStart = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridLines"/> class.
        /// </summary>
        /// <param name="mode">Mode to copy starting state from.</param>
        /// <param name="highPriorityColor">High priority line colour.</param>
        /// <param name="mediumPriorityColor">Medium priority line colour.</param>
        /// <param name="distanceScale">Line width distance scale.</param>
        public GridLines(LineBase mode, Color highPriorityColor, Color mediumPriorityColor, float distanceScale)
            : base(mode, highPriorityColor, mediumPriorityColor, distanceScale)
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

            // If we have a valid start but no valid elbow, just draw a straight line.
            if (!ValidElbow)
            {
                base.CalculatePoints(currentPos, spacingMode, rotationMode, spacing, randomSpacing, randomOffset, rotation, zBounds, pointList, ref heightData);
                return;
            }

            // Calculate line lengths.
            float3 baseLine = ElbowPoint - m_startPos;
            float baseLength = math.length(baseLine);
            float sideLength = math.length(currentPos - ElbowPoint);
            System.Random random = new ((int)baseLength * 1000);

            // Calculate base line angle (for absolute/relative rotation).
            float baseAngle = rotationMode == RotationMode.Absolute ? 0f : math.atan2(baseLine.x, baseLine.z);

            // Calculate applied rotation (in radians).
            float appliedRotation = spacingMode switch
            {
                SpacingMode.FenceMode => math.atan2(baseLine.x, baseLine.z),
                SpacingMode.W2WMode => math.atan2(baseLine.x, baseLine.z) + (math.PI / 2f),
                _ => math.radians(rotation) + baseAngle,
            };

            // Rotation quaternion.
            quaternion qRotation = quaternion.Euler(0f, appliedRotation, 0f);

            // Apply spacing setting to side spacing if fence or all-to-wall mode is active.
            float sideSpacing = spacing;
            if (spacingMode == SpacingMode.FenceMode || spacingMode == SpacingMode.W2WMode)
            {
                sideSpacing = LineToolSystem.Instance.Spacing;
            }

            // Calculate even full-length spacing if needed.
            if (spacingMode == SpacingMode.FullLength)
            {
                spacing = baseLength / math.round(baseLength / spacing);
                sideSpacing = sideLength / math.round(sideLength / sideSpacing);
            }

            // Calculate Lerp step sizes.
            float baseStep = spacing / baseLength;
            float sideStep = sideSpacing / sideLength;

            // Iterate through base and side lines for placement.
            for (float baseProportion = 0; baseProportion < 1.001f; baseProportion += baseStep)
            {
                for (float sideProportion = 0; sideProportion < 1.001f; sideProportion += sideStep)
                {
                    // Implement random spacing and/or offset.
                    float spacingAdjustment = 0f;
                    float offsetAdjustment = 0f;
                    if (spacingMode != SpacingMode.FenceMode && spacingMode != SpacingMode.W2WMode)
                    {
                        // Spacing is applied along the base line.
                        if (randomSpacing > 0f)
                        {
                            spacingAdjustment = ((float)(random.NextDouble() * randomSpacing * 2f) - randomSpacing) / baseLength;
                        }

                        // Offset is applied along the side line.
                        if (randomOffset > 0f)
                        {
                            offsetAdjustment = ((float)(random.NextDouble() * randomOffset * 2f) - randomOffset) / sideLength;
                        }
                    }

                    // Skip any placement that's outside of the grid area.
                    float baseLerp = baseProportion + spacingAdjustment;
                    float sideLerp = sideProportion + offsetAdjustment;
                    if (baseLerp < 0f || baseLerp > 1f || sideLerp < 0f || sideLerp > 1f)
                    {
                        continue;
                    }

                    // Calculate proportional point on both the base and side point.
                    float3 basePoint = math.lerp(m_startPos, ElbowPoint, baseLerp);
                    float3 sidePoint = math.lerp(ElbowPoint, currentPos, sideLerp);

                    // Extrapolate from base point parallel to side line.
                    float3 thisPoint = basePoint + sidePoint - ElbowPoint;

                    // Get terrain height for this point.
                    thisPoint.y = TerrainUtils.SampleHeight(ref heightData, thisPoint);

                    // Add point to list.
                    pointList.Add(new PointData
                    {
                        Position = thisPoint,
                        Rotation = qRotation,
                    });
                }
            }

            // Record end position for overlays.
            m_endPos = currentPos;
        }

        /// <summary>
        /// Draws any applicable overlay.
        /// </summary>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        /// <param name="tooltips">Tooltip list.</param>
        public override void DrawOverlay(OverlayRenderSystem.Buffer overlayBuffer, List<TooltipInfo> tooltips)
        {
            if (m_validStart)
            {
                // Draw an elbow overlay if we've got valid starting and elbow positions.
                if (ValidElbow)
                {
                    // Calculate lines.
                    Line3.Segment line1 = new (m_startPos, ElbowPoint);
                    Line3.Segment line2 = new (ElbowPoint, m_endPos);

                    // Draw lines.
                    DrawControlLine(m_startPos, ElbowPoint, line1, overlayBuffer, tooltips);
                    DrawControlLine(ElbowPoint, m_endPos, line2, overlayBuffer, tooltips);

                    // Draw angle.
                    DrawAngleIndicator(line1, line2, overlayBuffer, tooltips);
                }
                else
                {
                    // Initial position only; just draw a straight line (constrained if required).
                    base.DrawOverlay(overlayBuffer, tooltips);
                }
            }
        }
    }
}
