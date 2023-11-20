// <copyright file="LineToolTooltipSystem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// </copyright>

namespace LineTool
{
    using System.Collections.Generic;
    using Game.Rendering;
    using Game.UI.Tooltip;
    using Game.UI.Widgets;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine.Scripting;

    /// <summary>
    /// The Line Tool tooltip system.
    /// </summary>
    public partial class LineToolTooltipSystem : TooltipSystemBase
    {
        private LineToolSystem _lineToolSystem;
        private List<TooltipGroup> _tooltipGroups;

        /// <summary>
        /// Called when the system is created.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();

            _lineToolSystem = World.GetOrCreateSystemManaged<LineToolSystem>();
            _tooltipGroups = new List<TooltipGroup>();
        }

        /// <summary>
        /// Called every tool update.
        /// </summary>
        protected override void OnUpdate()
        {
            NativeList<GuideLinesSystem.TooltipInfo> tooltips = _lineToolSystem.Tooltips;

            for (int i = 0; i < tooltips.Length; i++)
            {
                GuideLinesSystem.TooltipInfo tooltipInfo = tooltips[i];
                if (_tooltipGroups.Count <= i)
                {
                    _tooltipGroups.Add(new TooltipGroup
                    {
                        path = $"guideLineTooltip{i}",
                        horizontalAlignment = TooltipGroup.Alignment.Center,
                        verticalAlignment = TooltipGroup.Alignment.Center,
                        children = { (IWidget)new IntTooltip() },
                    });
                }

                TooltipGroup tooltipGroup = _tooltipGroups[i];
                float2 @float = TooltipSystemBase.WorldToTooltipPos(tooltipInfo.m_Position);
                if (!tooltipGroup.position.Equals(@float))
                {
                    tooltipGroup.position = @float;
                    tooltipGroup.SetChildrenChanged();
                }

                IntTooltip intTooltip = tooltipGroup.children[0] as IntTooltip;
                switch (tooltipInfo.m_Type)
                {
                    case GuideLinesSystem.TooltipType.Angle:
                        intTooltip.icon = "Media/Glyphs/Angle.svg";
                        intTooltip.value = tooltipInfo.m_IntValue;
                        intTooltip.unit = "angle";
                        break;
                    case GuideLinesSystem.TooltipType.Length:
                        intTooltip.icon = "Media/Glyphs/Length.svg";
                        intTooltip.value = tooltipInfo.m_IntValue;
                        intTooltip.unit = "length";
                        break;
                }

                AddGroup(tooltipGroup);
            }
        }
    }
}
