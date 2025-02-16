﻿// <copyright file="Circle.cs" company="algernon (K. Algernon A. Sheppard)">
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
    using UnityEngine;
    using static Game.Rendering.GuideLinesSystem;

    /// <summary>
    ///  Circle placement mode.
    /// </summary>
    public class Circle : LineBase
    {
        // Calculated circle Bezier parts.
        private readonly Bezier4x3[] _overlayBeziers = new Bezier4x3[4];
        private bool _validOverlayBezier = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Circle"/> class.
        /// </summary>
        /// <param name="mode">Mode to copy starting state from.</param>
        /// <param name="highPriorityColor">High priority line colour.</param>
        /// <param name="mediumPriorityColor">Medium priority line colour.</param>
        /// <param name="distanceScale">Line width distance scale.</param>
        public Circle(LineBase mode, Color highPriorityColor, Color mediumPriorityColor, float distanceScale)
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

            // Calculate length.
            float3 difference = currentPos - m_startPos;
            float radius = math.length(difference);

            // Calculate circle Bezier by combining 4 curved parts.
            _validOverlayBezier = true;
            _overlayBeziers[0] = NetUtils.CircleCurve(m_startPos, radius, radius);
            _overlayBeziers[1] = NetUtils.CircleCurve(m_startPos, radius * -1f, radius);
            _overlayBeziers[2] = NetUtils.CircleCurve(m_startPos, radius, radius * -1f);
            _overlayBeziers[3] = NetUtils.CircleCurve(m_startPos, radius * -1f, radius * -1f);

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
        /// <param name="alpha">Overlay alpha value.</param>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        /// <param name="tooltips">Tooltip list.</param>
        public override void DrawOverlay(float alpha, OverlayRenderSystem.Buffer overlayBuffer, List<TooltipInfo> tooltips)
        {
            if (m_validStart)
            {
                // Draw a straight radial line (constrained if required).
                base.DrawOverlay(alpha, overlayBuffer, tooltips);

                // Draw circle overlay.
                if (_validOverlayBezier)
                {
                    for (int i = 0; i < _overlayBeziers.Length; i++)
                    {
                        DrawCurvedLine(_overlayBeziers[i], overlayBuffer);
                    }
                }
            }
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            // Invalidate overlay Bezier.
            _validOverlayBezier = false;
        }

        /// <summary>
        /// Performs actions after items are placed on the current line, setting up for the next line to be set.
        /// </summary>
        public override void ItemsPlaced()
        {
            // Invalidate overlay Bezier.
            _validOverlayBezier = false;

            // Otherwise empty, with no call to Base, to retain original start position (centre of circle).
        }
    }
}
