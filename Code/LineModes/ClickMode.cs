// <copyright file="ClickMode.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the Apache Licence, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineTool
{
    /// <summary>
    /// Line tool modes.
    /// </summary>
    public enum ClickMode
    {
        /// <summary>
        /// Initial click.
        /// </summary>
        Initial,

        /// <summary>
        /// Continuing click.
        /// </summary>
        Midpoint,

        /// <summary>
        /// Placement.
        /// </summary>
        Placed,
    }
}
