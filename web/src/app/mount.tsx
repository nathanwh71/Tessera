// app/mount.tsx: puts the page into the HTML element that holds it, and keeps a page that fails from
// leaving a blank window (US-23, criterion 9). React, the library that draws the page, removes the
// whole page when an error escapes every part of it; this file then draws a short message in its
// place, so the visitor learns what happened and what to do.

import type { ReactNode } from "react";
import { type Root, createRoot } from "react-dom/client";

/** What the page says when an error nothing else caught has stopped it. */
export const PAGE_FAILED = "The map stopped working. Reload the page to try again.";

/** The message, drawn where the page was, in the page's own card. */
function PageFailed(): ReactNode {
	return (
		<main className="relative h-full w-full bg-surface font-sans text-on-surface">
			<p role="alert" className="card message">
				{PAGE_FAILED}
			</p>
		</main>
	);
}

/**
 * Draws the page into the element. React calls `onUncaughtError` once it has removed a page that
 * threw, and the root then draws the message instead; the error still reaches the console, for
 * whoever looks into it.
 */
export function mount(element: HTMLElement, page: ReactNode): Root {
	const root = createRoot(element, {
		onUncaughtError: (error) => {
			console.error(error);
			root.render(<PageFailed />);
		},
	});
	root.render(page);
	return root;
}
