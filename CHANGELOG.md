### 1.1.5
- Add support for prefabs with no meshes.

### 1.1.4
- Update UI dependency versions to match game version 1.3.3.

### 1.1.3
- Update UI dependency versions to match game version 1.2.5.

### 1.1.2
- Remove support for buildings (hopefully temporarily) due to recent game changes causing instability.

### 1.1.1
- Add continuation constraint cancellation as a separate step before cancelling entire line.

### 1.1.0.1
- Fix input binding conflicts blocking entry to fixed preview mode.

### 1.1
- Add guideline transparency control.
- Add cancel action fallback to point mode.
- Tweak curved fence mode to more closely match desired final point.
- Fix continuous fence mode last item places for fences with asymmetrical Z-bounds (some UK pack props).

### 1.0.1.2
- Fix fence mode sometimes leaving detached end pieces.
- Update apply and cancel button handling.

### 1.0.1.1
- Update translations.

### 1.0.1
- Clamp variation offsets to positive numbers.
- Skip any objects whose offset places them outside the grid area in grid mode.

### 1.0
- Rebrand to 'Advanced Line Tool' to distinguish it from game tool.
- Add grid placement mode.
- Add snap-to-length.
- Add snap-to-angle for curves and grids.
- Change straight line and curved line icons to match new game style.
- Change overlay style to match new game style.
- Add sound effects.

### 0.9.8.6
- Update for game version 1.2.0.

### 0.9.8.5
- Enable fence mode for items with asymmetrical Z-bounds (fixes fence mode not being available for some UK pack props).

### 0.9.8.4
- Update for game version 1.1.12f1.

### 0.9.8.3
- Update translations.

### 0.9.8.2
- Add Portuguese translation.

### 0.9.8.1
- Fix apply action sometimes not working in editor.

### 0.9.8
- Update for game version 1.1.10f1.

### 0.9.7
- Always use relative rotation for fence or wall-to-wall mode.

### 0.9.6
- Hide Fence Mode and/or Wall-to-Wall Mode buttons if the selected prefab's mesh doesn't have an effective size in the relevant axis.

### 0.9.6
- Update for game version 1.2.0.

### 0.9.5
- Remove settings UI registration.

### 0.9.4
- Update for game version 1.1.6.
- Add decimal place display for spacing and offset.

### 0.9.3
- Replace Harmony patching with direct tool list manipulation.

### 0.9.2
- Update translations.

### 0.9.1
- Improve angle indicator overlay display and other overlay tweaks (credit: phillycheeze).
- Fix fixed-preview line selection circles being shown after cancellation where cursor is over the UI.

### 0.9
- Add absolute/relative rotation selection.
- Add dynamic scaling of overlay guidelines based on zoom level (credit: phillycheeze).
- Add additional guidelines for curves and circles (credit: phillycheeze).

### 0.8
- Add object appearance randomization controls.

### 0.7
- Remove tool change frame delay.
- Improve prefab change handling.

### 0.6
- Add wall-to-wall mode.
- Use consistent random seed in fence mode.
- Adjust fence mode placement for smoother curves.

### 0.5
- Update tooltips
- Update localization framework