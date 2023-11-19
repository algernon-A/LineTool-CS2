// <copyright file="SimpleCurveMode.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
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

    /// <summary>
    /// Simple curve placement mode.
    /// </summary>
    public class SimpleCurveMode : LineModeBase
    {
        // Current elbow point.
        private bool m_validElbow = false;
        private float3 m_elbowPoint;

        // Calculated Bezier.]
        private Bezier4x3 _thisBezier;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleCurveMode"/> class.
        /// </summary>
        /// <param name="mode">Mode to copy starting state from.</param>
        public SimpleCurveMode(LineModeBase mode)
            : base(mode)
        {
        }

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
                m_validStart = true;
                return false;
            }

            // Otherwise, if no valid elbow point, record this as the elbow point.
            if (!m_validElbow)
            {
                m_elbowPoint = position;
                m_validElbow = true;
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
            m_validElbow = false;
        }

        /// <summary>
        /// Calculates the points to use based on this mode.
        /// </summary>
        /// <param name="currentPos">Selection current position.</param>
        /// <param name="spacing">Spacing setting.</param>
        /// <param name="rotation">Rotation setting.</param>
        /// <param name="pointList">List of points to populate.</param>
        /// <param name="heightData">Terrain height data reference.</param>
        public override void CalculatePoints(float3 currentPos, float spacing, float rotation, List<PointData> pointList, ref TerrainHeightData heightData)
        {
            // Don't do anything if we don't have valid start.
            if (!m_validStart)
            {
                return;
            }

            // If we have a valid start but no valid elbow, just draw a straight line.
            if (!m_validElbow)
            {
                base.CalculatePoints(currentPos, spacing, rotation, pointList, ref heightData);
                return;
            }

            // Calculate bezier.
            _thisBezier = NetUtils.FitCurve(new Line3.Segment(m_startPos, m_elbowPoint), new Line3.Segment(currentPos, m_elbowPoint));

            // Create points.
            float tFactor = 0f;
            while (tFactor < 1.0f)
            {
                // Calculate point.
                Vector3 thisPoint = MathUtils.Position(_thisBezier, tFactor);

                // Calculate terrain height.
                thisPoint.y = TerrainUtils.SampleHeight(ref heightData, thisPoint);

                // Add point to list.
                pointList.Add(new PointData { Position = thisPoint, Rotation = quaternion.identity, });

                // Get next t factor.
                tFactor = BezierStep(tFactor, spacing);
            }
        }

        /// <summary>
        /// Draws any applicable overlay.
        /// </summary>
        /// <param name="currentPos">Current cursor world position.</param>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        public override void DrawOverlay(float3 currentPos, OverlayRenderSystem.Buffer overlayBuffer)
        {
            // Draw an elbow overlay if we've got valid starting and elbow positions.
            if (m_validStart && m_validElbow)
            {
                DrawDashedLine(m_startPos, m_elbowPoint, overlayBuffer);
                DrawDashedLine(m_elbowPoint, currentPos, overlayBuffer);
            }
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public override void Reset()
        {
            // Only clear elbow if we have one.
            if (m_validElbow)
            {
                m_validElbow = false;
            }
            else
            {
                base.Reset();
            }
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
        /// Steps along a Bezier BACKWARDS from the end point, calculating the target t factor for the given spacing distance.
        /// Code based on Alterran's PropLineTool (StepDistanceCurve, Utilities/PLTMath.cs).
        /// </summary>
        /// <param name="distance">Distance to travel.</param>
        /// <returns>Target t factor.</returns>
        private float BezierStepReverse(float distance)
        {
            const float Tolerance = 0.001f;
            const float ToleranceSquared = Tolerance * Tolerance;

            float tEnd = Travel(1, -distance);
            float usedDistance = CubicBezierArcLengthXZGauss04(tEnd, 1.0f);

            // Twelve iteration maximum for performance and to prevent infinite loops.
            for (int i = 0; i < 12; ++i)
            {
                // Stop looping if the remaining distance is less than tolerance.
                float remainingDistance = distance - usedDistance;
                if (remainingDistance * remainingDistance < ToleranceSquared)
                {
                    break;
                }

                usedDistance = CubicBezierArcLengthXZGauss04(tEnd, 1.0f);
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
            Vector3 vector = MathUtils.Position(_thisBezier, start);
            float num;
            float num2;
            float f;
            float f2;
            if (distance < 0f)
            {
                distance = 0f - distance;
                num = 0f;
                num2 = start;

                f = Vector3.SqrMagnitude(_thisBezier.a - (float3)vector);

                f2 = 0f;
                for (int i = 0; i < 8; i++)
                {
                    float num3 = (num + num2) * 0.5f;
                    Vector3 vector2 = MathUtils.Position(_thisBezier, num3);

                    float num4 = Vector3.SqrMagnitude(vector2 - vector);

                    if (num4 < distance * distance)
                    {
                        num2 = num3;
                        f2 = num4;
                    }
                    else
                    {
                        num = num3;
                        f = num4;
                    }
                }

                f = Mathf.Sqrt(f);
                f2 = Mathf.Sqrt(f2);
                float num5 = f - f2;
                if (num5 == 0f)
                {
                    return num2;
                }

                return Mathf.Lerp(num2, num, Mathf.Clamp01((distance - f2) / num5));
            }

            num = start;
            num2 = 1f;
            f = 0f;
            f2 = Vector3.SqrMagnitude(_thisBezier.d - (float3)vector);
            for (int j = 0; j < 8; j++)
            {
                float num6 = (num + num2) * 0.5f;

                Vector3 vector3 = MathUtils.Position(_thisBezier, num6);

                float num7 = Vector3.SqrMagnitude(vector3 - vector);

                if (num7 < distance * distance)
                {
                    num = num6;
                    f = num7;
                }
                else
                {
                    num2 = num6;
                    f2 = num7;
                }
            }

            f = Mathf.Sqrt(f);
            f2 = Mathf.Sqrt(f2);
            float num8 = f2 - f;
            if (num8 == 0f)
            {
                return num;
            }

            return Mathf.Lerp(num, num2, Mathf.Clamp01((distance - f) / num8));
        }

        /// <summary>
        /// Draws a dashed line overlay between the two given points.
        /// </summary>
        /// <param name="startPos">Line start position.</param>
        /// <param name="endPos">Line end position.</param>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        private void DrawDashedLine(float3 startPos, float3 endPos, OverlayRenderSystem.Buffer overlayBuffer)
        {
            const float LineWidth = 1f;

            Line3.Segment segment = new (startPos, endPos);
            float distance = math.distance(startPos.xz, endPos.xz);

            // Don't draw lines for short distances.
            if (distance > LineWidth * 8f)
            {
                // Offset segment, mimicing game simple curve overlay, to ensure dash spacing.
                float3 offset = (segment.b - segment.a) * (LineWidth * 4f / distance);
                Line3.Segment line = new (segment.a + offset, segment.b - offset);

                // Draw line - distance figures mimic game simple curve overlay.
                overlayBuffer.DrawDashedLine(Color.white, line, LineWidth * 3f, LineWidth * 5f, LineWidth * 3f);
            }
        }
    }
}
