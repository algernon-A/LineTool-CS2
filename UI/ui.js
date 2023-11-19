// Function to adjust spacing.
if (typeof adjustSpacing !== 'function') {
    function adjustSpacing(adjustment) {
        lineToolSpacing += adjustment;
        engine.trigger('SetLineToolSpacing', lineToolSpacing);
        document.getElementById("line-tool-spacing-field").innerHTML = lineToolSpacing + " m";
    }
}

if (typeof handleStraightMode !== 'function') {
    function handleStraightMode() {
        document.getElementById("line-tool-simplecurve").classList.remove("selected");
        document.getElementById("line-tool-circle").classList.remove("selected");
        document.getElementById("line-tool-straight").classList.add("selected");
        engine.trigger('SetStraightMode');
    }
}

if (typeof handleSimpleCurveMode !== 'function') {
    function handleSimpleCurveMode() {
        document.getElementById("line-tool-straight").classList.remove("selected");
        document.getElementById("line-tool-circle").classList.remove("selected");
        document.getElementById("line-tool-simplecurve").classList.add("selected");
        engine.trigger('SetSimpleCurveMode');
    }
}

if (typeof handleCircleMode !== 'function') {
    function handleCircleMode() {
        document.getElementById("line-tool-straight").classList.remove("selected");
        document.getElementById("line-tool-simplecurve").classList.remove("selected");
        document.getElementById("line-tool-circle").classList.add("selected");
        engine.trigger('SetCircleMode');
    }
}

// Set initial spacing.
adjustSpacing(0);

// Add button event handlers.
document.getElementById("line-tool-spacing-down").onclick = function() { adjustSpacing(-1); }
document.getElementById("line-tool-spacing-up").onclick = function () { adjustSpacing(1); }

document.getElementById("line-tool-straight").onclick = handleStraightMode;
document.getElementById("line-tool-simplecurve").onclick = handleSimpleCurveMode;
document.getElementById("line-tool-circle").onclick = handleCircleMode;