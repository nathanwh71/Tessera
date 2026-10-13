// app/app.ts: the page's one model, one message type and one update (US-23, criterion 3).
//
// The map module owns every message today; this file is the place where rules that cross modules
// would join, so the page always has a single `update` to test.

import * as MapView from "../map";

/** Every message the page handles. */
export type Message = MapView.Message;

/** The constructors, the functions that make each message. */
export const Msg = MapView.Msg;

/** What the page's program receives: its ports, the area, and the layer the address names. */
export type Props = MapView.Props;

/** The one model, the single value that holds everything the page shows. */
export type Model = MapView.Model;

/** The first model and the first command. */
export const init = MapView.init;

/** The update function: from the old model and a message to the new model, with no effects of its own. */
export const update = MapView.update;
