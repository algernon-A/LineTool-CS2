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
        public ElbowBase(LineBase mode)
            : base(mode)
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
        /// <returns>True if items are to be placed as a result of this click, false otherwise.</returns>
        public override bool HandleClick(float3 position)
        {
            // If no valid initial point, record this as the first point.
            if (!m_validStart)
            {
                m_startPos = position;
                m_endPos = position;
                m_validStart = true;
                return false;
            }

            // Otherwise, if no valid elbow point, record this as the elbow point.
            if (!ValidElbow)
            {
                ElbowPoint = ConstrainPos(position);
                ValidElbow = true;
                return false;
            }

            // Place the items on the curve.
            return true;
        }

        /// <summary>
        /// Performs actions after items are placed on the current line, setting up for the next line to be set.
        /// </summary>
        /// <param name="position">Click world position.</param>
        public override void ItemsPlaced(float3 position)
        {
            // Update new starting location to the previous end point and clear elbow.
            m_startPos = position;
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
        /// Applies any active constraints the given current cursor world position.
        /// </summary>
        /// <param name="currentPos">Current cursor world position.</param>
        /// <returns>Constrained cursor world position.</returns>
        protected float3 ConstrainPos(float3 currentPos)
        {
            // Constrain to continuous curve.
            if (m_validStart && !ValidElbow && _validPreviousElbow)
            {
                // Use closest point on infinite line projected from previous curve end tangent.
                return math.project(currentPos - _previousElbowPoint, m_startPos - _previousElbowPoint) + _previousElbowPoint;
            }

            return currentPos;
        }

        /// <summary>
        /// Draws an angle indicator between two lines.
        /// </summary>
        /// <param name="line1">Line 1.</param>
        /// <param name="line2">Line 2.</param>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        /// <param name="tooltips">Tooltip list.</param>
        /// <param name="cameraController">Active camera controller instance.</param>
        protected void DrawAngleIndicator(Line3.Segment line1, Line3.Segment line2, OverlayRenderSystem.Buffer overlayBuffer, List<TooltipInfo> tooltips, CameraUpdateSystem cameraController)
        {
            // Semi-transparent white color.
            Color lineColour = new (1f, 1f, 1f, 0.6f);

            // Calculate line lengths.
            float line1Length = math.distance(line1.a.xz, line1.b.xz);
            float line2Length = math.distance(line2.a.xz, line2.b.xz);

            float lineScaleModifier = (cameraController.zoom * 0.0025f) + 0.1f;
            float overlayLineDistance = math.min(line1Length, line2Length) / 8f;
            float overlayLineWidth = lineScaleModifier * 0.5f;

            // Minimum line length check.
            if (line1Length > 2f && line2Length > 2f)
            {
                // Normalize vectors using elbow pivot: line1.b or line2.a.
                float3 normalizedVector1 = (line1.a - line1.b) / line1Length;
                float3 normalizedVector2 = (line2.b - line1.b) / line2Length;

                // Determine angle using dot product formula for normalized vectors: a . b = cos(angle).
                double angleRadians = math.acos(math.dot(normalizedVector1, normalizedVector2));
                int angleDegrees = (int)math.round(math.degrees(angleRadians));

                // Calculate vector split down the middle (using vector addition parallelogram method).
                float3 overlayJointS = line1.b + ((normalizedVector1 + normalizedVector2) * overlayLineDistance);

                // Calculate joints where angle guidelines will connect at.
                float3 overlayJoint1 = line1.b + (normalizedVector1 * overlayLineDistance);
                float3 overlayJoint2 = line1.b + (normalizedVector2 * overlayLineDistance);

                if (angleDegrees == 90)
                {
                    // Set joints in-between and then draw full width line as "box".
                    overlayBuffer.DrawLine(lineColour, new Line3.Segment((overlayJoint1 + line1.b) / 2, (overlayJointS + overlayJoint2) / 2), overlayLineDistance);
                }
                else
                {
                    Bezier4x3 angleOverlayCurve = NetUtils.FitCurve(new Line3.Segment(overlayJoint1, overlayJointS), new Line3.Segment(overlayJoint2, overlayJointS));
                    overlayBuffer.DrawCurve(lineColour, angleOverlayCurve, overlayLineWidth);
                }

                // Add tooltip.
                float3 tooltipPos = line1.b + ((line1.b - overlayJointS) * 0.3f);
                TooltipInfo value = new (TooltipType.Angle, tooltipPos, angleDegrees);
                tooltips.Add(value);
            }
        }
    }
}
