import { ModRegistrar } from "cs2/modding";
import { LineToolOptionsComponent } from "mods/LineToolOptions";
import { ToolOptionsVisibility } from "mods/ToolOptionsVisibility";

const register: ModRegistrar = (moduleRegistry) => {
    // Add line tool options to options panel.
    moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', LineToolOptionsComponent(moduleRegistry));


    // Ensures tool option visibility.
    moduleRegistry.extend("game-ui/game/components/tool-options/tool-options-panel.tsx", 'useToolOptionsVisible', ToolOptionsVisibility);
}

export default register;