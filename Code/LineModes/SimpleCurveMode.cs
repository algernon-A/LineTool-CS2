// <copyright file="SimpleCurveMode.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// </copyright>

namespace LineTool
{
    using Colossal.Mathematics;
    using Game.Net;
    using Game.Rendering;
    using Game.Simulation;
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;
    using static Game.Rendering.GuideLinesSystem;

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
        public override void CalculatePoints(float3 currentPos, float spacing, int rotation, NativeList<PointData> pointList, ref TerrainHeightData heightData)
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

            // Rotation quaternion.
            quaternion eulerRotation = quaternion.Euler(0f, rotation, 0f);

            // Create points.
            float tFactor = 0f;
            while (tFactor < 1.0f)
            {
                // Calculate point.
                Vector3 thisPoint = MathUtils.Position(_thisBezier, tFactor);

                // Calculate terrain height.
                thisPoint.y = TerrainUtils.SampleHeight(ref heightData, thisPoint);

                // Add point to list.
                pointList.Add(new PointData { Position = thisPoint, Rotation = eulerRotation, });

                // Get next t factor.
                tFactor = BezierStep(tFactor, spacing);
            }
        }

        /// <summary>
        /// Draws any applicable overlay.
        /// </summary>
        /// <param name="currentPos">Current cursor world position.</param>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        /// <param name="tooltips">Tooltip list.</param>
        public override void DrawOverlay(float3 currentPos, OverlayRenderSystem.Buffer overlayBuffer, NativeList<TooltipInfo> tooltips)
        {
            if (m_validStart)
            {
                // Draw an elbow overlay if we've got valid starting and elbow positions.
                if (m_validElbow)
                {
                    // Calculate lines.
                    Line3.Segment line1 = new (m_startPos, m_elbowPoint);
                    Line3.Segment line2 = new (m_elbowPoint, currentPos);

                    // Draw lines.
                    DrawDashedLine(m_startPos, m_elbowPoint, line1, overlayBuffer, tooltips);
                    DrawDashedLine(m_elbowPoint, currentPos, line2, overlayBuffer, tooltips);

                    // Draw angle.
                    DrawAngleIndicator(line1, line2, 8f, 8f, overlayBuffer, tooltips);
                }
                else
                {
                    // Initial position only; just draw a straight line.
                    base.DrawOverlay(currentPos, overlayBuffer, tooltips);
                }
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
        /// Draws an angle indicator between two lines.
        /// </summary>
        /// <param name="line1">Line 1.</param>
        /// <param name="line2">Line 2.</param>
        /// <param name="lineWidth">Overlay line width.</param>
        /// <param name="lineLength">Overlay line length.</param>
        /// <param name="overlayBuffer">Overlay buffer.</param>
        /// <param name="tooltips">Tooltip list.</param>
        private void DrawAngleIndicator(Line3.Segment line1, Line3.Segment line2, float lineWidth, float lineLength, OverlayRenderSystem.Buffer overlayBuffer, NativeList<TooltipInfo> tooltips)
        {
            bool angleSide = false;

            // Calculate distances.
            float distance1 = math.distance(line1.a.xz, line1.b.xz);
            float distance2 = math.distance(line2.a.xz, line2.b.xz);

            // Minimum line length check.
            if (distance1 > lineWidth * 7f && distance2 > lineWidth * 7f)
            {
                float2 direction1 = (line1.b.xz - line1.a.xz) / distance1;
                float2 direction2 = (line2.a.xz - line2.b.xz) / distance2;
                float size = math.min(lineLength, math.min(distance1, distance2)) * 0.5f;
                int angle = Mathf.RoundToInt(math.degrees(math.acos(math.clamp(math.dot(direction1, direction2), -1f, 1f))));
                if (angle < 180)
                {
                    angleSide = math.dot(MathUtils.Right(direction1), direction2) < 0f;
                }

                DrawAngleIndicator(line1, line2, direction1, direction2, size, lineWidth, angle, angleSide, overlayBuffer, tooltips);
            }
        }

        /// <summary>
        /// Draws an angle indicator.
        /// </summary>
        /// <param name="line1">Line 1.</param>
        /// <param name="line2">Line 2.</param>
        /// <param name="direction1">Direction 1.</param>
        /// <param name="direction2">Direction 2.</param>
        /// <param name="size">Line size.</param>
        /// <param name="lineWidth">Overlay line width.</param>
        /// <param name="angle">Angle to draw.</param>
        /// <param name="angleSide">Angle side.</param>
        /// <param name="buffer">Overlay buffer.</param>
        /// <param name="tooltips">Tooltip list.</param>
        private void DrawAngleIndicator(Line3.Segment line1, Line3.Segment line2, float2 direction1, float2 direction2, float size, float lineWidth, int angle, bool angleSide, OverlayRenderSystem.Buffer buffer, NativeList<TooltipInfo> tooltips)
        {
            if (angle == 180)
            {
                float2 @float = angleSide ? MathUtils.Right(direction1) : MathUtils.Left(direction1);
                float2 float2 = angleSide ? MathUtils.Right(direction2) : MathUtils.Left(direction2);
                float3 b = line1.b;
                b.xz -= direction1 * size;
                float3 b2 = line1.b;
                float3 b3 = line1.b;
                b2.xz += (@float * (size - (lineWidth * 0.5f))) - (direction1 * size);
                b3.xz += (@float * size) - (direction1 * (size + (lineWidth * 0.5f)));
                float3 a = line2.a;
                float3 a2 = line2.a;
                a.xz -= (float2 * size) + (direction2 * (size + (lineWidth * 0.5f)));
                a2.xz -= (float2 * (size - (lineWidth * 0.5f))) + (direction2 * size);
                float3 a3 = line2.a;
                a3.xz -= direction2 * size;
                buffer.DrawLine(Color.white, new Line3.Segment(b, b2), lineWidth);
                buffer.DrawLine(Color.white, new Line3.Segment(b3, a), lineWidth);
                buffer.DrawLine(Color.white, new Line3.Segment(a2, a3), lineWidth);
                float3 b4 = line1.b;
                b4.xz += @float * (size * 1.5f);
                TooltipInfo value = new (TooltipType.Angle, b4, angle);
                tooltips.Add(in value);
            }
            else if (angle > 90)
            {
                float2 float3 = math.normalize(direction1 + direction2);
                float3 b5 = line1.b;
                b5.xz -= direction1 * size;
                float3 startTangent = default;
                startTangent.xz = angleSide ? MathUtils.Right(direction1) : MathUtils.Left(direction1);
                float3 b6 = line1.b;
                b6.xz -= float3 * size;
                float3 float4 = default;
                float4.xz = angleSide ? MathUtils.Right(float3) : MathUtils.Left(float3);
                float3 a4 = line2.a;
                a4.xz -= direction2 * size;
                float3 endTangent = default;
                endTangent.xz = angleSide ? MathUtils.Right(direction2) : MathUtils.Left(direction2);
                buffer.DrawCurve(Color.white, NetUtils.FitCurve(b5, startTangent, float4, b6), lineWidth);
                buffer.DrawCurve(Color.white, NetUtils.FitCurve(b6, float4, endTangent, a4), lineWidth);
                float3 b7 = line1.b;
                b7.xz -= float3 * (size * 1.5f);
                TooltipInfo value = new (TooltipType.Angle, b7, angle);
                tooltips.Add(in value);
            }
            else if (angle == 90)
            {
                float3 b8 = line1.b;
                b8.xz -= direction1 * size;
                float3 b9 = line1.b;
                float3 b10 = line1.b;
                b9.xz -= (direction2 * (size - (lineWidth * 0.5f))) + (direction1 * size);
                b10.xz -= (direction2 * size) + (direction1 * (size + (lineWidth * 0.5f)));
                float3 a5 = line2.a;
                a5.xz -= direction2 * size;
                buffer.DrawLine(Color.white, new Line3.Segment(b8, b9), lineWidth);
                buffer.DrawLine(Color.white, new Line3.Segment(b10, a5), lineWidth);
                float3 b11 = line1.b;
                b11.xz -= math.normalizesafe(direction1 + direction2) * (size * 1.5f);
                TooltipInfo value = new (TooltipType.Angle, b11, angle);
                tooltips.Add(in value);
            }
            else if (angle > 0)
            {
                float3 b12 = line1.b;
                b12.xz -= direction1 * size;
                float3 startTangent2 = default;
                startTangent2.xz = angleSide ? MathUtils.Right(direction1) : MathUtils.Left(direction1);
                float3 a6 = line2.a;
                a6.xz -= direction2 * size;
                float3 endTangent2 = default;
                endTangent2.xz = angleSide ? MathUtils.Right(direction2) : MathUtils.Left(direction2);
                buffer.DrawCurve(Color.white, NetUtils.FitCurve(b12, startTangent2, endTangent2, a6), lineWidth);
                float3 b13 = line1.b;
                b13.xz -= math.normalizesafe(direction1 + direction2) * (size * 1.5f);
                TooltipInfo value = new (TooltipType.Angle, b13, angle);
                tooltips.Add(in value);
            }
        }
    }
}
