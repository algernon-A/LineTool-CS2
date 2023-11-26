// <copyright file="LineToolUISystem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// </copyright>

namespace LineTool
{
    using System;
    using System.IO;
    using cohtml.Net;
    using Colossal.Logging;
    using Game.Prefabs;
    using Game.SceneFlow;
    using Game.Tools;
    using Game.UI;
    using Unity.Entities;

    /// <summary>
    /// A tool UI system for LineTool.
    /// </summary>
    public sealed partial class LineToolUISystem : UISystemBase
    {
        // Cached references.
        private View _uiView;
        private ToolSystem _toolSystem;
        private LineToolSystem _lineToolSystem;
        private ILog _log;

        // Internal status.
        private bool _toolIsActive = false;

        // UI injection data.
        private string _injectedHTML;
        private string _injectedJS;

        /// <summary>
        /// Called when the system is created.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();

            // Set log.
            _log = Mod.Instance.Log;

            // Set references.
            _uiView = GameManager.instance.userInterface.view.View;
            _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            _lineToolSystem = World.GetOrCreateSystemManaged<LineToolSystem>();

            // Read injection data.
            _injectedHTML = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "UI", "ui.html"), "div.className = \"tool-options-panel_Se6\"; div.id = \"line-tool-spacing\"; document.getElementsByClassName(\"tool-side-column_l9i\")[0].appendChild(div);");
            _injectedJS = UIFileUtils.ReadJS(Path.Combine(UIFileUtils.AssemblyPath, "UI", "ui.js"));

            // Set initial variables in UI (multiply spacing by 10 for accuracy conversion).
            UIFileUtils.ExecuteScript(_uiView, $"var lineToolSpacing = {_lineToolSystem.Spacing * 10};");
            UIFileUtils.ExecuteScript(_uiView, $"var lineToolRotation = {_lineToolSystem.Rotation};");

            // Register event callbacks.
            _uiView.RegisterForEvent("SetLineToolSpacing", (Action<float>)SetSpacing);
            _uiView.RegisterForEvent("SetLineToolRotation", (Action<int>)SetRotation);
            _uiView.RegisterForEvent("SetStraightMode", (Action)SetStraightMode);
            _uiView.RegisterForEvent("SetSimpleCurveMode", (Action)SetSimpleCurveMode);
            _uiView.RegisterForEvent("SetCircleMode", (Action)SetCircleMode);
            _uiView.RegisterForEvent("LineToolTreeControlUpdated", (Action)TreeControlUpdated);

            // Add mod UI resource directory to UI resource handler.
            GameUIResourceHandler uiResourceHandler = GameManager.instance.userInterface.view.uiSystem.resourceHandler as GameUIResourceHandler;
            uiResourceHandler?.HostLocationsMap.Add("linetoolui", new System.Collections.Generic.List<string> { Mod.Instance.AssemblyPath + "/UI" });
        }

        /// <summary>
        /// Called every UI update.
        /// </summary>
        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Check for line tool activation.
            if (_toolSystem.activeTool == _lineToolSystem)
            {
                if (!_toolIsActive)
                {
                    // Tool is now active but previously wasn't; attempt to get game's tool options menu.
                    UIFileUtils.ExecuteScript(_uiView, "var toolOptions = document.getElementsByClassName(\"tool-side-column_l9i\"); if (toolOptions && toolOptions.length > 0) { engine.trigger('ToolOptionsReady', toolOptions[0].innerHTML);}");

                    // Attach our custom controls.
                    // Inject scripts.
                    _log.Debug("injecting component data");
                    UIFileUtils.ExecuteScript(_uiView, _injectedHTML);
                    UIFileUtils.ExecuteScript(_uiView, _injectedJS);

                    // Determine active tool mode.
                    string modeElement = _lineToolSystem.Mode switch
                    {
                        LineMode.SimpleCurve => "line-tool-simplecurve",
                        LineMode.Circle => "line-tool-circle",
                        _ => "line-tool-straight",
                    };

                    // Select active tool button.
                    UIFileUtils.ExecuteScript(_uiView, $"document.getElementById(\"{modeElement}\").classList.add(\"selected\");");

                    // Show tree control menu if tree control is active.
                    if (EntityManager.HasComponent<TreeData>(_lineToolSystem.SelectedEntity))
                    {
                        UIFileUtils.ExecuteScript(_uiView, "addLineToolTreeControl();");
                    }

                    // Record current tool state.
                    _toolIsActive = true;
                }
            }
            else
            {
                // Line tool not active - clean up if this is the first update after deactivation.
                if (_toolIsActive)
                {
                    // Remove DOM activation.
                    UIFileUtils.ExecuteScript(_uiView, "var spacing = document.getElementById(\"line-tool-spacing\"); if (spacing) spacing.parentElement.removeChild(spacing);");

                    // Record current tool state.
                    _toolIsActive = false;
                }
            }
        }

        /// <summary>
        /// Event callback to set current spacing.
        /// </summary>
        /// <param name="spacing">Value to set.</param>
        private void SetSpacing(float spacing) => _lineToolSystem.Spacing = spacing;

        /// <summary>
        /// Event callback to set current rotation.
        /// </summary>
        /// <param name="rotation">Value to set.</param>
        private void SetRotation(int rotation) => _lineToolSystem.Rotation = rotation;

        /// <summary>
        /// Event callback to set straight line mode.
        /// </summary>
        private void SetStraightMode() => _lineToolSystem.Mode = LineMode.Straight;

        /// <summary>
        /// Event callback to set simple curve mode.
        /// </summary>
        private void SetSimpleCurveMode() => _lineToolSystem.Mode = LineMode.SimpleCurve;

        /// <summary>
        /// Event callback to set circle mode.
        /// </summary>
        private void SetCircleMode() => _lineToolSystem.Mode = LineMode.Circle;

        /// <summary>
        /// Event callback to update Tree Control settings.
        /// </summary>
        private void TreeControlUpdated() => _lineToolSystem.RefreshTreeControl();
    }
}
