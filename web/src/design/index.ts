// The design module's public face, the list of what other modules may import: Material 3 parts, the group looks and the theme (UI-12, UI-15).
export type { MapImage, StretchMetadata } from "./canvas";
export { IMAGE_MARGIN, IMAGE_PIXEL_RATIO, PIN_SIZE, drawCluster, drawPin, drawTooltip } from "./canvas";
export type { IconProps } from "./components";
export { Icon, PinShape } from "./components";
export { contrastRatio, parseHex } from "./contrast";
export { DROP_SPREAD_MS, DROP_STEP_MS, MOTION, dropDelays, dropMs, emphasised, movementMs, prefersReducedMotion } from "./motion";
export type { Shape } from "./shapes";
export { PIN_PATH, SHAPE_PATHS } from "./shapes";
export type { GroupKey, GroupLook, Theme } from "./theme";
export { GROUP_KEYS, GROUP_LOOKS, readTheme, themeFrom } from "./theme";
