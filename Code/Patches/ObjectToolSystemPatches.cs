// <copyright file="ObjectToolSystemPatches.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the Apache Licence, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineTool
{
    using Game.Prefabs;
    using Game.Tools;
    using HarmonyLib;
    using Unity.Entities;

    /// <summary>
    /// Harmony patches to <see cref="ObjectToolSystem"/> to overcome restrictions in game support for modding.
    /// </summary>
    [HarmonyPatch(typeof(ObjectToolSystem))]
    internal static class ObjectToolSystemPatches
    {
        /// <summary>
        /// Harmony prefix to <see cref="ObjectToolSystem.TrySetPrefab(PrefabBase)"/> to selectively block execution of the game's method when Line Tool is active.
        /// </summary>
        /// <param name="prefab">Prefab to set.</param>
        /// <returns><c>false</c> (skip game execution) if Line Tool is active and the prefab is appropriate for Line Tool, <c>true</c> otherwise.</returns>
        [HarmonyPatch(nameof(ObjectToolSystem.TrySetPrefab))]
        [HarmonyPrefix]
        internal static bool TrySetPrefabPrefix(ref PrefabBase prefab)
        {
            ToolSystem toolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            LineToolSystem lineTool = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<LineToolSystem>();

            // Don't execute game method if Line Tool is active and the prefab is supported.
            if (toolSystem.activeTool == lineTool && lineTool.TrySetPrefab(prefab))
            {
                return false;
            }

            // If we got here, then we need to execute the game method.
            return true;
        }
    }
}
