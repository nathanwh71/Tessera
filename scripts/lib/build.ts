// The one step of the build that changes files itself: clearing out the map an older build left.
// It touches the disk, so it is not pure, but it reads only the folder it is given, and so a test
// can run it on a folder of its own (ADR 0002).
import { rm } from 'node:fs/promises';
import { join } from 'node:path';

/**
 * The folder the release serves the map from: the project copies web/dist there as wwwroot/
 * (US-23 c4). `root` is the repository's top folder.
 */
export const releaseMap = (root: string): string =>
  join(root, 'src', 'Tessera', 'bin', 'Release', 'net10.0', 'wwwroot');

/**
 * Deletes the release's map folder before dotnet build copies web/dist into it again. The copy
 * adds and replaces files but never deletes one, and Vite names each file after its contents, so
 * every build of the map leaves new names behind; without this step the old files would pile up
 * and ship with the release. The folder may not exist yet, on a first build, and that is no error.
 */
export async function emptyReleaseMap(root: string): Promise<void> {
  await rm(releaseMap(root), { recursive: true, force: true });
}
