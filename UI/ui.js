// Function to adjust spacing.
if (typeof adjustSpacing !== 'function') {
    function adjustSpacing(adjustment) {
        lineToolSpacing += adjustment;
        engine.trigger('SetLineToolSpacing', lineToolSpacing);
        document.getElementById("line-tool-spacing-field").innerHTML = lineToolSpacing + " m";
    }
}

// Set initial spacing.
adjustSpacing(0);

// Add button event handlers.
document.getElementById("line-tool-spacing-down").onclick = function() { adjustSpacing(-1); }
document.getElementById("line-tool-spacing-up").onclick = function () { adjustSpacing(1); }