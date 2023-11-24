// Function to adjust spacing.
if (typeof adjustSpacing !== 'function') {
    function adjustSpacing(event, adjustment) {
        // Adjust for modifier keys - multiplying adjustment by 10 for FP rounding.
        var finalAdjustment = adjustment;
        if (event) {
            if (event.shiftKey)
                finalAdjustment *= 100;
            else if (!event.ctrlKey)
                finalAdjustment *= 10;
        }

        // Don't apply if adjutment will bring us below zero.
        newSpacing = lineToolSpacing + finalAdjustment;
        if (newSpacing < 1) return;

        // Apply spacing.
        lineToolSpacing = newSpacing;
        var roundedSpacing = newSpacing / 10;
        engine.trigger('SetLineToolSpacing', roundedSpacing);
        document.getElementById("line-tool-spacing-field").innerHTML = roundedSpacing + " m";
    }
}

// Function to adjust rotation.
if (typeof adjustRotation !== 'function') {
    function adjustRotation(event, adjustment) {
        // Adjust for modifier keys.
        var finalAdjustment = adjustment;
        if (event) {
            if (event.shiftKey)
                finalAdjustment *= 100;
            else if (!event.ctrlKey)
                finalAdjustment *= 10;
        }

        // Bounds check rotation.
        lineToolRotation += finalAdjustment;
        if (lineToolRotation > 360) {
            lineToolRotation -= 360;
        }
        if (lineToolRotation < 0) {
            lineToolRotation += 360;
        }

        // Apply rotation.
        engine.trigger('SetLineToolRotation', lineToolRotation);
        document.getElementById("line-tool-rotation-field").innerHTML = lineToolRotation + "&deg;";
    }
}

// Function to show the Tree Control age panel.
if (typeof addLineToolTreeControl !== 'function') {
    function addLineToolTreeControl(event, adjustment) {
        if (typeof buildTreeAgeItem == 'function') {
            var treeControlIntegration = document.createElement("line-tool-tree-control");
            treeControlIntegration.className = "item-content_nNz";
            document.getElementById("line-tool-settings").appendChild(treeControlIntegration);
            buildTreeAgeItem(treeControlIntegration, "beforebegin");
            document.getElementById("YYTC-change-age-buttons-panel").onclick = function() { engine.trigger('LineToolTreeControlUpdated') };
        }
    }
}

// Function to activate straight mode.
if (typeof handleStraightMode !== 'function') {
    function handleStraightMode() {
        document.getElementById("line-tool-simplecurve").classList.remove("selected");
        document.getElementById("line-tool-circle").classList.remove("selected");
        document.getElementById("line-tool-straight").classList.add("selected");
        engine.trigger('SetStraightMode');
    }
}

// Function to activate simple curve mode.
if (typeof handleSimpleCurveMode !== 'function') {
    function handleSimpleCurveMode() {
        document.getElementById("line-tool-straight").classList.remove("selected");
        document.getElementById("line-tool-circle").classList.remove("selected");
        document.getElementById("line-tool-simplecurve").classList.add("selected");
        engine.trigger('SetSimpleCurveMode');
    }
}

// Function to activate circle mode.
if (typeof handleCircleMode !== 'function') {
    function handleCircleMode() {
        document.getElementById("line-tool-straight").classList.remove("selected");
        document.getElementById("line-tool-simplecurve").classList.remove("selected");
        document.getElementById("line-tool-circle").classList.add("selected");
        engine.trigger('SetCircleMode');
    }
}

// Set initial figures.
adjustSpacing(null, 0);
adjustRotation(null, 0);

// Add button event handlers.
document.getElementById("line-tool-spacing-down").onmousedown = (event) => { adjustSpacing(event, -1); }
document.getElementById("line-tool-spacing-up").onmousedown = (event) => { adjustSpacing(event, 1); }

document.getElementById("line-tool-rotation-down").onmousedown = (event) => { adjustRotation(event, -1); }
document.getElementById("line-tool-rotation-up").onmousedown = (event) => { adjustRotation(event, 1); }

document.getElementById("line-tool-straight").onclick = handleStraightMode;
document.getElementById("line-tool-simplecurve").onclick = handleSimpleCurveMode;
document.getElementById("line-tool-circle").onclick = handleCircleMode;
