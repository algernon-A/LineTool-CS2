// <copyright file="HoverColorsCompatibility.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the Apache Licence, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineTool
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Compatibility checks for Hover Colors.
    /// </summary>
    internal static class CompatibilityHoverColors
    {
        /// <summary>
        /// True once Hover Colors has been detected.
        /// </summary>
        private static bool s_hoverColorsLoaded;

        /// <summary>
        /// Returns true if Hover Colors appears to be loaded.
        /// </summary>
        /// <returns>True if Hover Colors is loaded; otherwise false.</returns>
        internal static bool IsHoverColorsLoaded()
        {
            if (s_hoverColorsLoaded)
            {
                return true;
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    string assemblyName = assembly.GetName().Name ?? string.Empty;

                    if (assemblyName.Equals("HoverColors", StringComparison.OrdinalIgnoreCase)
                        || assemblyName.Equals("Mochi-HoverColors", StringComparison.OrdinalIgnoreCase)
                        || assemblyName.Equals("CS2-Mochi-HoverColors", StringComparison.OrdinalIgnoreCase))
                    {
                        s_hoverColorsLoaded = true;
                        return true;
                    }

                    if (assembly.GetType("HoverColors.Mod", false) is not null
                        || assembly.GetType("HoverColors.Settings.HoverColorsSettings", false) is not null
                        || assembly.GetType("HoverColors.Systems.GuidelineColorSystem", false) is not null)
                    {
                        s_hoverColorsLoaded = true;
                        return true;
                    }
                }
                catch
                {
                    // Ignore assemblies that cannot be inspected.
                }
            }

            return false;
        }
    }
}