// Function to adjust spacing.
if (typeof adjustSpacing !== 'function') {
    function adjustSpacing(adjustment) {
        lineToolSpacing += adjustment;
        engine.trigger('SetLineToolSpacing', lineToolSpacing);
        document.getElementById("line-tool-spacing-field").innerHTML = lineToolSpacing + " m";
    }
}

if (typeof straightMode !== 'function') {
    function straightMode() {
        document.getElementById("line-tool-simplecurve").classList.remove("selected");
        document.getElementById("line-tool-straight").classList.add("selected");
        engine.trigger('SetStraightMode');
    }
}

if (typeof simpleCurveMode !== 'function') {
    function simpleCurveMode() {
        document.getElementById("line-tool-straight").classList.remove("selected");
        document.getElementById("line-tool-simplecurve").classList.add("selected");
        engine.trigger('SetSimpleCurveMode');
    }
}

// Set initial spacing.
adjustSpacing(0);

// Add button event handlers.
document.getElementById("line-tool-spacing-down").onclick = function() { adjustSpacing(-1); }
document.getElementById("line-tool-spacing-up").onclick = function () { adjustSpacing(1); }

document.getElementById("line-tool-straight").onclick = straightMode;
document.getElementById("line-tool-simplecurve").onclick = simpleCurveMode;