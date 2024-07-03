// <copyright file="ToolbarUISystemPatches.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the Apache Licence, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineTool
{
    using Colossal.UI.Binding;
    using Game;
    using Game.Tools;
    using Game.UI.InGame;
    using HarmonyLib;
    using static Game.GameModeExtensions;

    /// <summary>
    /// Harmony patches for <see cref="ToolbarUISystem"/> to ensure visibility of tree age selection buttons when Line Tool is active.
    /// </summary>
    [HarmonyPatch(typeof(ToolbarUISystem))]
    internal static class ToolbarUISystemPatches
    {
        /// <summary>
        /// Harmony postfix for <c>ToolbarUISystem.Apply</c> to ensure that tree age selection controls remain visible if Line Tool is selected (and the selected prefab is a tree).
        /// </summary>
        /// <param name="___m_AgeMaskBinding"><see cref="ToolbarUISystem"/> private field <c>m_AgeMaskBinding</c> (UI tree age control binding).</param>
        [HarmonyPatch("Apply")]
        [HarmonyPostfix]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony patching syntax")]
        internal static void ApplyPostfix(ValueBinding<int> ___m_AgeMaskBinding)
        {
            // Get tool system.
            if (LineToolSystem.Instance.World.GetOrCreateSystemManaged<ToolSystem>() is ToolSystem toolSystem && toolSystem.actionMode.IsGame())
            {
                Patcher.Instance.Log.Debug("ToolbarUISystem.Apply postfix with active toolSystem in game");

                // Check if we're in-game, Line Tool is active, and a tree is currently selected.
                if (toolSystem.activeTool is LineToolSystem lineToolSystem && lineToolSystem.TreeSelected)
                {
                    // All checks met - force an update of the UI binding with Line Tool's active tree age mask.
                    Patcher.Instance.Log.Debug($"ToolbarUISystem.Apply postfix updating AgeMaskBinding with {lineToolSystem.AgeMask}");
                    ___m_AgeMaskBinding.Update((int)lineToolSystem.AgeMask);
                }
            }
        }
    }
}
