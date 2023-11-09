// Spacing setting.
var spacing = 10;

// Function to set the given element's visibility.
if (typeof setVisibility !== 'function') {
    function setVisibility(elementID, isVisible) {
        var element = document.getElementById(elementID);
        if (element) {
            if (isVisible)
                element.classList.remove('hidden');
            else
                element.classList.add('hidden');
        }
    }
}

// Function to adjust spacing.
if (typeof adjustSpacing !== 'function') {
    function adjustSpacing(adjustment) {
        spacing += adjustment;
        engine.trigger('AdjustSpacing', spacing);
        document.getElementById("spacing-field").innerHTML = spacing + " m";
    }
}

// Set initial spacing.
adjustSpacing(0);

// Add button event handlers.
document.getElementById("down-arrow").onclick = function() { adjustSpacing(-1); }
document.getElementById("up-arrow").onclick = function () { adjustSpacing(1); }
