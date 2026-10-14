// app/index.ts: the app module's public face, the list of what other files may import. main.tsx
// starts the page from it. The map module owns every message, so the page's one model, message type
// and update are the map's, passed on here unchanged (US-23); the tests reach them through this file.
export type { AppProps } from "./App";
export { App } from "./App";
export { PAGE_FAILED, mount } from "./mount";
export type { Message, Model, Props } from "../map";
export { Msg, init, update } from "../map";
