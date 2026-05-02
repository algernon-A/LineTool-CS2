// <copyright file="ElevationMode.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the Apache Licence, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineTool
{
    /// <summary>
    /// Line tool elevation modes.
    /// </summary>
    public enum ElevationMode
    {
        /// <summary>
        /// Follow terrain elevation.
        /// </summary>
        FollowTerrain,

        /// <summary>
        /// Fixed elevation (terrain elevation at start).
        /// </summary>
        Fixed,

        /// <summary>
        /// Constant slope from start to end terrain elevation.
        /// </summary>
        ConstantSlope,
    }
}
