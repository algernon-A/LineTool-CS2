import { ModRegistrar } from "cs2/modding";
import { LineToolOptionsComponent } from "mods/LineToolOptions";

const register: ModRegistrar = (moduleRegistry) => {
    // Add line tool options to options panel.
    moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', LineToolOptionsComponent(moduleRegistry));
}

export default register;