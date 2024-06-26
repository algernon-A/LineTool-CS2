// <copyright file="SimpleCurve.cs" company="algernon (K. Algernon A. Sheppard)">
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
    using static LineToolSystem;

    /// <summary>
    /// Simple curve placement mode.
    /// </summary>
    public class SimpleCurve : LineBase
    {
        // Current elbow point.
        private bool _validElbow = false;
        private bool _validPreviousElbow = false;
        private float3 _elbowPoint;
        private float3 _previousElbowPoint;

        // Calculated Bezier.
        private Bezier4x3 _thisBezier;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleCurve"/> class.
        /// </summary>
        /// <param name="mode">Mode to copy starting state from.</param>
        public SimpleCurve(LineBase mode)
            : base(mode)
        {
        }

        /// <summary>
        /// Gets a value indicating whether we're ready to place (we have enough control positions).
        /// </summary>
        public override bool HasAllPoints => m_validStart & _validElbow;

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
            if (!_validElbow)
            {
                _elbowPoint = ConstrainPos(position);
                _validElbow = true;
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
            _validElbow = false;
            _previousElbowPoint = _elbowPoint;
            _validPreviousElbow = true;
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
            if (!_validElbow)
            {
                // Constrain as required.
                m_endPos = ConstrainPos(currentPos);
                base.CalculatePoints(m_endPos, spacingMode, rotationMode, spacing, randomSpacing, randomOffset, rotation, zBounds, pointList, ref heightData);
                return;
            }

            // Calculate Bezier.
            _thisBezier = NetUtils.FitCurve(new Line3.Segment(m_startPos, _elbowPoint), new Line3.Segment(currentPos, _elbowPoint));

            // Calculate even full-length spacing if needed.
            float adjustedSpacing = spacing;
            float length = MathUtils.Length(_thisBezier);
            if (spacingMode == SpacingMode.FullLength)
            {
                adjustedSpacing = length / math.round(length / spacing);
            }

            // Default rotation quaternion.
            float rotationRadians = math.radians(rotation);
            quaternion qRotation = quaternion.Euler(0f, rotationRadians, 0f);

            // Randomizer.
            System.Random random = new ((int)(currentPos.x + currentPos.z) * 1000);

            // Traverse Bezier and place objects.
            float tFactor = 0f;
            float distanceTravelled = 0f;
            while (tFactor < 1.0f)
            {
                // Apply spacing randomization.
                float adjustedT = tFactor;
                if (randomSpacing > 0f && spacingMode != SpacingMode.FenceMode && spacingMode != SpacingMode.W2WMode)
                {
                    float spacingAdjustment = (float)(random.NextDouble() * randomSpacing * 2f) - randomSpacing;
                    adjustedT = spacingAdjustment < 0f ? BezierStepReverse(tFactor, spacingAdjustment) : BezierStep(tFactor, spacingAdjustment);
                }

                // Calculate point.
                float3 thisPoint = MathUtils.Position(_thisBezier, adjustedT);

                // Apply offset randomization.
                if (randomOffset > 0f && spacingMode != SpacingMode.FenceMode)
                {
                    float3 tangent = MathUtils.Tangent(_thisBezier, adjustedT);
                    float3 left = math.normalize(new float3(-tangent.z, 0f, tangent.x));
                    thisPoint += left * ((float)(randomOffset * random.NextDouble() * 2f) - randomOffset);
                }

                // Get next t factor.
                tFactor = BezierStep(tFactor, adjustedSpacing);
                distanceTravelled += adjustedSpacing;

                // Calculate position and rotation.
                if (spacingMode == SpacingMode.FenceMode)
                {
                    // Fence mode.
                    float3 nextPoint = MathUtils.Position(_thisBezier, tFactor);

                    // Check validity of result.
                    if (float.IsNaN(nextPoint.x) || float.IsNaN(nextPoint.y) || float.IsNaN(nextPoint.z))
                    {
                        continue;
                    }

                    // Apply rotation.
                    float effectiveRotation = CalculateRelativeAngle(thisPoint, nextPoint, 0f);
                    qRotation = quaternion.Euler(0f, effectiveRotation, 0f);
                    thisPoint = (nextPoint + thisPoint) / 2f;
                }
                else if (spacingMode == SpacingMode.W2WMode)
                {
                    // Wall-to-wall mode.
                    float3 nextPoint = MathUtils.Position(_thisBezier, tFactor);

                    // Check validity of result.
                    if (float.IsNaN(nextPoint.x) || float.IsNaN(nextPoint.y) || float.IsNaN(nextPoint.z))
                    {
                        continue;
                    }

                    // Apply rotation.
                    float relativeAngle = CalculateRelativeAngle(thisPoint, MathUtils.Position(_thisBezier, tFactor), math.PI / 2f);
                    qRotation = quaternion.Euler(0f, relativeAngle, 0f);
                    thisPoint = (nextPoint + thisPoint) / 2f;
                }
                else if (rotationMode == RotationMode.Relative)
                {
                    // Apply rotation.
                    float relativeAngle = CalculateRelativeAngle(thisPoint, MathUtils.Position(_thisBezier, tFactor), rotationRadians);
                    qRotation = quaternion.Euler(0f, relativeAngle, 0f);
                }

                // Calculate terrain height.
                thisPoint.y = TerrainUtils.SampleHeight(ref heightData, thisPoint);

                // Add point to list.
                pointList.Add(new PointData { Position = thisPoint, Rotation = qRotation, });
            }

            // Final item for full-length mode if required (if there was a distance overshoot).
            if (spacingMode == SpacingMode.FullLength && distanceTravelled < length + adjustedSpacing)
            {
                float3 thisPoint = currentPos;
                thisPoint.y = TerrainUtils.SampleHeight(ref heightData, thisPoint);

                // Add point to list.
                pointList.Add(new PointData { Position = thisPoint, Rotation = qRotation, });
            }

            // Record end position for overlays.
            m_endPos = currentPos;
        }

        /// <summary>
        /// Draws any applicable overlay.
        /// </summary>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        /// <param name="tooltips">Tooltip list.</param>
        /// <param name="cameraController">Current camera controller.</param>
        public override void DrawOverlay(OverlayRenderSystem.Buffer overlayBuffer, List<TooltipInfo> tooltips, CameraUpdateSystem cameraController)
        {
            if (m_validStart)
            {
                // Draw an elbow overlay if we've got valid starting and elbow positions.
                if (_validElbow)
                {
                    // Calculate lines.
                    Line3.Segment line1 = new (m_startPos, _elbowPoint);
                    Line3.Segment line2 = new (_elbowPoint, m_endPos);

                    // Draw lines.
                    DrawDashedLine(m_startPos, _elbowPoint, line1, overlayBuffer, tooltips, cameraController);
                    DrawDashedLine(_elbowPoint, m_endPos, line2, overlayBuffer, tooltips, cameraController);

                    // Draw angle.
                    DrawAngleIndicator(line1, line2, overlayBuffer, tooltips, cameraController);

                    // Draw curved line.
                    DrawCurvedDashedLine(_thisBezier, overlayBuffer, cameraController);
                }
                else
                {
                    // Initial position only; just draw a straight line (constrained if required).
                    base.DrawOverlay(overlayBuffer, tooltips, cameraController);
                }
            }
        }

        /// <summary>
        /// Draws point overlays.
        /// </summary>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        public override void DrawPointOverlays(OverlayRenderSystem.Buffer overlayBuffer)
        {
            base.DrawPointOverlays(overlayBuffer);

            // Draw elbow point.
            if (_validElbow)
            {
                Color softCyan = Color.cyan;
                softCyan.a *= 0.1f;
                overlayBuffer.DrawCircle(Color.cyan, softCyan, 0.3f, 0, new float2(0f, 1f), _elbowPoint, PointRadius * 2f);
            }
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public override void Reset()
        {
            // Only clear elbow, if we have one.
            if (_validElbow)
            {
                _validElbow = false;
                _thisBezier = new ();
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
            if (mode == DragMode.None && _validElbow && math.distancesq(position, _elbowPoint) < (PointRadius * PointRadius))
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
                _elbowPoint = position;
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
        private float3 ConstrainPos(float3 currentPos)
        {
            // Constrain to continuous curve.
            if (m_validStart && !_validElbow && _validPreviousElbow)
            {
                // Use closest point on infinite line projected from previous curve end tangent.
                return math.project(currentPos - _previousElbowPoint, m_startPos - _previousElbowPoint) + _previousElbowPoint;
            }

            return currentPos;
        }

        /// <summary>
        /// Steps along a Bezier calculating the target t factor for the given starting t factor and the given distance.
        /// Code based on Alterran's PropLineTool (StepDistanceCurve, Utilities/PLTMath.cs).
        /// </summary>
        /// <param name="tStart">Starting t factor.</param>
        /// <param name="distance">Distance to travel.</param>
        /// <returns>Target t factor.</returns>
        private float BezierStep(float tStart, float distance)
        {
            const float Tolerance = 0.001f;
            const float ToleranceSquared = Tolerance * Tolerance;

            float tEnd = Travel(tStart, distance);
            float usedDistance = CubicBezierArcLengthXZGauss04(tStart, tEnd);

            // Twelve iteration maximum for performance and to prevent infinite loops.
            for (int i = 0; i < 12; ++i)
            {
                // Stop looping if the remaining distance is less than tolerance.
                float remainingDistance = distance - usedDistance;
                if (remainingDistance * remainingDistance < ToleranceSquared)
                {
                    break;
                }

                usedDistance = CubicBezierArcLengthXZGauss04(tStart, tEnd);
                tEnd += (distance - usedDistance) / CubicSpeedXZ(tEnd);
            }

            return tEnd;
        }

        /// <summary>
        /// Steps along a Bezier BACKWARDS from the given t factor, calculating the target t factor for the given spacing distance.
        /// Code based on Alterran's PropLineTool (StepDistanceCurve, Utilities/PLTMath.cs).
        /// </summary>
        /// <param name="tStart">Starting t factor.</param>
        /// <param name="distance">Distance to travel.</param>
        /// <returns>Target t factor.</returns>
        private float BezierStepReverse(float tStart, float distance)
        {
            const float Tolerance = 0.001f;
            const float ToleranceSquared = Tolerance * Tolerance;

            float tEnd = Travel(tStart, -distance);
            float usedDistance = CubicBezierArcLengthXZGauss04(tEnd, tStart);

            // Twelve iteration maximum for performance and to prevent infinite loops.
            for (int i = 0; i < 12; ++i)
            {
                // Stop looping if the remaining distance is less than tolerance.
                float remainingDistance = distance - usedDistance;
                if (remainingDistance * remainingDistance < ToleranceSquared)
                {
                    break;
                }

                usedDistance = CubicBezierArcLengthXZGauss04(tEnd, tStart);
                tEnd -= (distance - usedDistance) / CubicSpeedXZ(tEnd);
            }

            return tEnd;
        }

        /// <summary>
        /// From Alterann's PropLineTool (CubicSpeedXZ, Utilities/PLTMath.cs).
        /// Returns the integrand of the arc length function for a cubic Bezier curve, constrained to the XZ-plane at a specific t.
        /// </summary>
        /// <param name="t"> t factor.</param>
        /// <returns>Integrand of arc length.</returns>
        private float CubicSpeedXZ(float t)
        {
            // Pythagorean theorem.
            float3 tangent = MathUtils.Tangent(_thisBezier, t);
            float derivXsqr = tangent.x * tangent.x;
            float derivZsqr = tangent.z * tangent.z;

            return math.sqrt(derivXsqr + derivZsqr);
        }

        /// <summary>
        /// From Alterann's PropLineTool (CubicBezierArcLengthXZGauss04, Utilities/PLTMath.cs).
        /// Returns the XZ arclength of a cubic Bezier curve between two t factors.
        /// Uses Gauss–Legendre Quadrature with n = 4.
        /// </summary>
        /// <param name="t1">Starting t factor.</param>
        /// <param name="t2">Ending t factor.</param>
        /// <returns>XZ arc length.</returns>
        private float CubicBezierArcLengthXZGauss04(float t1, float t2)
        {
            float linearAdj = (t2 - t1) / 2f;

            // Constants are from Gauss-Lengendre quadrature rules for n = 4.
            float p1 = CubicSpeedXZGaussPoint(0.3399810435848563f, 0.6521451548625461f, t1, t2);
            float p2 = CubicSpeedXZGaussPoint(-0.3399810435848563f, 0.6521451548625461f, t1, t2);
            float p3 = CubicSpeedXZGaussPoint(0.8611363115940526f, 0.3478548451374538f, t1, t2);
            float p4 = CubicSpeedXZGaussPoint(-0.8611363115940526f, 0.3478548451374538f, t1, t2);

            return linearAdj * (p1 + p2 + p3 + p4);
        }

        /// <summary>
        /// From Alterann's PropLineTool (CubicSpeedXZGaussPoint, Utilities/PLTMath.cs).
        /// </summary>
        /// <param name="x_i">X i.</param>
        /// <param name="w_i">W i.</param>
        /// <param name="a">a.</param>
        /// <param name="b">b.</param>
        /// <returns>Cubic speed.</returns>
        private float CubicSpeedXZGaussPoint(float x_i, float w_i, float a, float b)
        {
            float linearAdj = (b - a) / 2f;
            float constantAdj = (a + b) / 2f;
            return w_i * CubicSpeedXZ((linearAdj * x_i) + constantAdj);
        }

        /// <summary>
        /// Based on CS1's mathematics calculations for Bezier travel.
        /// </summary>
        /// <param name="start">Starting t-factor.</param>
        /// <param name="distance">Distance to travel.</param>
        /// <returns>Ending t-factor.</returns>
        private float Travel(float start, float distance)
        {
            Vector3 startPos = MathUtils.Position(_thisBezier, start);

            if (distance < 0f)
            {
                // Negative (reverse) direction.
                distance = 0f - distance;
                float startT = 0f;
                float endT = start;
                float startDistance = Vector3.SqrMagnitude(_thisBezier.a - (float3)startPos);
                float endDistance = 0f;

                // Eight steps max.
                for (int i = 0; i < 8; ++i)
                {
                    // Calculate current position.
                    float midT = (startT + endT) * 0.5f;
                    Vector3 midpoint = MathUtils.Position(_thisBezier, midT);
                    float midDistance = Vector3.SqrMagnitude(midpoint - startPos);

                    // Check for nearer match.
                    if (midDistance < distance * distance)
                    {
                        endT = midT;
                        endDistance = midDistance;
                    }
                    else
                    {
                        startT = midT;
                        startDistance = midDistance;
                    }
                }

                // We've been using square magnitudes for comparison, so rest to true value.
                startDistance = Mathf.Sqrt(startDistance);
                endDistance = Mathf.Sqrt(endDistance);

                // Check for exact match.
                float fDiff = startDistance - endDistance;
                if (fDiff == 0f)
                {
                    // Exact match found - return that.
                    return endT;
                }

                // Not an exact match - use an interpolation.
                return Mathf.Lerp(endT, startT, Mathf.Clamp01((distance - endDistance) / fDiff));
            }
            else
            {
                // Positive (forward) direction.
                float startT = start;
                float endT = 1f;
                float startDistance = 0f;
                float endDistance = Vector3.SqrMagnitude(_thisBezier.d - (float3)startPos);

                // Eight steps max.
                for (int i = 0; i < 8; ++i)
                {
                    // Calculate current position.
                    float tMid = (startT + endT) * 0.5f;
                    Vector3 midPoint = MathUtils.Position(_thisBezier, tMid);
                    float midDistance = Vector3.SqrMagnitude(midPoint - startPos);

                    // Check for nearer match.
                    if (midDistance < distance * distance)
                    {
                        startT = tMid;
                        startDistance = midDistance;
                    }
                    else
                    {
                        endT = tMid;
                        endDistance = midDistance;
                    }
                }

                // We've been using square magnitudes for comparison, so rest to true value.
                startDistance = Mathf.Sqrt(startDistance);
                endDistance = Mathf.Sqrt(endDistance);

                // Check for exact match.
                float remainder = endDistance - startDistance;
                if (remainder == 0f)
                {
                    // Exact match found - return that.
                    return startT;
                }

                // Not an exact match - use an interpolation.
                return Mathf.Lerp(startT, endT, Mathf.Clamp01((distance - startDistance) / remainder));
            }
        }

        /// <summary>
        /// Draws an angle indicator between two lines.
        /// </summary>
        /// <param name="line1">Line 1.</param>
        /// <param name="line2">Line 2.</param>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        /// <param name="tooltips">Tooltip list.</param>
        /// <param name="cameraController">Active camera controller instance.</param>
        private void DrawAngleIndicator(Line3.Segment line1, Line3.Segment line2, OverlayRenderSystem.Buffer overlayBuffer, List<TooltipInfo> tooltips, CameraUpdateSystem cameraController)
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
