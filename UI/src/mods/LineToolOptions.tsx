import { useLocalization } from "cs2/l10n";
import { ModuleRegistry } from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import {Tooltip} from "cs2/ui";

// Boolean update bindings.
export const showModeRow$ = bindValue<boolean>('LineTool', 'ShowModeRow');
export const pointModeEnabled$ = bindValue<boolean>('LineTool', 'PointModeEnabled');
export const straightLineModeEnabled$ = bindValue<boolean>('LineTool', 'StraightLineEnabled');
export const simpleCurveModeEnabled$ = bindValue<boolean>('LineTool', 'SimpleCurveEnabled');
export const circleModeEnabled$ = bindValue<boolean>('LineTool', 'CircleEnabled');
export const gridModeEnabled$ = bindValue<boolean>('LineTool', 'GridEnabled');
export const fenceModeAvailable$ = bindValue<boolean>('LineTool', 'FenceModeAvailable');
export const w2wModeAvailable$ = bindValue<boolean>('LineTool', 'W2WModeAvailable');
export const fenceModeEnabled$ = bindValue<boolean>('LineTool', 'FenceModeEnabled');
export const w2wModeEnabled$ = bindValue<boolean>('LineTool', 'W2WModeEnabled');
export const randomizationEnabled$ = bindValue<boolean>('LineTool', 'RandomizationEnabled');
export const lengthSnapEnabled$ = bindValue<boolean>('LineTool', 'LengthSnapEnabled');
export const fullLengthEnabled$ = bindValue<boolean>('LineTool', 'FullLengthEnabled');
export const absoluteRotationEnabled$ = bindValue<boolean>('LineTool', 'AbsoluteRotationEnabled');

export const relativeRotationEnabled$ = bindValue<boolean>('LineTool', 'RelativeRotationEnabled');

export const randomRotationEnabled$ = bindValue<boolean>('LineTool', 'RandomRotationEnabled');

// Number update bindings.
export const Spacing$ = bindValue<Number>('LineTool', 'Spacing');
export const Rotation$ = bindValue<Number>('LineTool', 'Rotation');
export const SpacingVariation$ = bindValue<Number>('LineTool', 'SpacingVariation');
export const OffsetVariation$ = bindValue<Number>('LineTool', 'OffsetVariation');

// Trigger bindings.
export function pointModeClick() { trigger("LineTool", "SetPointMode"); }
export function straightLineModeClick() { trigger("LineTool", "SetStraightLineMode"); }
export function simpleCurveModeClick() { trigger("LineTool", "SetSimpleCurveMode"); }
export function circleModeClick() { trigger("LineTool", "SetCircleMode"); }
export function gridModeClick() { trigger("LineTool", "SetGridMode"); }
export function fenceModeClick() { trigger("LineTool", "ToggleFenceMode"); }
export function w2wModeClick() { trigger("LineTool", "ToggleW2WMode"); }
export function randomizationClick() { trigger("LineTool", "ToggleRandomization"); }
export function changeRandomClick() { trigger("LineTool", "UpdateRandomSeed"); }
export function lengthSnapClick() { trigger("LineTool", "ToggleLengthSnap"); }
export function fullLengthClick() { trigger("LineTool", "ToggleFullLength"); }
export function spacingUpClick() { trigger("LineTool", "IncreaseSpacing"); }
export function spacingDownClick() { trigger("LineTool", "DecreaseSpacing"); }
export function relativeRotationClick() { trigger("LineTool", "SetRelativeRotation"); }
export function absoluteRotationClick() { trigger("LineTool", "SetAbsoluteRotation"); }
export function randomRotationClick() { trigger("LineTool", "SetRandomRotation"); }
export function rotationUpClick() { trigger("LineTool", "IncreaseRotation"); }
export function rotationDownClick() { trigger("LineTool", "DecreaseRotation"); }
export function spacingVariationUpClick() { trigger("LineTool", "IncreaseSpacingVariation"); }
export function spacingVariationDownClick() { trigger("LineTool", "DecreaseSpacingVariation"); }
export function offsetVariationUpClick() { trigger("LineTool", "IncreaseOffsetVariation"); }
export function offsetVariationDownClick() { trigger("LineTool", "DecreaseOffsetVariation"); }

