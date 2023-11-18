// <copyright file="StraightMode.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// </copyright>

namespace LineTool
{
    /// <summary>
    /// Straight-line placement mode.
    /// </summary>
    public class StraightMode : LineModeBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StraightMode"/> class.
        /// </summary>
        public StraightMode()
        {
            // Basic state.
            m_validStart = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StraightMode"/> class.
        /// </summary>
        /// <param name="mode">Mode to copy starting state from.</param>
        public StraightMode(LineModeBase mode)
            : base(mode)
        {
        }
    }
}
