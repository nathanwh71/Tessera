// Checks what Nginx adds in front of the service (ADR 0008): the office-only rule and the limits
// for each visitor, kept tiles, 304, gzip, a second copy that takes over when the first stops, and
// tiles served while both copies are down.
// scripts/e2e.ts runs these after the browser tests, with the office set to 127.0.0.4/30.
import assert from 'node:assert/strict';
import { Agent, request } from 'node:http';
import { connect } from 'node:net';
import { gunzipSync } from 'node:zlib';

/** One answer, read whole, so a check can compare its status, headers and exact bytes. */
type Answer = { readonly status: number; readonly headers: Record<string, string | string[] | undefined>; readonly body: Buffer };

// node:http, not fetch, because fetch unpacks gzip by itself and hides the bytes on the wire.
// `from` is the address the request leaves from, so a check can play an office machine or a stranger.
function ask(url: string, headers: Record<string, string> = {}, method = 'GET', from?: string, agent?: Agent): Promise<Answer> {
  return new Promise((done, fail) => {
    const sent = request(url, { method, headers, ...(from === undefined ? {} : { localAddress: from }), ...(agent === undefined ? {} : { agent }) }, answer => {
      const parts: Buffer[] = [];
      answer.on('data', (part: Buffer) => parts.push(part));
      answer.on('end', () => done({ status: answer.statusCode ?? 0, headers: answer.headers, body: Buffer.concat(parts) }));
    });
    sent.on('error', fail);
    sent.end();
  });
}

// A request written byte for byte, so nothing tidies the address on the way, as fetch would.
function raw(base: string, target: string): Promise<string> {
  const { hostname, port } = new URL(base);
  return new Promise((done, fail) => {
    const socket = connect(Number(port), hostname, () => socket.end(`GET ${target} HTTP/1.1\r\nHost: ${hostname}\r\nConnection: close\r\n\r\n`));
    const parts: Buffer[] = [];
    socket.on('data', (part: Buffer) => parts.push(part));
    socket.on('end', () => done(Buffer.concat(parts).toString('latin1')));
    socket.on('error', fail);
  });
}

