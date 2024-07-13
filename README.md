## Line Tool
A mod for Cities: Skylines 2.  Available for download only on [Paradox Mods](https://mods.paradoxplaza.com/mods/75816/Windows).

The primary platform for support is the [Cities Skylines Modding Discord](https://discord.gg/HTav7ARPs2).

## Description
This mod supplements existing tools so you can precisely and quickly place objects such as trees, shrubs, and props in lines, curves, and circles with varying parameters.

## Features
- **Integrates directly with in-game tools**: No hotkey required.
- **Place objects in lines**: Create straight lines, curves, or circles.
- Works both *in-game* and *in the editor*.
- **Fence mode**: Automatically align and place objects end-to-end.
- **Wall-to-wall mode**: Automatically align and place objects side-to-side.
- **Appearance randomization controls**: Settings to control object appearance randomization.
- **Accurate placement**: No worrying about imprecision.
- **Adjust spacing and rotation**: Use the in-game tool UI for more control, including random rotation for a more natural look.
- **Random position variation** (optional): Provides natural irregularity.
- **Live preview**: Provisionally place a line, adjust spacing and/or rotation, and see how it looks in real-time before making the final placement (or cancelling).
- Displays distances and angles for fine-tuning.

# Usage
- To activate the tool, select the object that you'd like to place in a line (such as a tree), either normally or with the Dev UI. Then, select a line mode from the line modes options at the bottom of the tool options panel.
- To exit the tool, select 'single placement mode' to return to the normal placement mode for the selected object, or press **Escape**.

## Place a line
- **Click** where you want the line to begin, and click again at the desired endpoint to place the objects. **Note**: Curves require three clicks - start, guidepoint, and end.
- **Shift-click** at the end of a line starts a new line placement at the spot where the previous line ended.
- **Control-click** at the end of a line leaves it in preview mode; you can adjust the settings and see the results in real-time. You can also drag the highlighted control points (blue circles) to adjust the line positioning (**Control-click** to start dragging ensures that you don't accidentally trigger placement if you miss the point circles, but regular clicking also works). When finished, **click** to place or **right-click** to cancel.
- **Right-click** to cancel placement.

## Tool options
### Line modes
- Toggle between **single object** (default game tool), **straight line**, **curved**, and **circle** modes.
- Toggle **fence mode** to align objects with the line direction, and place them continuously end-to-end (like a fence).
- Toggle **wall-to-wall mode** to align objects with the line direction, and place them continuously side-to-side (like a row of buildings).

 **Note**: fence mode and/or wall-to-wall mode are not available for all prefabs (and those options will be hidden if they're not available).  This occurs when the underlying prefab doesn't have a mesh that can be used to properly calculate dimensions (e.g. parking lots).

### Appearance randomization
- Enable **randomization** to randomize the appearance of objects to be placed (within the normal range for that object), or **disable** randomization to have all objects take their default appearance.
- Use the **change fixed random seed** to change the appearance of objects to place to the next random option.

### Object placement spacing
- Adjust **distances** using the arrow buttons - click for 1m increments, **Shift-click** for 10m, and **Control-click** for 0.1m. For circle mode, spacing is rounded *up* to the nearest distance that ensures an even placement around the circle.
- Select **fixed-length even spacing mode** to space out objects evenly over the entire length of the line, with spacing *as close as possible* to the spacing distance you set.  For circle mode, this causes spacing to be rounded to the *nearest number* (up or down) that ensures an even placement around the circle (default circle rounding is always *up*).

### Object rotation (angles)
- Manually adjust the rotation for all items using the arrow buttons - click for 10-degree increments, **Shift-click** for 90 degrees, **Control-click** for 1 degree.
- Select **relative rotation** to make the object face along the line direction (following any curves).
- Select **absolute rotation** to make the objects all face in the same direction (ignoring the line direction).
- Select **random rotation** to have each object in the line have a different randomly-chosen rotation.

### Placement randomization
- Set **variable spacing** greater than zero to apply a random length offset to each item's spacing, up to the maximum distance specified - click for 1m increments, **Shift-click** for 10m, **Control-click** for 0.1m.
- Set **variable offset** greater than zero to apply a random sideways offset to each item, up to the maximum distance specified - click for 1m increments, **Shift-click** for 10m, **Control-click** for 0.1m.
- To remove variable spacing and/or offset, set the field(s) back to zero. **Shift-click** (10m increments) to make this faster.

## Meta
### Translations
This mod supports localization via a [CrowdIn project](https://crowdin.com/project/line-tool-cs2).  Please help out if you can!

### Modders
Modders (and aspiring modders!), as always I'm available and happy to chat about what I've done and answer any questions, and also about how you can implement anything that I've done for your own mods.  Come grab me on the [Cities Skylines Modding Discord](https://discord.gg/HTav7ARPs2)!

Pull requests welcome! Note that translations should be submitted via CrowdIn (see link above), and not by PR.

### Credits
Incorporates overlay improvements and enhancements by [phillycheeze](https://github.com/phillycheeze/).

Special thanks to [yenyang](https://github.com/yenyang/) for various tips and advice!

Thumbnail design by Macwelshman.

This mod uses the [Harmony Patching Library](url=https://github.com/pardeike/Harmony]Harmony[/url) by Andreas Pardeike.