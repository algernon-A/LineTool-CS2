import { useLocalization } from "cs2/l10n";
import { ModuleRegistry } from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";

// Boolean update bindings.
export const showModeRow$ = bindValue<boolean>('LineTool', 'ShowModeRow');
export const pointModeEnabled$ = bindValue<boolean>('LineTool', 'PointModeEnabled');
export const straightLineModeEnabled$ = bindValue<boolean>('LineTool', 'StraightLineEnabled');
export const simpleCurveModeEnabled$ = bindValue<boolean>('LineTool', 'SimpleCurveEnabled');
export const circleModeEnabled$ = bindValue<boolean>('LineTool', 'CircleEnabled');
export const fenceModeEnabled$ = bindValue<boolean>('LineTool', 'FenceModeEnabled');
export const fullLengthEnabled$ = bindValue<boolean>('LineTool', 'FullLengthEnabled');
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
export function fenceModeClick() { trigger("LineTool", "ToggleFenceMode"); }
export function fullLengthClick() { trigger("LineTool", "ToggleFullLength"); }
export function spacingUpClick() { trigger("LineTool", "IncreaseSpacing"); }
export function spacingDownClick() { trigger("LineTool", "DecreaseSpacing"); }
export function randomRotationClick() { trigger("LineTool", "ToggleRandomRotation"); }
export function rotationUpClick() { trigger("LineTool", "IncreaseRotation"); }
export function rotationDownClick() { trigger("LineTool", "DecreaseRotation"); }
export function spacingVariationUpClick() { trigger("LineTool", "IncreaseSpacingVariation"); }
export function spacingVariationDownClick() { trigger("LineTool", "DecreaseSpacingVariation"); }
export function offsetVariationUpClick() { trigger("LineTool", "IncreaseOffsetVariation"); }
export function offsetVariationDownClick() { trigger("LineTool", "DecreaseOffsetVariation"); }