/** Runs every check against Nginx at `base`; `stopFirst` and `stopSecond` stop each copy of the service. */
export async function checkEdge(base: string, stopFirst: () => Promise<void>, stopSecond: () => Promise<void>): Promise<void> {
  const tile = `${base}/food.cafes/0/0/0.pbf`;

  // N-19: the first request builds the tile; the second comes from Nginx's store.
  const first = await ask(tile, { 'Accept-Encoding': 'identity' });
  assert.equal(first.status, 200);
  assert.equal(first.headers['content-type'], 'application/vnd.mapbox-vector-tile');
  // 60 seconds is Tiles.CacheSeconds' default, and Nginx keeps the tile for as long as it allows.
  assert.equal(first.headers['cache-control'], 'public, max-age=60');
  const second = await ask(tile, { 'Accept-Encoding': 'identity' });
  assert.equal(second.headers['x-cache-status'], 'HIT');
  assert.deepEqual(second.body, first.body);

  // N-19 c2: a browser that sends the tag back gets 304 and no body. The tag is weak, 'W/',
  // because the packed and the plain bytes are the same tile (N-19 c1), and gzip keeps a weak tag.
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

  // HEAD gets the headers alone.
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

  // S-06: a malformed address reaches no route. Nginx refuses one it cannot read, and the service
  // finds no route for the rest; either way the answer is a problem document that hides the address.
  for (const target of ['/food.cafes/%2e%2e/1/2/3.pbf', '/food.cafes/../../1/2/3.pbf', '/food.cafes%2F15/1/2.pbf', '/food.cafes/15/1/2.pbf%zz', '/food.cafes/15/1/2.pbf%00']) {
    const answer = await raw(base, target);
    assert.match(answer, /^HTTP\/1\.1 40[04] /, target);
    assert.match(answer, /application\/problem\+json/, target);
    assert.doesNotMatch(answer.split('\r\n\r\n')[1] ?? '', /food\.cafes/, target);
  }

  // S-07: a machine outside the office gets 403 as a problem document; one inside gets the tile.
  const stranger = await ask(tile, {}, 'GET', '127.0.0.2');
  assert.equal(stranger.status, 403);
  assert.equal(stranger.headers['content-type'], 'application/problem+json');
  assert.equal(stranger.headers['cache-control'], 'no-store');
  assert.equal((await ask(tile, {}, 'GET', '127.0.0.5')).status, 200);

  // S-30: one visitor asking for 600 tiles as fast as eight connections allow passes the burst of
  // 300 and meets 429, which asks it to wait a second. The flood comes from an address of its own,
  // so the limit it meets touches no other check. Eight connections stay well under the cap of 32
  // at once, so only the rate limit can answer 429 here.
  const flood = new Agent({ keepAlive: true, maxSockets: 8 });
  const answers = await Promise.all(Array.from({ length: 600 }, () => ask(tile, {}, 'GET', '127.0.0.6', flood)));
  flood.destroy();
  const busy = answers.filter(answer => answer.status === 429);
  assert.ok(busy.length > 0, 'no request met the limit');
  assert.ok(answers.filter(answer => answer.status === 200).length >= 300, 'the burst of 300 did not pass');
  assert.equal(busy[0]?.headers['content-type'], 'application/problem+json');
  assert.equal(busy[0]?.headers['retry-after'], '1');
  // Another office machine is not slowed by the first one's limit.
  assert.equal((await ask(tile, {}, 'GET', '127.0.0.5')).status, 200);

  // N-24: twenty requests at once for one tile that takes two seconds to build all get it in about
  // two. Without Nginx's lock, twenty queries would share the two copies' eight connections and
  // take three rounds, six seconds or more.
  const crowdTile = `${base}/food.slow_cafes/0/0/0.pbf`;
  const began = Date.now();
  const crowd = await Promise.all(Array.from({ length: 20 }, () => ask(crowdTile, { 'Accept-Encoding': 'identity' })));
  const took = Date.now() - began;
  assert.ok(crowd.every(answer => answer.status === 200), `statuses ${crowd.map(answer => answer.status).join(',')}`);
  assert.ok(took >= 1800 && took < 4000, `twenty requests took ${took} ms`);

  // S-30: one machine may have 32 requests in hand at once. Forty at once for a slow tile leave
  // eight or more refused with 429, and the rest receive the tile. Without keep-alive, and with room
  // for 64 sockets, each request holds a connection of its own, so Nginx counts all forty at once.
  // The tile, 1/1/0, is one no check has asked for, so every request waits on the slow query
  // rather than meeting a kept copy; the address, 127.0.0.7, has met no limit yet.
  const many = new Agent({ keepAlive: false, maxSockets: 64 });
  const forty = await Promise.all(Array.from({ length: 40 }, () => ask(`${base}/food.slow_cafes/1/1/0.pbf`, {}, 'GET', '127.0.0.7', many)));
  many.destroy();
  const refused = forty.filter(answer => answer.status === 429).length;
  assert.ok(refused >= 8, `${refused} of 40 refused`);
  assert.equal(forty.filter(answer => answer.status === 200).length, 40 - refused);

  // N-29: with the first copy stopped, the second serves a tile Nginx never held, and /health
  // still says UP. Nginx tries the stopped copy, meets a refused connection, and moves on. The copies
  // take turns (round robin), so six fresh tiles send at least three to the stopped copy: enough to
  // meet it, and to reach max_fails=3, after which Nginx rests it.
  await stopFirst();
  for (let attempt = 0; attempt < 6; attempt++) {
    const fresh = await ask(`${base}/food.cafes/22/1/${attempt}.pbf`, { 'Accept-Encoding': 'identity' });
    assert.equal(fresh.status, 200, `tile ${attempt} with one copy stopped`);
    assert.equal(fresh.headers['x-cache-status'], 'MISS');
  }
  const health = await ask(`${base}/health`);
  assert.equal(health.status, 200);
  assert.match(health.body.toString(), /"UP"/);

  // With both copies down, a kept tile still arrives, and a tile never kept asks the browser to
  // try again shortly, as a problem document.
  await stopSecond();
  const kept = await ask(tile, { 'Accept-Encoding': 'identity' });
  assert.equal(kept.status, 200);
  assert.deepEqual(kept.body, first.body);
  const never = await ask(`${base}/food.cafes/22/0/0.pbf`);
  assert.equal(never.status, 503);
  assert.equal(never.headers['content-type'], 'application/problem+json');
  assert.equal(never.headers['retry-after'], '5');
  console.log('Nginx checks passed: the office-only rule, the limit for each visitor, the cap on requests at once, one query for a crowd, kept tiles, 304, gzip, HEAD, malformed addresses, a second copy taking over, and tiles served with both copies down.');
}
