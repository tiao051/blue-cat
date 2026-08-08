/**
 * Hand-drawn 8x8 pixel item icons for habit slots — original art, Minecraft-flavored.
 * Keyed by the `icon` field on habits (spec §6). Static strings, safe for v-html.
 */

const wrap = (rects: string) =>
  `<svg viewBox="0 0 8 8" shape-rendering="crispEdges" xmlns="http://www.w3.org/2000/svg">${rects}</svg>`

const ICONS: Record<string, string> = {
  // a red book, white page edge, clasp
  book: wrap(
    `<rect x="1" y="1" width="5" height="6" fill="#a83a2a"/>` +
      `<rect x="6" y="1" width="1" height="6" fill="#e8e0d0"/>` +
      `<rect x="1" y="1" width="1" height="6" fill="#7c2a1e"/>` +
      `<rect x="3" y="3" width="2" height="1" fill="#d8b13c"/>`,
  ),
  // an iron barbell (dark enough to read on the gray panel)
  barbell: wrap(
    `<rect x="0" y="3" width="8" height="1" fill="#565e68"/>` +
      `<rect x="1" y="1" width="1" height="5" fill="#31383f"/>` +
      `<rect x="2" y="2" width="1" height="3" fill="#454d56"/>` +
      `<rect x="6" y="1" width="1" height="5" fill="#31383f"/>` +
      `<rect x="5" y="2" width="1" height="3" fill="#454d56"/>`,
  ),
  // a little terminal block with a green prompt
  code: wrap(
    `<rect x="0" y="1" width="8" height="6" fill="#22282e"/>` +
      `<rect x="0" y="1" width="8" height="1" fill="#39424c"/>` +
      `<rect x="1" y="3" width="1" height="1" fill="#6cc627"/>` +
      `<rect x="2" y="4" width="1" height="1" fill="#6cc627"/>` +
      `<rect x="1" y="5" width="1" height="1" fill="#6cc627"/>` +
      `<rect x="4" y="5" width="3" height="1" fill="#4e7e2e"/>`,
  ),
  // a microphone on a stand (dark chrome)
  microphone: wrap(
    `<rect x="3" y="1" width="2" height="2" fill="#7c8894"/>` +
      `<rect x="3" y="0" width="2" height="1" fill="#4a545e"/>` +
      `<rect x="2" y="1" width="1" height="2" fill="#4a545e"/>` +
      `<rect x="5" y="1" width="1" height="2" fill="#4a545e"/>` +
      `<rect x="3" y="3" width="2" height="1" fill="#31383f"/>` +
      `<rect x="3" y="4" width="1" height="3" fill="#4a545e"/>` +
      `<rect x="2" y="7" width="4" height="1" fill="#31383f"/>`,
  ),
  // an oak door, ajar with a golden knob
  'door-exit': wrap(
    `<rect x="2" y="0" width="4" height="8" fill="#8a5a2b"/>` +
      `<rect x="2" y="0" width="1" height="8" fill="#6e451f"/>` +
      `<rect x="3" y="1" width="2" height="2" fill="#9c6a38"/>` +
      `<rect x="3" y="4" width="2" height="3" fill="#9c6a38"/>` +
      `<rect x="5" y="4" width="1" height="1" fill="#d8b13c"/>` +
      `<rect x="6" y="1" width="1" height="1" fill="#f0e6a0" opacity="0.7"/>` +
      `<rect x="7" y="3" width="1" height="1" fill="#f0e6a0" opacity="0.5"/>`,
  ),
}

/** Fallback: a plain crafting-table-ish block */
const FALLBACK = wrap(
  `<rect x="1" y="1" width="6" height="6" fill="#8a6a3a"/>` +
    `<rect x="1" y="1" width="6" height="1" fill="#a8854c"/>` +
    `<rect x="3" y="3" width="2" height="2" fill="#6e5124"/>`,
)

export function pixelIcon(name: string): string {
  return ICONS[name] ?? FALLBACK
}
