// <copyright file="ModSettings.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the Apache Licence, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineTool
{
    using Colossal.IO.AssetDatabase;
    using Game.Input;
    using Game.Modding;
    using Game.Settings;

    /// <summary>
    /// The mod's settings.
    /// </summary>
    [FileLocation(Mod.ModName)]
    [SettingsUIMouseAction(ApplyActionName, ActionType.Button, false, false, new string[] { "LineTool" })]
    [SettingsUIMouseAction(CancelActionName, ActionType.Button, false, false, usages: new string[] { "LineTool" })]
    internal class ModSettings : ModSetting
    {
        /// <summary>
        /// Line tool's apply action name.
        /// </summary>
        internal const string ApplyActionName = "LineToolApply";

        /// <summary>
        /// Line tool's apply action name.
        /// </summary>
        internal const string CancelActionName = "LineToolCancel";

        /// <summary>
        /// Initializes a new instance of the <see cref="ModSettings"/> class.
        /// </summary>
        /// <param name="mod"><see cref="IMod"/> instance.</param>
        public ModSettings(IMod mod)
            : base (mod)
        {
        }

        /// <summary>
        /// Gets or sets the Line Tool apply action (copied from game action).
        /// </summary>
        [SettingsUIMouseBinding(ApplyActionName)]
        [SettingsUIBindingMimic(InputManager.kToolMap, "Apply")]
        [SettingsUIHidden]
        public ProxyBinding LineToolApply { get; set; }

        /// <summary>
        /// Gets or sets the Line Tool cancel action (copied from game action).
        /// </summary>
        [SettingsUIMouseBinding(CancelActionName)]
        [SettingsUIBindingMimic(InputManager.kToolMap, "Cancel")]
        [SettingsUIHidden]
        public ProxyBinding LineToolCancel { get; set; }

        /// <summary>
        /// Restores mod settings to default.
        /// </summary>
        public override void SetDefaults()
        {
        }
    }
}
