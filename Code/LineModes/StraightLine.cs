// <copyright file="StraightLine.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the Apache Licence, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineTool
{
    using UnityEngine;

    /// <summary>
    /// Straight-line placement mode.
    /// </summary>
    public class StraightLine : LineBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StraightLine"/> class.
        /// </summary>
        /// <param name="highPriorityColor">High priority line colour.</param>
        /// <param name="mediumPriorityColor">Medium priority line colour.</param>
        /// <param name="distanceScale">Line width distance scale.</param>
        public StraightLine(Color highPriorityColor, Color mediumPriorityColor, float distanceScale)
            : base(highPriorityColor, mediumPriorityColor, distanceScale)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StraightLine"/> class.
        /// </summary>
        /// <param name="mode">Mode to copy starting state from.</param>
        /// <param name="highPriorityColor">High priority line colour.</param>
        /// <param name="mediumPriorityColor">Medium priority line colour.</param>
        /// <param name="distanceScale">Line width distance scale.</param>
        public StraightLine(LineBase mode, Color highPriorityColor, Color mediumPriorityColor, float distanceScale)
            : base(mode, highPriorityColor, mediumPriorityColor, distanceScale)
        {
        }
    }
}