export const LineToolOptionsComponent = (moduleRegistry: ModuleRegistry) => (Component: any) => {
    return (props: any) => {
        // Component paths.
        const theme = moduleRegistry.registry.get("game-ui/game/components/tool-options/tool-button/tool-button.module.scss")?.classes;
        const toolMouseModule = moduleRegistry.registry.get("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx");
        const mouseToolTheme = moduleRegistry.registry.get("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.module.scss")?.classes;
        const toolButtonModule = moduleRegistry.registry.get("game-ui/game/components/tool-options/tool-button/tool-button.tsx")!!;
        const focusKey = moduleRegistry.registry.get("game-ui/common/focus/focus-key.ts");

        const Section: any = toolMouseModule?.Section;
        const ToolButton: any = toolButtonModule?.ToolButton;
        const { translate } = useLocalization();
        const { children, ...otherProps } = props || {};
        const FocusDisabled: any = focusKey?.FOCUS_DISABLED;

        // Boolean update bindings.
        const showModeRow: boolean = useValue(showModeRow$);
        const pointModeEnabled: boolean = useValue(pointModeEnabled$);
        const straightLineModeEnabled: boolean = useValue(straightLineModeEnabled$);
        const simpleCurveModeEnabled: boolean = useValue(simpleCurveModeEnabled$);
        const circleModeEnabled: boolean = useValue(circleModeEnabled$);
        const fenceModeEnabled: boolean = useValue(fenceModeEnabled$);
        const fullLengthEnabled: boolean = useValue(fullLengthEnabled$);
        const randomRotationEnabled: boolean = useValue(randomRotationEnabled$);

        // Number update bindings.
        const Spacing: Number = useValue(Spacing$);
        const Rotation: Number = useValue(Rotation$);
        const SpacingVariation: Number = useValue(SpacingVariation$);
        const OffsetVariation: Number = useValue(OffsetVariation$);

        // Number display strings.
        const renderedSpacing: string = Spacing.toFixed(1).toString() + " m";
        const renderedRotation: string = Rotation.toFixed(0).toString() + "°";
        const renderedSpacingVariation: string = SpacingVariation.toFixed(0).toString() + " m";
        const renderedOffsetVariation: string = OffsetVariation.toFixed(0).toString() + " m";

        // Show mode row if set.
        let result: JSX.Element = Component();
        if (showModeRow) {
            result.props.children?.push(
                <Section title={translate("LINETOOL.LineMode")}>
                    <ToolButton
                        className={theme.button}
                        src={"Media/Tools/Net Tool/Point.svg"}
                        tooltip={translate("LINETOOL.PointMode") + ": " + "\r\n" + translate("LINETOOL_DESCRIPTION.PointMode")}
                        onSelect={pointModeClick}
                        selected={pointModeEnabled}
                        multiSelect={false}
                        disabled={false}
                        focusKey={FocusDisabled}
                    />
                    <ToolButton
                        className={theme.button}
                        src={"Media/Tools/Net Tool/Straight.svg"}
                        tooltip={translate("LINETOOL.StraightLine") + ": " + translate("LINETOOL_DESCRIPTION.StraightLine")}
                        onSelect={straightLineModeClick}
                        selected={straightLineModeEnabled}
                        multiSelect={false}
                        disabled={false}
                        focusKey={FocusDisabled}
                    />
                    <ToolButton
                        className={theme.button}
                        src={"Media/Tools/Net Tool/SimpleCurve.svg"}
                        tooltip={translate("LINETOOL.SimpleCurve") + ": " + translate("LINETOOL_DESCRIPTION.SimpleCurve")}
                        onSelect={simpleCurveModeClick}
                        selected={simpleCurveModeEnabled}
                        multiSelect={false}
                        disabled={false}
                        focusKey={FocusDisabled}
                    />
                    <ToolButton
                        className={theme.button}
                        src={"coui://uil/Standard/Circle.svg"}
                        tooltip={translate("LINETOOL.Circle") + ": " + translate("LINETOOL_DESCRIPTION.Circle")}
                        onSelect={circleModeClick}
                        selected={circleModeEnabled}
                        multiSelect={false}
                        disabled={false}
                        focusKey={FocusDisabled}
                    />
                </Section>
            );

            // Show options row if anything other than single point mode is selected.
            if (!pointModeEnabled) {
                result.props.children?.push(
                    <>
                        <Section title={translate("LINETOOL.Options")}>
                            <ToolButton
                                className={theme.button}
                                src={"coui://uil/Standard/Fence.svg"}
                                tooltip={translate("LINETOOL.FenceMode") + ": " + translate("LINETOOL_DESCRIPTION.FenceMode")}
                                onSelect={fenceModeClick}
                                selected={fenceModeEnabled}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                        </Section>
                        <Section title={translate("LINETOOL.Spacing")} tooltip={translate("LINETOOL_DESCRIPTION.Spacing")}>
                            <ToolButton
                                className={theme.button}
                                src={"coui://uil/Standard/MeasureEven.svg"}
                                tooltip={translate("LINETOOL.FixedLength") + ": " + translate("LINETOOL_DESCRIPTION.FixedLength")}
                                onSelect={fullLengthClick}
                                selected={fullLengthEnabled}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                            <ToolButton
                                className={mouseToolTheme.startButton}
                                src="coui://uil/Standard/ArrowDownThickStroke.svg"
                                tooltip={translate("LINETOOL.SpacingDown") + ": " + translate("LINETOOL_DESCRIPTION.SpacingDown")}
                                onSelect={spacingDownClick}
                                selected={false}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                            <div className={mouseToolTheme.numberField}>{renderedSpacing}</div>
                            <ToolButton
                                className={mouseToolTheme.endButton}
                                src="coui://uil/Standard/ArrowUpThickStroke.svg"
                                tooltip={translate("LINETOOL.SpacingUp") + ": " + translate("LINETOOL_DESCRIPTION.SpacingUp") + translate("LINETOOL_DESCRIPTION.Spacing")}
                                onSelect={spacingUpClick}
                                selected={false}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                        </Section>
                        <Section title={translate("LINETOOL.Rotation")} tooltip={translate("LINETOOL_DESCRIPTION.Rotation")}>
                            <ToolButton
                                className={theme.button}
                                src={"coui://uil/Standard/Dice.svg"}
                                tooltip={translate("LINETOOL.RandomRotation") + ": " + translate("LINETOOL_DESCRIPTION.RandomRotation")}
                                onSelect={randomRotationClick}
                                selected={randomRotationEnabled}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                            <ToolButton
                                className={mouseToolTheme.startButton}
                                src="coui://uil/Standard/ArrowDownThickStroke.svg"
                                tooltip={translate("LINETOOL.AntiClockwise") + ": " + translate("LINETOOL_DESCRIPTION.AntiClockwise") + translate("LINETOOL_DESCRIPTION.Rotation")}
                                onSelect={rotationDownClick}
                                selected={false}
                                multiSelect={false}
                                disabled={randomRotationEnabled}
                                focusKey={FocusDisabled}
                            />
                            <div className={mouseToolTheme.numberField}>{renderedRotation}</div>
                            <ToolButton
                                className={mouseToolTheme.endButton}
                                src="coui://uil/Standard/ArrowUpThickStroke.svg"
                                tooltip={translate("LINETOOL.Clockwise") + ": " + translate("LINETOOL_DESCRIPTION.Clockwise") + translate("LINETOOL_DESCRIPTION.Rotation")}
                                onSelect={rotationUpClick}
                                selected={false}
                                multiSelect={false}
                                disabled={randomRotationEnabled}
                                focusKey={FocusDisabled}
                            />
                        </Section>
                        <Section title={translate("LINETOOL.SpacingVariation")}>
                            <ToolButton
                                className={mouseToolTheme.startButton}
                                src="coui://uil/Standard/ArrowDownThickStroke.svg"
                                tooltip={translate("LINETOOL.RandomSpacingDown") + ": " + translate("LINETOOL_DESCRIPTION.RandomSpacingDown")}
                                onSelect={spacingVariationDownClick}
                                selected={false}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                            <div className={mouseToolTheme.numberField}>{renderedSpacingVariation}</div>
                            <ToolButton
                                className={mouseToolTheme.endButton}
                                src="coui://uil/Standard/ArrowUpThickStroke.svg"
                                tooltip={translate("LINETOOL.RandomSpacingUp") + ": " + translate("LINETOOL_DESCRIPTION.RandomSpacingUp")}
                                onSelect={spacingVariationUpClick}
                                selected={false}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                        </Section>
                        <Section title={translate("LINETOOL.OffsetVariation")}>
                            <ToolButton
                                className={mouseToolTheme.startButton}
                                src="coui://uil/Standard/ArrowDownThickStroke.svg"
                                tooltip={translate("LINETOOL.RandomOffsetUp") + ": " + translate("LINETOOL_DESCRIPTION.RandomOffsetUp")}
                                onSelect={offsetVariationDownClick}
                                selected={false}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                            <div className={mouseToolTheme.numberField}>{renderedOffsetVariation}</div>
                            <ToolButton
                                className={mouseToolTheme.endButton}
                                src="coui://uil/Standard/ArrowUpThickStroke.svg"
                                tooltip={translate("LINETOOL.RandomOffsetDown") + ": " + translate("LINETOOL_DESCRIPTION.RandomOffsetDown") + translate("LINETOOL_DESCRIPTION.OffsetVariation")}
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