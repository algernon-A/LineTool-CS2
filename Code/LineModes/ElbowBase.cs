// <copyright file="ElbowBase.cs" company="algernon (K. Algernon A. Sheppard)">
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
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using static Game.Rendering.GuideLinesSystem;
    using static LineToolSystem;

    /// <summary>
    /// Placement modes that use placement elbows.
    /// </summary>
    public abstract class ElbowBase : LineBase
    {
        // Elbow point.
        private bool _validPreviousElbow = false;
        private float3 _previousElbowPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElbowBase"/> class.
        /// </summary>
        public ElbowBase()
        {
            // Basic state.
            m_validStart = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ElbowBase"/> class.
        /// </summary>
        /// <param name="mode">Mode to copy starting state from.</param>
        /// <param name="highPriorityColor">High priority line colour.</param>
        /// <param name="mediumPriorityColor">Medium priority line colour.</param>
        /// <param name="distanceScale">Line width distance scale.</param>
        public ElbowBase(LineBase mode, Color highPriorityColor, Color mediumPriorityColor, float distanceScale)
            : base(mode, highPriorityColor, mediumPriorityColor, distanceScale)
        {
        }

        /// <summary>
        /// Gets a value indicating whether we're ready to place (we have enough control positions).
        /// </summary>
        public override bool HasAllPoints => m_validStart & ValidElbow;

        /// <summary>
        /// Gets a value indicating whether a valid elbow point is currently set.
        /// </summary>
        protected bool ValidElbow { get; private set; } = false;

        /// <summary>
        /// Gets the current elbow point.
        /// May be invalid; validity must be determined using <see cref="ValidElbow"/>.
        /// </summary>
        protected float3 ElbowPoint { get; private set; }

        /// <summary>
        /// Handles a mouse click.
        /// </summary>
        /// <param name="position">Click world position.</param>
        /// <returns>The <see cref="ClickMode"/> reflecting how the click was processed.</returns>
        public override ClickMode HandleClick(float3 position)
        {
            // If no valid initial point, record this as the first point.
            if (!m_validStart)
            {
                m_startPos = position;
                m_endPos = position;
                m_validStart = true;
                return ClickMode.Initial;
            }

            // Otherwise, if no valid elbow point, record this as the elbow point.
            if (!ValidElbow)
            {
                ElbowPoint = position;
                ValidElbow = true;
                return ClickMode.Midpoint;
            }

            // Place the items on the curve.
            return ClickMode.Placed;
        }

        /// <summary>
        /// Performs actions after items are placed on the current line, setting up for the next line to be set.
        /// </summary>
        public override void ItemsPlaced()
        {
            base.ItemsPlaced();

            // Clear elbow.
            ValidElbow = false;
            _previousElbowPoint = ElbowPoint;
            _validPreviousElbow = true;
        }

        /// <summary>
        /// Draws point overlays.
        /// </summary>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        public override void DrawPointOverlays(OverlayRenderSystem.Buffer overlayBuffer)
        {
            base.DrawPointOverlays(overlayBuffer);

            // Draw elbow point.
            if (ValidElbow)
            {
                Color softCyan = Color.cyan;
                softCyan.a *= 0.1f;
                overlayBuffer.DrawCircle(Color.cyan, softCyan, 0.3f, 0, new float2(0f, 1f), ElbowPoint, PointRadius * 2f);
            }
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public override void Reset()
        {
            // Only clear elbow, if we have one.
            if (ValidElbow)
            {
                ValidElbow = false;
            }
            else
            {
                // Otherwise, reset entire state.
                _validPreviousElbow = false;
                base.Reset();
            }
        }

        /// <summary>
        /// Applies any active constraints to the given world position.
        /// </summary>
        /// <param name="currentPos">World position to constrain.</param>
        /// <param name="spacing">Spacing value to apply for length constraints.</param>
        /// /// <param name="snapToLength"><c>true</c> if the snap-to-length is enabled, <c>false</c> otherwise.</param>
        /// <returns>Constrained world position.</returns>
        internal override float3 ConstrainPos(float3 currentPos, float spacing, bool snapToLength)
        {
            float3 startPos = currentPos;

            // Apply length snapping, if active.
            if (snapToLength)
            {
                if (ValidElbow)
                {
                    startPos = SnapToLength(currentPos, ElbowPoint, spacing);
                }
                else
                {
                    startPos = SnapToLength(currentPos, m_startPos, spacing);
                }
            }

            // Snapping requires at least a valid start point.
            if (m_validStart)
            {
                // If a there's a valid elbow position and the alt key is pressed, perform any angle constraints.
                if (ValidElbow && Keyboard.current.altKey.isPressed)
                {
                    // Get 2D length of the vector to the cursor position.
                    float elbowToCurrentLength = math.distance(ElbowPoint.xz, startPos.xz);

                    // Get the normalised direction of the base line.
                    float3 direction = math.normalize(m_startPos - ElbowPoint);

                    // Determine angle using dot product formula for normalized vectors: a . b = cos(angle).
                    // Control for invalid values by clamping dot product to (-1, 1) and treating any NaN as zero.
                    float dotProduct = math.clamp(math.dot(math.normalize(startPos - ElbowPoint), direction), -1f, 1f);
                    if (float.IsNaN(dotProduct))
                    {
                        dotProduct = 0;
                    }

                    // Get raw angle between lines from dot product and snap to increments of 15 degrees.
                    int angleDegrees = (int)math.round(math.degrees(math.acos(dotProduct)));
                    angleDegrees = (int)math.round(angleDegrees / 15f) * 15;

                    // Use custom cross product to determine if the cursor point is to the left- or right- side of the base line.
                    float crossProduct = ((ElbowPoint.x - m_startPos.x) * (startPos.z - m_startPos.z)) - ((ElbowPoint.z - m_startPos.z) * (startPos.x - m_startPos.x));
                    if (crossProduct < 0)
                    {
                        // If the cross product is negative, the current point is to the right of the line and we need to invert the angle.
                        angleDegrees = 360 - angleDegrees;
                    }

                    // Rotate original vector direction around the Y axis.
                    Quaternion q = quaternion.Euler(0f, math.radians(angleDegrees), 0f);
                    float3 rotatedVector = q * direction;

                    // Set the clamped position by extrapolation from the elbow point.
                    return ElbowPoint + (rotatedVector * elbowToCurrentLength);
                }
                else if (_validPreviousElbow)
                {
                    // If there's no a vaid elbow yet, constrain to continuous curve if there's a valid previous elbow to constrain to.
                    // Use closest point on infinite line projected from previous curve end tangent.
                    return math.project(startPos - _previousElbowPoint, m_startPos - _previousElbowPoint) + _previousElbowPoint;
                }
            }

            // Default is just to return the unmodified position.
            return startPos;
        }

        /// <summary>
        /// Checks to see if a click should initiate point dragging.
        /// </summary>
        /// <param name="position">Click position in world space.</param>
        /// <returns>Drag mode.</returns>
        internal override DragMode CheckDragHit(float3 position)
        {
            // Start and end points.
            DragMode mode = base.CheckDragHit(position);

            // If no hit from base (start and end points), check for elbow point hit.
            if (mode == DragMode.None && ValidElbow && math.distancesq(position, ElbowPoint) < (PointRadius * PointRadius))
            {
                return DragMode.ElbowPos;
            }

            return mode;
        }

        /// <summary>
        /// Handles dragging action.
        /// </summary>
        /// <param name="dragMode">Dragging mode.</param>
        /// <param name="position">New position.</param>
        internal override void HandleDrag(DragMode dragMode, float3 position)
        {
            if (dragMode == DragMode.ElbowPos)
            {
                // Update elbow point.
                ElbowPoint = position;
            }
            else
            {
                // Other points.
                base.HandleDrag(dragMode, position);
            }
        }

        /// <summary>
        /// Draws an angle indicator between two lines.
        /// </summary>
        /// <param name="line1">Line 1.</param>
        /// <param name="line2">Line 2.</param>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        /// <param name="tooltips">Tooltip list.</param>
        protected void DrawAngleIndicator(Line3.Segment line1, Line3.Segment line2, OverlayRenderSystem.Buffer overlayBuffer, List<TooltipInfo> tooltips)
        {
            // Calculate line lengths.
            float line1Length = math.distance(line1.a.xz, line1.b.xz);
            float line2Length = math.distance(line2.a.xz, line2.b.xz);

            // Calculate line and indicator size factors.
            float overlayLineWidth = m_distanceScale * 0.125f;
            float maxScale = m_distanceScale * 4f;
            float line1lengthXZ = MathUtils.Length(line1.xz);

            // Minimum line length check.
            if (line1lengthXZ > overlayLineWidth * 7f)
            {
                // Normalize vectors using elbow pivot: line1.b == line2.a.
                float3 normalizedLine1 = (line1.a - line1.b) / line1Length;
                float3 normalizedLine2 = (line2.b - line1.b) / line2Length;

                // Determine angle using dot product formula for normalized vectors: a . b = cos(angle).
                double angleRadians = math.acos(math.dot(normalizedLine1, normalizedLine2));
                int angleDegrees = (int)math.round(math.degrees(angleRadians));

                // Calculate angle box points.
                float size = math.min(line1lengthXZ, maxScale) * 0.5f;
                float3 overlayJoint1 = line1.b + (normalizedLine1 * size);
                float3 overlayJoint2 = line1.b + (normalizedLine2 * size);
                float3 overlayJointS = line1.b + (normalizedLine1 * size) + (normalizedLine2 * size);

                if (angleDegrees == 90)
                {
                    // Right angle box.
                    overlayBuffer.DrawLine(m_highPriorityColor, new Line3.Segment(overlayJointS, overlayJoint1), overlayLineWidth);
                    overlayBuffer.DrawLine(m_highPriorityColor, new Line3.Segment(overlayJointS, overlayJoint2), overlayLineWidth);
                }
                else if (angleDegrees >= 180 || angleDegrees <= -180)
                {
                    // Straight-line box; need to calculate line normal for box offset.
                    float3 lineNormal = new (normalizedLine1.z, normalizedLine1.y, -normalizedLine1.x);

                    // Calculate two offset points for corner of box, straddling the elbot pivot.
                    overlayJointS = line1.b + (normalizedLine1 * size) + (lineNormal * size);
                    float3 overlayJointS2 = line1.b - (normalizedLine1 * size) + (lineNormal * size);

                    overlayBuffer.DrawLine(m_highPriorityColor, new Line3.Segment(overlayJointS, overlayJoint1), overlayLineWidth);
                    overlayBuffer.DrawLine(m_highPriorityColor, new Line3.Segment(overlayJointS, overlayJointS2), overlayLineWidth);
                    overlayBuffer.DrawLine(m_highPriorityColor, new Line3.Segment(overlayJointS2, overlayJoint2), overlayLineWidth);
                }
                else
                {
                    Bezier4x3 angleOverlayCurve = NetUtils.FitCurve(new Line3.Segment(overlayJoint1, overlayJointS), new Line3.Segment(overlayJoint2, overlayJointS));
                    overlayBuffer.DrawCurve(m_highPriorityColor, angleOverlayCurve, overlayLineWidth);
                }

                // Add tooltip.
                float3 tooltipPos = line1.b + ((line1.b - overlayJointS) * 0.3f);
                TooltipInfo value = new (TooltipType.Angle, tooltipPos, angleDegrees);
                tooltips.Add(value);
            }
        }
    }
}
