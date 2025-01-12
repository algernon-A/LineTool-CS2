// <copyright file="ModSettings.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the Apache Licence, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineTool
{
    using Colossal.IO.AssetDatabase;
    using Game.Modding;
    using Game.Settings;
    using Game.UI;

    /// <summary>
    /// The mod's settings.
    /// </summary>
    [FileLocation(Mod.ModName)]
    internal class ModSettings : ModSetting
    {
        private float _guidelineTransparency = 0f;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModSettings"/> class.
        /// </summary>
        /// <param name="mod"><see cref="IMod"/> instance.</param>
        public ModSettings(IMod mod)
            : base(mod)
        {
        }

        /// <summary>
        /// Gets or sets the guideline transparency (0..1).
        /// </summary>
        [SettingsUISlider(min = 0f, max = 100f, step = 1f, scalarMultiplier = 100f, unit = Unit.kPercentage)]
        [SettingsUICustomFormat]
        public float GuidelineTransparency
        {
            get => _guidelineTransparency;
            set
            {
                if (_guidelineTransparency != value)
                {
                    _guidelineTransparency = value;
                    if (LineToolSystem.Instance is not null)
                    {
                        LineToolSystem.Instance.GuidelineTransparency = value;
                    }
                }
            }
        }

        /// <summary>
        /// Restores mod settings to default.
        /// </summary>
        public override void SetDefaults()
        {
            GuidelineTransparency = 0f;
        }
    }
}
