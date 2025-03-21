// <copyright file="LineBase.cs" company="algernon (K. Algernon A. Sheppard)">
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
    using static LineToolSystem;

    /// <summary>
    /// Line placement mode.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Protected fields")]
    public abstract class LineBase
    {
        /// <summary>
        /// Selection radius of points.
        /// </summary>
        protected const float PointRadius = 8f;

        /// <summary>
        /// Indicates whether a valid starting position has been recorded.
        /// </summary>
        protected bool m_validStart;

        /// <summary>
        /// Records the current selection start position.
        /// </summary>
        protected float3 m_startPos;

        /// <summary>
        /// Records the current selection end position.
        /// </summary>
        protected float3 m_endPos;

        /// <summary>
        /// High priority line colour.
        /// </summary>
        protected Color m_highPriorityColor;

        /// <summary>
        /// Medium priority line colour.
        /// </summary>
        protected Color m_mediumPriorityColor;

        /// <summary>
        /// Line width distance scale.
        /// </summary>
        protected float m_distanceScale;

        /// <summary>
        /// Initializes a new instance of the <see cref="LineBase"/> class.
        /// </summary>
        public LineBase()
        {
            // Basic state.
            m_validStart = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineBase"/> class.
        /// </summary>
        /// <param name="highPriorityColor">High priority line colour.</param>
        /// <param name="mediumPriorityColor">Medium priority line colour.</param>
        /// <param name="distanceScale">Line width distance scale.</param>
        public LineBase(Color highPriorityColor, Color mediumPriorityColor, float distanceScale)
        {
            m_validStart = false;
            m_highPriorityColor = highPriorityColor;
            m_mediumPriorityColor = mediumPriorityColor;
            m_distanceScale = distanceScale;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineBase"/> class.
        /// </summary>
        /// <param name="mode">Mode to copy starting state from.</param>
        /// <param name="highPriorityColor">High priority line colour.</param>
        /// <param name="mediumPriorityColor">Medium priority line colour.</param>
        /// <param name="distanceScale">Line width distance scale.</param>
        public LineBase(LineBase mode, Color highPriorityColor, Color mediumPriorityColor, float distanceScale)
        {
            m_validStart = mode.m_validStart;
            m_startPos = mode.m_startPos;
            m_highPriorityColor = highPriorityColor;
            m_mediumPriorityColor = mediumPriorityColor;
            m_distanceScale = distanceScale;
        }

        /// <summary>
        /// Gets a value indicating whether a valid starting position has been recorded.
        /// </summary>
        public bool HasStart => m_validStart;

        /// <summary>
        /// Gets a value indicating whether we're ready to place (we have enough control positions).
        /// </summary>
        public virtual bool HasAllPoints => m_validStart;

        /// <summary>
        /// Handles a mouse click.
        /// </summary>
        /// <param name="position">Click world position.</param>
        /// <returns>The <see cref="ClickMode"/> reflecting how the click was processed.</returns>
        public virtual ClickMode HandleClick(float3 position)
        {
            // If no valid start position is set, record it.
            if (!m_validStart)
            {
                m_startPos = position;
                m_endPos = position;
                m_validStart = true;

                // This is the initial click.
                return ClickMode.Initial;
            }

            // Second click; we're placing items.
            return ClickMode.Placed;
        }

        /// <summary>
        /// Performs actions after items are placed on the current line, setting up for the next line to be set.
        /// </summary>
        public virtual void ItemsPlaced()
        {
            // Update new starting location to the previous end point.
            m_startPos = m_endPos;
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
        public virtual void CalculatePoints(float3 currentPos, SpacingMode spacingMode, RotationMode rotationMode, float spacing, float randomSpacing, float randomOffset, int rotation, Bounds1 zBounds, List<PointData> pointList, ref TerrainHeightData heightData)
        {
            // Don't do anything if we don't have a valid start point.
            if (!m_validStart)
            {
                return;
            }

            // Calculate length.
            float3 difference = currentPos - m_startPos;
            float length = math.length(difference);
            System.Random random = new ((int)length * 1000);

            // Calculate base line angle (for absolute/relative rotation).
            float baseAngle = rotationMode == RotationMode.Absolute ? 0f : math.atan2(difference.x, difference.z);

            // Calculate applied rotation (in radians).
            float appliedRotation = spacingMode switch
            {
                SpacingMode.FenceMode => math.atan2(difference.x, difference.z),
                SpacingMode.W2WMode => math.atan2(difference.x, difference.z) + (math.PI / 2f),
                _ => math.radians(rotation) + baseAngle,
            };

            // Rotation quaternion.
            quaternion qRotation = quaternion.Euler(0f, appliedRotation, 0f);

            // Calculate even full-length spacing if needed.
            float adjustedSpacing = spacing;
            if (spacingMode == SpacingMode.FullLength)
            {
                adjustedSpacing = length / math.round(length / spacing);
            }

            // Create points.
            float currentDistance = spacingMode == SpacingMode.FenceMode ? -zBounds.min : 0f;
            float endLength = spacingMode == SpacingMode.FenceMode ? length - zBounds.max : length;
            while (currentDistance < endLength + 0.001f)
            {
                // Calculate interpolated point.
                float spacingAdjustment = 0f;
                if (randomSpacing > 0f && spacingMode != SpacingMode.FenceMode && spacingMode != SpacingMode.W2WMode)
                {
                    spacingAdjustment = (float)(random.NextDouble() * randomSpacing * 2f) - randomSpacing;
                }

                float3 thisPoint = math.lerp(m_startPos, currentPos, (currentDistance + spacingAdjustment) / length);

                // Apply offset adjustment.
                if (randomOffset > 0f && spacingMode != SpacingMode.FenceMode && spacingMode != SpacingMode.W2WMode)
                {
                    float3 left = math.normalize(new float3(-difference.z, 0f, difference.x));
                    thisPoint += left * ((float)(randomOffset * random.NextDouble() * 2f) - randomOffset);
                }

                thisPoint.y = TerrainUtils.SampleHeight(ref heightData, thisPoint);

                // Add point to list.
                pointList.Add(new PointData { Position = thisPoint, Rotation = qRotation, });
                currentDistance += adjustedSpacing;
            }

            // Final item for fence mode.
            if (spacingMode == SpacingMode.FenceMode && currentDistance < length - zBounds.min)
            {
                float3 thisPoint = math.lerp(m_startPos, currentPos, (length - zBounds.max + 0.001f) / length);
                thisPoint.y = TerrainUtils.SampleHeight(ref heightData, thisPoint);
                pointList.Add(new PointData { Position = thisPoint, Rotation = qRotation, });
            }

            // Otherwise, final item if we haven't placed but are within 2% of final placement distance.
            else if (currentDistance < endLength + (adjustedSpacing * 0.02f))
            {
                float3 thisPoint = currentPos;
                thisPoint.y = TerrainUtils.SampleHeight(ref heightData, thisPoint);
                pointList.Add(new PointData { Position = thisPoint, Rotation = qRotation, });
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
        public virtual void DrawOverlay(float alpha, OverlayRenderSystem.Buffer overlayBuffer, List<TooltipInfo> tooltips)
        {
            // Don't draw overlay if we don't have a valid start.
            if (m_validStart)
            {
                DrawControlLine(m_startPos, m_endPos, new Line3.Segment(m_startPos, m_endPos), alpha, overlayBuffer, tooltips);
            }
        }

        /// <summary>
        /// Draws point overlays.
        /// </summary>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        public virtual void DrawPointOverlays(OverlayRenderSystem.Buffer overlayBuffer)
        {
            Color softCyan = Color.cyan;
            softCyan.a *= 0.1f;

            overlayBuffer.DrawCircle(Color.cyan, softCyan, 0.3f, 0, new float2(0f, 1f), m_startPos, PointRadius * 2f);
            overlayBuffer.DrawCircle(Color.cyan, softCyan, 0.3f, 0, new float2(0f, 1f), m_endPos, PointRadius * 2f);
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public virtual void Reset()
        {
            m_validStart = false;
        }

        /// <summary>
        /// Applies any active constraints to the given world position.
        /// </summary>
        /// <param name="currentPos">World position to constrain.</param>
        /// <param name="spacing">Spacing value to apply for length constraints.</param>
        /// <param name="snapToLength"><c>true</c> if the snap-to-length is enabled, <c>false</c> otherwise.</param>
        /// <returns>Constrained world position.</returns>
        internal virtual float3 ConstrainPos(float3 currentPos, float spacing, bool snapToLength)
        {
            // Constrain to length.
            if (snapToLength)
            {
                // Length constraint requires a valid starting poing.
                if (m_validStart)
                {
                    return SnapToLength(currentPos, m_startPos, spacing);
                }
            }

            // Default is to return the unaltered original position.
            return currentPos;
        }

        /// <summary>
        /// Checks to see if a click should initiate point dragging.
        /// </summary>
        /// <param name="position">Click position in world space.</param>
        /// <returns>Drag mode.</returns>
        internal virtual DragMode CheckDragHit(float3 position)
        {
            if (math.distancesq(position, m_startPos) < (PointRadius * PointRadius))
            {
                // Start point.
                return DragMode.StartPos;
            }
            else if (math.distancesq(position, m_endPos) < (PointRadius * PointRadius))
            {
                // End point.
                return DragMode.EndPos;
            }

            // No hit.
            return DragMode.None;
        }

        /// <summary>
        /// Handles dragging action.
        /// </summary>
        /// <param name="dragMode">Dragging mode.</param>
        /// <param name="position">New position.</param>
        internal virtual void HandleDrag(DragMode dragMode, float3 position)
        {
            // Drag start point.
            if (dragMode == DragMode.StartPos)
            {
                m_startPos = position;
            }
        }

        /// <summary>
        /// Draws two control point circles at the given locations with a straight dashed line overlay between them.
        /// </summary>
        /// <param name="startPos">Line start position.</param>
        /// <param name="endPos">Line end position.</param>
        /// <param name="segment">Line segment.</param>
        /// <param name="alpha">Overlay alpha value.</param>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        /// <param name="tooltips">Tooltip list.</param>
        protected void DrawControlLine(float3 startPos, float3 endPos, Line3.Segment segment, float alpha, OverlayRenderSystem.Buffer overlayBuffer, List<TooltipInfo> tooltips)
        {
            // Set line width.
            float lineScale = m_distanceScale * 0.125f;

            // Don't draw line if the distance is too short.
            float lineLength = math.distance(startPos.xz, endPos.xz);
            if (lineLength > lineScale * 8f)
            {
                // Draw dashed line, inset from points to leave space for control point circles.
                float3 insetLength = (segment.b - segment.a) * (lineScale * 4f / lineLength);
                m_highPriorityColor.a = alpha;
                overlayBuffer.DrawDashedLine(m_highPriorityColor, new (segment.a + insetLength, segment.b - insetLength), lineScale * 3f, lineScale * 5f, lineScale * 3f);

                // Draw control point circles.
                overlayBuffer.DrawCircle(m_highPriorityColor, startPos, lineScale * 5f);
                overlayBuffer.DrawCircle(m_highPriorityColor, endPos, lineScale * 5f);

                // Add length tooltip.
                int length = (int)math.round(math.distance(startPos.xz, endPos.xz));
                if (length > 0)
                {
                    tooltips.Add(new TooltipInfo(TooltipType.Length, (startPos + endPos) * 0.5f, length));
                }
            }
        }

        /// <summary>
        /// Draws a curved dashed line overlay along the given Bezier curve.
        /// </summary>
        /// <param name="curve">Line curve segment.</param>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        protected void DrawCurvedLine(Bezier4x3 curve, OverlayRenderSystem.Buffer overlayBuffer) => overlayBuffer.DrawCurve(m_mediumPriorityColor, curve, m_distanceScale * 0.125f);

        /// <summary>
        /// Calculates the 2D XZ angle (in radians) between two points, adding the provided adjustment.
        /// </summary>
        /// <param name="point1">First point.</param>
        /// <param name="point2">Second point.</param>
        /// <param name="adjustment">Adjustment to apply (in radians).</param>
        /// <returns>2D XZ angle from the first to the second point, in radians.</returns>
        protected float CalculateRelativeAngle(float3 point1, float3 point2, float adjustment)
        {
            // Calculate angle from point 1 to point 2.
            float3 difference = point2 - point1;
            float relativeAngle = math.atan2(difference.x, difference.z) + adjustment;

            // Error check.
            if (float.IsNaN(relativeAngle))
            {
                relativeAngle = 0f;
            }

            // Minimum bounds check.
            while (relativeAngle < -math.PI)
            {
                relativeAngle += math.PI * 2f;
            }

            // Maximum bounds check.
            while (relativeAngle >= math.PI)
            {
                relativeAngle -= math.PI * 2f;
            }

            return relativeAngle;
        }

        /// <summary>
        /// Calculates a length-snapped endpoint for the given line.
        /// </summary>
        /// <param name="end">Line end point (position to be snapped).</param>
        /// <param name="start">Line start point (base position to measure snapping distance from).</param>
        /// <param name="lengthMultiple">Snapping length multiple to apply.</param>
        /// <returns>New length-snapped endpoint.</returns>
        protected float3 SnapToLength(float3 end, float3 start, float lengthMultiple)
        {
            // Get 2D length of the vector to the cursor position.
            float3 line = end - start;
            float length = math.length(line);

            // Round it to next lowest multiple of the current length.
            float roundedLength = math.floor(length / lengthMultiple) * lengthMultiple;

            // Include small round-up adjustment.
            return start + ((roundedLength * line) / length) + 0.01f;
        }
    }
}
