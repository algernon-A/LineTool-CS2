import { useLocalization } from "cs2/l10n";
import { ModuleRegistry } from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import {tool} from "cs2/bindings";

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
        const toolButtonModule = moduleRegistry.registry.get("game-ui/game/components/tool-options/tool-button/tool-button.tsx");
        const toolButtonTheme = moduleRegistry.registry.get("game-ui/game/components/tool-options/tool-button/tool-button.module.scss")?.classes;
        const toolMouseModule = moduleRegistry.registry.get("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx");
        const mouseToolTheme = moduleRegistry.registry.get("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.module.scss")?.classes;
        const focusKey = moduleRegistry.registry.get("game-ui/common/focus/focus-key.ts");
        const descriptionToolTip = moduleRegistry.registry.get("game-ui/common/tooltip/description-tooltip/description-tooltip.tsx");
        const descriptionTooltipTheme = moduleRegistry.registry.get("game-ui/common/tooltip/description-tooltip/description-tooltip.module.scss")?.classes;
        
        // Components.
        const Section: any = toolMouseModule?.Section;
        const ToolButton: any = toolButtonModule?.ToolButton;
        const DescriptionTooltip: any = descriptionToolTip?.DescriptionTooltip;
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
        
        // Titled tooltip generator.
        function  TitledTooltip (titleKey: string, contentKey: string): JSX.Element {
            return (<DescriptionTooltip>
            <>
                <div className={descriptionTooltipTheme.title}>{translate(titleKey)}</div>
                <div className={descriptionTooltipTheme.content}>{translate(contentKey)}</div>
            </>
            </DescriptionTooltip>)
        }
        
        // Two-paragraph titled tooltip generator.
        function  TitledParaTooltip (titleKey: string, firstParaKey: string, secondParaKey: string): JSX.Element {
            return (<DescriptionTooltip>
                <>
                    <div className={descriptionTooltipTheme.title}>{translate(titleKey)}</div>
                    <div className={descriptionTooltipTheme.content}>{translate(firstParaKey)}</div>
                    <div className={descriptionTooltipTheme.content}> </div>
                    <div className={descriptionTooltipTheme.content}>{translate(secondParaKey)}</div>
                </>
            </DescriptionTooltip>)
        }

        // Show mode row if set.
        let result: JSX.Element = Component();
        if (showModeRow) {
            result.props.children?.push(
                <Section title={translate("LINETOOL.LineMode")}>
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
                        src={"Media/Tools/Net Tool/Straight.svg"}
                        tooltip={TitledTooltip("LINETOOL.StraightLine","LINETOOL_DESCRIPTION.PointMode")}
                        onSelect={straightLineModeClick}
                        selected={straightLineModeEnabled}
                        multiSelect={false}
                        disabled={false}
                        focusKey={FocusDisabled}
                    />
                    <ToolButton
                        className={toolButtonTheme.button}
                        src={"Media/Tools/Net Tool/SimpleCurve.svg"}
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
                </Section>
            );

            // Show options row if anything other than single point mode is selected.
            if (!pointModeEnabled) {
                result.props.children?.push(
                    <>
                        <Section title={translate("LINETOOL.Options")}>
                            <ToolButton
                                className={toolButtonTheme.button}
                                src={"coui://uil/Standard/Fence.svg"}
                                tooltip={TitledTooltip("LINETOOL.FenceMode", "LINETOOL_DESCRIPTION.FenceMode")}
                                onSelect={fenceModeClick}
                                selected={fenceModeEnabled}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                        </Section>
                        <Section title={translate("LINETOOL.Spacing")} tooltip={translate("LINETOOL_DESCRIPTION.Spacing")}>
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
                                tooltip={TitledParaTooltip("LINETOOL.SpacingDown", "LINETOOL_DESCRIPTION.Spacing", "LINETOOL_DESCRIPTION.SpacingDown")}
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
                                tooltip={TitledParaTooltip("LINETOOL.SpacingUp", "LINETOOL_DESCRIPTION.Spacing", "LINETOOL_DESCRIPTION.SpacingUp")}
                                onSelect={spacingUpClick}
                                selected={false}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                        </Section>
                        <Section title={translate("LINETOOL.Rotation")} tooltip={translate("LINETOOL_DESCRIPTION.Rotation")}>
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
                                tooltip={TitledParaTooltip("LINETOOL.AntiClockwise", "LINETOOL_DESCRIPTION.Rotation", "LINETOOL_DESCRIPTION.AntiClockwise")}
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
                                tooltip={TitledParaTooltip("LINETOOL.Clockwise", "LINETOOL_DESCRIPTION.Rotation", "LINETOOL_DESCRIPTION.Clockwise")}
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
                                tooltip={TitledParaTooltip("LINETOOL.RandomSpacingDown", "LINETOOL_DESCRIPTION.SpacingVariation", "LINETOOL_DESCRIPTION.RandomSpacingDown")}
                                onSelect={spacingVariationDownClick}
                                selected={false}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                            <div className={mouseToolTheme.numberField}>
                                {renderedSpacingVariation}
                            </div>
                            <ToolButton
                                className={mouseToolTheme.endButton}
                                src="coui://uil/Standard/ArrowUpThickStroke.svg"
                                tooltip={TitledParaTooltip("LINETOOL.RandomSpacingUp", "LINETOOL_DESCRIPTION.SpacingVariation", "LINETOOL_DESCRIPTION.RandomSpacingUp")}
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
                                tooltip={TitledParaTooltip("LINETOOL.RandomOffsetUp", "LINETOOL_DESCRIPTION.OffsetVariation", "LINETOOL_DESCRIPTION.RandomOffsetUp")}
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
                                tooltip={TitledParaTooltip("LINETOOL.RandomOffsetDown", "LINETOOL_DESCRIPTION.OffsetVariation", "LINETOOL_DESCRIPTION.RandomOffsetDown")}
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