export const LineToolOptionsComponent = (moduleRegistry: ModuleRegistry) => (Component: any) => {
    return (props: any) => {
        // Component paths.
        const toolButtonModule = moduleRegistry.registry.get("game-ui/game/components/tool-options/tool-button/tool-button.tsx");
        const toolButtonTheme = moduleRegistry.registry.get("game-ui/game/components/tool-options/tool-button/tool-button.module.scss")?.classes;
        const toolMouseModule = moduleRegistry.registry.get("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx");
        const mouseToolTheme = moduleRegistry.registry.get("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.module.scss")?.classes;
        const focusKey = moduleRegistry.registry.get("game-ui/common/focus/focus-key.ts");
        const descriptionTooltipTheme = moduleRegistry.registry.get("game-ui/common/tooltip/description-tooltip/description-tooltip.module.scss")?.classes;
        
        // Components.
        const Section: any = toolMouseModule?.Section;
        const ToolButton: any = toolButtonModule?.ToolButton;
        const FocusDisabled: any = focusKey?.FOCUS_DISABLED;
        
        // General.
        const { translate } = useLocalization();
        const { children, ...otherProps } = props || {};

        // Boolean update bindings.
        const showModeRow: boolean = useValue(showModeRow$);
        const pointModeEnabled: boolean = useValue(pointModeEnabled$);
        const straightLineModeEnabled: boolean = useValue(straightLineModeEnabled$);
        const simpleCurveModeEnabled: boolean = useValue(simpleCurveModeEnabled$);
        const circleModeEnabled: boolean = useValue(circleModeEnabled$);
        const gridModeEnabled: boolean = useValue(gridModeEnabled$);
        const fenceModeEnabled: boolean = useValue(fenceModeEnabled$);
        const w2wModeEnabled: boolean = useValue(w2wModeEnabled$);
        const randomizationEnabled: boolean = useValue(randomizationEnabled$);
        const lengthSnapEnabled: boolean = useValue(lengthSnapEnabled$);
        const fullLengthEnabled: boolean = useValue(fullLengthEnabled$);
        const relativeRotationEnabled: boolean = useValue(relativeRotationEnabled$);
        const absoluteRotationEnabled: boolean = useValue(absoluteRotationEnabled$);
        const randomRotationEnabled: boolean = useValue(randomRotationEnabled$);
        const fenceModeAvailable: boolean = useValue(fenceModeAvailable$);
        const w2wModeAvailable: boolean = useValue(w2wModeAvailable$);

        // Number update bindings.
        const Spacing: Number = useValue(Spacing$);
        const Rotation: Number = useValue(Rotation$);
        const SpacingVariation: Number = useValue(SpacingVariation$);
        const OffsetVariation: Number = useValue(OffsetVariation$);

        // Number display strings.
        const renderedSpacing: string = Spacing.toFixed(1).toString() + " m";
        const renderedRotation: string = Rotation.toFixed(0).toString() + "°";
        const renderedSpacingVariation: string = SpacingVariation.toFixed(1).toString() + " m";
        const renderedOffsetVariation: string = OffsetVariation.toFixed(1).toString() + " m";

        // Titled tooltip generator.
        function  TitledTooltip (titleKey: string, contentKey: string): JSX.Element {
            return (
                <>
                    <div className={descriptionTooltipTheme.title}>{translate(titleKey)}</div>
                    <div className={descriptionTooltipTheme.content}>{translate(contentKey)}</div>
                </>
            )
        }
        
        // Two-paragraph titled tooltip generator.
        function  TitledParaTooltip (titleKey: string, firstParaKey: string, secondParaKey: string): JSX.Element {
            return (
                <>
                    <div className={descriptionTooltipTheme.title}>{translate(titleKey)}</div>
                    <div className={descriptionTooltipTheme.content}>{translate(firstParaKey)}</div>
                    <div className={descriptionTooltipTheme.content}> </div>
                    <div className={descriptionTooltipTheme.content}>{translate(secondParaKey)}</div>
                </>
            )
        }
        
        // Fence mode button.
        function FenceModeButton(): JSX.Element {
            // Only visible if fence mode is available.
            if (fenceModeAvailable) {
                return (
                    <ToolButton
                        className={toolButtonTheme.button}
                        src={"coui://uil/Standard/Fence.svg"}
                        tooltip={TitledTooltip("LINETOOL.FenceMode", "LINETOOL_DESCRIPTION.FenceMode")}
                        onSelect={fenceModeClick}
                        selected={fenceModeAvailable && fenceModeEnabled}
                        multiSelect={false}
                        disabled={!fenceModeAvailable}
                        focusKey={FocusDisabled}
                    />
                )
            }
            else {
                // Fence mode not available - return empty.
                return (<></>)
            }
        }

        // Wall-to-wall mode button.
        function W2WModeButton(): JSX.Element {
            // Only visible if wall-to-wall mode is available.
            if (w2wModeAvailable) {
                return (
                    <ToolButton
                        className={toolButtonTheme.button}
                        src={"coui://uil/Standard/BoxesWallToWall.svg"}
                        tooltip={TitledTooltip("LINETOOL.W2WMode", "LINETOOL_DESCRIPTION.W2WMode")}
                        onSelect={w2wModeClick}
                        selected={w2wModeAvailable && w2wModeEnabled}
                        multiSelect={false}
                        disabled={!w2wModeAvailable}
                        focusKey={FocusDisabled}
                    />
                )
            }
            else {
                // Wall-to-wall mode not available - return empty.
                return (<></>)
            }
        }

        // Show mode row if set.
        let result: JSX.Element = Component();
        if (showModeRow) {
            result.props.children?.push(
                <Section title={ pointModeEnabled ? translate("LINETOOL.Title") : translate("LINETOOL.LineMode")}>
                    <ToolButton
                        className={toolButtonTheme.button}
                        src={"Media/Tools/Net Tool/Point.svg"}
                        tooltip={TitledTooltip("LINETOOL.PointMode","LINETOOL_DESCRIPTION.PointMode")}
                        onSelect={pointModeClick}
                        selected={pointModeEnabled}
                        multiSelect={false}
                        disabled={false}
                        focusKey={FocusDisabled}
                    />
                    <ToolButton
                        className={toolButtonTheme.button}
                        src={"Media/Tools/Object Tool/Line.svg"}
                        tooltip={TitledTooltip("LINETOOL.StraightLine","LINETOOL_DESCRIPTION.PointMode")}
                        onSelect={straightLineModeClick}
                        selected={straightLineModeEnabled}
                        multiSelect={false}
                        disabled={false}
                        focusKey={FocusDisabled}
                    />
                    <ToolButton
                        className={toolButtonTheme.button}
                        src={"Media/Tools/Object Tool/Curve.svg"}
                        tooltip={TitledTooltip("LINETOOL.SimpleCurve", "LINETOOL_DESCRIPTION.SimpleCurve")}
                        onSelect={simpleCurveModeClick}
                        selected={simpleCurveModeEnabled}
                        multiSelect={false}
                        disabled={false}
                        focusKey={FocusDisabled}
                    />
                    <ToolButton
                        className={toolButtonTheme.button}
                        src={"coui://uil/Standard/Circle.svg"}
                        tooltip={TitledTooltip("LINETOOL.Circle", "LINETOOL_DESCRIPTION.Circle")}
                        onSelect={circleModeClick}
                        selected={circleModeEnabled}
                        multiSelect={false}
                        disabled={false}
                        focusKey={FocusDisabled}
                    />
                    <ToolButton
                        className={toolButtonTheme.button}
                        src={"Media/Tools/Net Tool/Grid.svg"}
                        tooltip={TitledTooltip("LINETOOL.Grid", "LINETOOL_DESCRIPTION.Grid")}
                        onSelect={gridModeClick}
                        selected={gridModeEnabled}
                        multiSelect={false}
                        disabled={false}
                        focusKey={FocusDisabled}
                    />
                </Section>
            );

            // Show additional options if anything other than single point mode is selected.
            if (!pointModeEnabled) {
                result.props.children?.push(
                    <>
                        <Section title={translate("LINETOOL.Options")}>
                            <ToolButton
                                className={toolButtonTheme.button}
                                src={"Media/Tools/Snap Options/Distance.svg"}
                                tooltip={TitledTooltip("LINETOOL.LengthSnap", "LINETOOL_DESCRIPTION.LengthSnap")}
                                onSelect={lengthSnapClick}
                                selected={lengthSnapEnabled}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                            {FenceModeButton()}
                            {W2WModeButton()}
                            <ToolButton
                                className={toolButtonTheme.button}
                                src={"coui://uil/Standard/Dice.svg"}
                                tooltip={TitledTooltip("LINETOOL.RandomizationEnabled", "LINETOOL_DESCRIPTION.RandomizationEnabled")}
                                onSelect={randomizationClick}
                                selected={randomizationEnabled}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                            <ToolButton
                                className={toolButtonTheme.button}
                                src={"coui://uil/Standard/Reset.svg"}
                                tooltip={TitledTooltip("LINETOOL.ChangeRandom", "LINETOOL_DESCRIPTION.ChangeRandom")}
                                onSelect={changeRandomClick}
                                selected={false}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                        </Section>
                    </>
                );
                
                // Show spacing and rotation if we're not in fence or wall-to-wall modes (unless we're in grid mode, in which case show it anyway).
                if (gridModeEnabled || (!fenceModeEnabled && !w2wModeEnabled)) {
                    result.props.children?.push(
                        <>
                            <Section title={translate("LINETOOL.Spacing")}
                                     tooltip={translate("LINETOOL_DESCRIPTION.Spacing")}>
                                <ToolButton
                                    className={toolButtonTheme.button}
                                    src={"coui://uil/Standard/MeasureEven.svg"}
                                    tooltip={TitledTooltip("LINETOOL.FixedLength", "LINETOOL_DESCRIPTION.FixedLength")}
                                    onSelect={fullLengthClick}
                                    selected={fullLengthEnabled}
                                    multiSelect={false}
                                    disabled={false}
                                    focusKey={FocusDisabled}
                                />
                                <ToolButton
                                    className={mouseToolTheme.startButton}
                                    src="coui://uil/Standard/ArrowDownThickStroke.svg"
                                    tooltip={TitledParaTooltip("LINETOOL.SpacingDown", "LINETOOL_DESCRIPTION.Spacing", "LINETOOL_DESCRIPTION.SpacingModifiers")}
                                    onSelect={spacingDownClick}
                                    selected={false}
                                    multiSelect={false}
                                    disabled={false}
                                    focusKey={FocusDisabled}
                                />
                                <Tooltip tooltip={translate("LINETOOL_DESCRIPTION.Spacing")}>
                                    <div className={mouseToolTheme.numberField}>{renderedSpacing}</div>
                                </Tooltip>
                                <ToolButton
                                    className={mouseToolTheme.endButton}
                                    src="coui://uil/Standard/ArrowUpThickStroke.svg"
                                    tooltip={TitledParaTooltip("LINETOOL.SpacingUp", "LINETOOL_DESCRIPTION.Spacing", "LINETOOL_DESCRIPTION.SpacingModifiers")}
                                    onSelect={spacingUpClick}
                                    selected={false}
                                    multiSelect={false}
                                    disabled={false}
                                    focusKey={FocusDisabled}
                                />
                            </Section>
                            <Section title={translate("LINETOOL.Rotation")}
                                     tooltip={translate("LINETOOL_DESCRIPTION.Rotation")}>
                                <ToolButton
                                    className={toolButtonTheme.button}
                                    src={"coui://uil/Standard/RotateAngleRelative.svg"}
                                    tooltip={TitledTooltip("LINETOOL.RelativeRotation", "LINETOOL_DESCRIPTION.RelativeRotation")}
                                    onSelect={relativeRotationClick}
                                    selected={relativeRotationEnabled}
                                    multiSelect={false}
                                    disabled={false}
                                    focusKey={FocusDisabled}
                                />
                                <ToolButton
                                    className={toolButtonTheme.button}
                                    src={"coui://uil/Standard/RotateAngleAbsolute.svg"}
                                    tooltip={TitledTooltip("LINETOOL.AbsoluteRotation", "LINETOOL_DESCRIPTION.AbsoluteRotation")}
                                    onSelect={absoluteRotationClick}
                                    selected={absoluteRotationEnabled}
                                    multiSelect={false}
                                    disabled={false}
                                    focusKey={FocusDisabled}
                                />
                                <ToolButton
                                    className={toolButtonTheme.button}
                                    src={"coui://uil/Standard/Dice.svg"}
                                    tooltip={TitledTooltip("LINETOOL.RandomRotation", "LINETOOL_DESCRIPTION.RandomRotation")}
                                    onSelect={randomRotationClick}
                                    selected={randomRotationEnabled}
                                    multiSelect={false}
                                    disabled={false}
                                    focusKey={FocusDisabled}
                                />
                                <ToolButton
                                    className={mouseToolTheme.startButton}
                                    src="coui://uil/Standard/ArrowDownThickStroke.svg"
                                    tooltip={TitledParaTooltip("LINETOOL.AntiClockwise", "LINETOOL_DESCRIPTION.Rotation", "LINETOOL_DESCRIPTION.RotationModifiers")}
                                    onSelect={rotationDownClick}
                                    selected={false}
                                    multiSelect={false}
                                    disabled={randomRotationEnabled}
                                    focusKey={FocusDisabled}
                                />
                                <Tooltip tooltip={translate("LINETOOL_DESCRIPTION.Rotation")}>
                                    <div className={mouseToolTheme.numberField}>{renderedRotation}</div>
                                </Tooltip>
                                <ToolButton
                                    className={mouseToolTheme.endButton}
                                    src="coui://uil/Standard/ArrowUpThickStroke.svg"
                                    tooltip={TitledParaTooltip("LINETOOL.Clockwise", "LINETOOL_DESCRIPTION.Rotation", "LINETOOL_DESCRIPTION.RotationModifiers")}
                                    onSelect={rotationUpClick}
                                    selected={false}
                                    multiSelect={false}
                                    disabled={randomRotationEnabled}
                                    focusKey={FocusDisabled}
                                />
                            </Section>
                        </>
                    );
                }
                
                // Variation rows.
                result.props.children?.push(
                    <>
                        <Section title={translate("LINETOOL.SpacingVariation")}>
                            <ToolButton
                                className={mouseToolTheme.startButton}
                                src="coui://uil/Standard/ArrowDownThickStroke.svg"
                                tooltip={TitledParaTooltip("LINETOOL.RandomSpacingDown", "LINETOOL_DESCRIPTION.SpacingVariation", "LINETOOL_DESCRIPTION.SpacingModifiers")}
                                onSelect={spacingVariationDownClick}
                                selected={false}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                            <Tooltip tooltip={translate("LINETOOL_DESCRIPTION.SpacingVariation")}>
                                <div className={mouseToolTheme.numberField}>{renderedSpacingVariation}</div>
                            </Tooltip>
                            <ToolButton
                                className={mouseToolTheme.endButton}
                                src="coui://uil/Standard/ArrowUpThickStroke.svg"
                                tooltip={TitledParaTooltip("LINETOOL.RandomSpacingUp", "LINETOOL_DESCRIPTION.SpacingVariation", "LINETOOL_DESCRIPTION.SpacingModifiers")}
                                onSelect={spacingVariationUpClick}
                                selected={false}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                                useTooltipParagraph="true"
                            />
                        </Section>
                        <Section title={translate("LINETOOL.OffsetVariation")}>
                            <ToolButton
                                className={mouseToolTheme.startButton}
                                src="coui://uil/Standard/ArrowDownThickStroke.svg"
                                tooltip={TitledParaTooltip("LINETOOL.RandomOffsetUp", "LINETOOL_DESCRIPTION.OffsetVariation", "LINETOOL_DESCRIPTION.SpacingModifiers")}
                                onSelect={offsetVariationDownClick}
                                selected={false}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                            <Tooltip tooltip={translate("LINETOOL_DESCRIPTION.OffsetVariation")}>
                                <div className={mouseToolTheme.numberField}>{renderedOffsetVariation}</div>
                            </Tooltip>
                            <ToolButton
                                className={mouseToolTheme.endButton}
                                src="coui://uil/Standard/ArrowUpThickStroke.svg"
                                tooltip={TitledParaTooltip("LINETOOL.RandomOffsetDown", "LINETOOL_DESCRIPTION.OffsetVariation", "LINETOOL_DESCRIPTION.SpacingModifiers")}
                                onSelect={offsetVariationUpClick}
                                selected={false}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                        </Section>
                    </>
                );
            }
        }

        return result;
    };
}