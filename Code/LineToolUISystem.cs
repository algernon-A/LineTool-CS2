// <copyright file="LineToolUISystem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// </copyright>

namespace LineTool
{
    using System;
    using System.IO;
    using cohtml.Net;
    using Colossal.Logging;
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
        private bool _gotComponent = false;

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
            _log = Mod.Log;

            // Set references.
            _uiView = GameManager.instance.userInterface.view.View;
            _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            _lineToolSystem = World.GetOrCreateSystemManaged<LineToolSystem>();

            // Read injection data.
            _injectedHTML = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "UI", "ui.html"), "div.className = \"tool-options-panel_Se6\"; div.id = \"line-tool-spacing\"; document.getElementsByClassName(\"tool-side-column_l9i\")[0].appendChild(div);");
            _injectedJS = UIFileUtils.ReadJS(Path.Combine(UIFileUtils.AssemblyPath, "UI", "ui.js"));

            _log.Debug(_injectedHTML);
            _log.Debug(_injectedJS);

            // Set initial spacing variable in UI.
            UIFileUtils.ExecuteScript(_uiView, $"var lineToolSpacing = {_lineToolSystem.Spacing};");

            // Register event callbacks.
            _uiView.RegisterForEvent("ToolOptionsReady", (Action)ToolOptionsReady);
            _uiView.RegisterForEvent("SetLineToolSpacing", (Action<float>)SetSpacing);
            _uiView.RegisterForEvent("SetStraightMode", (Action)SetStraightMode);
            _uiView.RegisterForEvent("SetSimpleCurveMode", (Action)SetSimpleCurveMode);
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

                    // If we were successful in getting the game's tool options menu, attach our custom controls.
                    if (_gotComponent)
                    {
                        // Inject scripts.
                        Mod.Log.Debug("injecting component data");
                        UIFileUtils.ExecuteScript(_uiView, _injectedHTML);
                        UIFileUtils.ExecuteScript(_uiView, _injectedJS);

                        // gotComponent has now been processed).
                        _gotComponent = false;

                        // Record current tool state.
                        _toolIsActive = true;
                    }
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
        private void SetSpacing(float spacing) => World.GetOrCreateSystemManaged<LineToolSystem>().Spacing = spacing;

        /// <summary>
        /// Event callback to indicate that the game's tool UI panel is ready.
        /// </summary>
        private void ToolOptionsReady() => _gotComponent = true;

        /// <summary>
        /// Event callback to set straight line mode.
        /// </summary>
        private void SetStraightMode()
        {
            _log.Info("setting straight line mode");
        }

        /// <summary>
        /// Event callback to set simple curve mode.
        /// </summary>
        private void SetSimpleCurveMode()
        {
            _log.Info("setting simple curve line mode");
        }
    }
}
