This mod enhances existing tools so you can precisely and quickly place objects such as trees, shrubs, and props in lines, curves, and circles with varying parameters. You can also place simple buildings and even vehicles (for amusement value only - see limitations below).

## Features

- **Integrates directly with in-game tools**: No hotkey required.
- **Place objects in lines**: Create straight lines, curves, or circles.
- Works both *in-game* and *in the editor*.
- **Fence mode**: Automatically align and place objects end-to-end.
- **Accurate placement**: No worrying about imprecision.
- **Adjust spacing and rotation**: Use the in-game tool UI for more control, including random rotation for a more natural look.
- **Random position variation** (optional): Provides natural irregularity.
- **Live preview**: Provisionally place a line, adjust spacing and/or rotation, and see how it looks in real-time before making the final placement (or cancelling).
- Displays distances and angles for fine-tuning.

## Start and stop Line Tool Lite

- To activate the tool, select the object that you'd like to place in a line (such as a tree), either normally or with the Dev UI. Then, select a line mode from the line modes options at the bottom of the tool options panel (below Tool Mode; see image above).
- To exit the tool, select 'single placement mode' to return to the normal placement mode for the selected object, or press **Escape**.

## Place a line

- **Click** where you want the line to begin, and click again at the desired endpoint to place the objects. **Note**: Curves require three clicks - start, guidepoint, and end.
- **Shift-click** at the end of a line starts a new line placement at the spot where the previous line ended.
- **Control-click** at the end of a line leaves it in preview mode; you can adjust the settings and see the results in real-time. You can also drag the highlighted control points (blue circles) to adjust the line positioning (**Control-click** to start dragging ensures that you don't accidentally trigger placement if you miss the point circles, but regular clicking also works). When finished, **click** to place or **right-click** to cancel.
- **Right-click** to cancel placement.

### Tool options

- Toggle **fence mode** to align objects with the line direction, and place them continuously end-to-end.
- Toggle between **straight line**, **curved**, and **circle** modes.
- Adjust **distances** using the arrow buttons - click for 1m (approximately 1 yard) increments, **Shift-click** for 10m, and **Control-click** for 0.1m. For circle mode, spacing is rounded *up* to the nearest distance that ensures an even placement around the circle.
- Select **fixed-length even spacing mode** to space out objects evenly over the entire length of the line, with spacing *as close as possible* to the spacing distance you set.  For circle mode, this causes spacing to be rounded to the *nearest number* (up or down) that ensures an even placement around the circle (default circle rounding is always *up*).
- Select **random rotation** to have each object in the line have a different randomly-chosen rotation, or manually adjust the rotation for all items using the arrow buttons - click for 10-degree increments, **Shift-click** for 90 degrees, **Control-click** for 1 degree.
- Set **variable spacing** greater than zero to apply a random length offset to each item's spacing, up to the maximum distance specified - click for 1m increments, **Shift-click** for 10m, **Control-click** for 0.1m.
- Set **variable offset** greater than zero to apply a random sideways offset to each item, up to the maximum distance specified - click for 1m increments, **Shift-click** for 10m, **Control-click** for 0.1m.

To remove variable spacing and/or offset, set the field(s) back to zero. **Shift-click** (10m increments) to make this faster.

## Limitations

Sub-objects aren't yet supported, so if you use this to place buildings they won't have any of their sub-components (such as props, embedded networks, or sub-buildings). The full version of this mod rectifies this.

Some specialized buildings can cause issues/and crashes, such as carparks.

Vehicles *can* be placed, but will get confused and likely do strange things, which does have its amusement value at least. The full version will allow you to place vehicles as 'props' (so they just sit there and look pretty, and not e.g. rise up nose-first into the sky in various geometric patterns).

## Translations

This mod supports localization. Please help out translating this mod into different languages!