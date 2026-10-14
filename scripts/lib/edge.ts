// Checks what Nginx adds in front of the service (ADR 0008): kept tiles, 304, gzip, and tiles
// served while the service is down. scripts/e2e.ts runs these after the browser tests.
import assert from 'node:assert/strict';
import { request } from 'node:http';
import { gunzipSync } from 'node:zlib';

type Answer = { readonly status: number; readonly headers: Record<string, string | string[] | undefined>; readonly body: Buffer };

// node:http, not fetch, because fetch unpacks gzip by itself and hides the bytes on the wire.
function ask(url: string, headers: Record<string, string> = {}, method = 'GET'): Promise<Answer> {
  return new Promise((done, fail) => {
    const sent = request(url, { method, headers }, answer => {
      const parts: Buffer[] = [];
      answer.on('data', (part: Buffer) => parts.push(part));
      answer.on('end', () => done({ status: answer.statusCode ?? 0, headers: answer.headers, body: Buffer.concat(parts) }));
    });
    sent.on('error', fail);
    sent.end();
  });
}

/** Runs every check against Nginx at `base`; `stopService` stops the service for the last two. */
export async function checkEdge(base: string, stopService: () => Promise<void>): Promise<void> {
  const tile = `${base}/food.cafes/0/0/0.pbf`;

  // N-19: the first request builds the tile; the second comes from Nginx's store.
  const first = await ask(tile, { 'Accept-Encoding': 'identity' });
  assert.equal(first.status, 200);
  assert.equal(first.headers['content-type'], 'application/vnd.mapbox-vector-tile');
  assert.equal(first.headers['cache-control'], 'public, max-age=60');
  const second = await ask(tile, { 'Accept-Encoding': 'identity' });
  assert.equal(second.headers['x-cache-status'], 'HIT');
  assert.deepEqual(second.body, first.body);

  // N-19 c2: a browser that sends the tag back gets 304 and no body.
  const tag = String(first.headers['etag']);
  assert.match(tag, /^W\//);
  const unchanged = await ask(tile, { 'If-None-Match': tag });
  assert.equal(unchanged.status, 304);
  assert.equal(unchanged.body.length, 0);

  // N-20: packed when the browser accepts gzip, and the same bytes once unpacked.
  const packed = await ask(tile, { 'Accept-Encoding': 'gzip' });
  assert.equal(packed.headers['content-encoding'], 'gzip');
  assert.match(String(packed.headers['vary']), /Accept-Encoding/);
  assert.deepEqual(gunzipSync(packed.body), first.body);

  // ADR 0006 rule 7: HEAD gets the headers alone.
  const head = await ask(tile, {}, 'HEAD');
  assert.equal(head.status, 200);
  assert.equal(head.body.length, 0);

  // N-20 test 2: the map's main script travels at under half its plain size.
  const page = (await ask(`${base}/`)).body.toString();
  const script = /src="(\/assets\/[^"]+\.js)"/.exec(page)?.[1] ?? '';
  const plainScript = await ask(`${base}${script}`, { 'Accept-Encoding': 'identity' });
  const packedScript = await ask(`${base}${script}`, { 'Accept-Encoding': 'gzip' });
  assert.ok(packedScript.body.length < plainScript.body.length / 2, `${packedScript.body.length} packed against ${plainScript.body.length} plain`);
  assert.deepEqual(gunzipSync(packedScript.body), plainScript.body);

  // With the service down, a kept tile still arrives, and a tile never kept asks the browser to
  // try again shortly, as a problem document.
  await stopService();
  const kept = await ask(tile, { 'Accept-Encoding': 'identity' });
  assert.equal(kept.status, 200);
  assert.deepEqual(kept.body, first.body);
  const never = await ask(`${base}/food.cafes/22/0/0.pbf`);
  assert.equal(never.status, 503);
  assert.equal(never.headers['content-type'], 'application/problem+json');
  assert.equal(never.headers['retry-after'], '5');
  console.log('Nginx checks passed: kept tiles, 304, gzip, HEAD, and tiles served with the service down.');
}
