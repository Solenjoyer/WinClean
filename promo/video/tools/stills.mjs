// Renders a handful of frames to PNG for a quick visual check without a full render.
// Usage: node tools/stills.mjs 70 200 400
import {bundle} from '@remotion/bundler';
import {renderStill, selectComposition} from '@remotion/renderer';
import {mkdirSync} from 'node:fs';
import path from 'node:path';

const frames = process.argv.slice(2).map(Number);
const browserExecutable = process.env.REMOTION_BROWSER_EXECUTABLE;
const root = path.resolve(import.meta.dirname, '..');

mkdirSync(path.join(root, 'out', 'stills'), {recursive: true});
const serveUrl = await bundle({entryPoint: path.join(root, 'src', 'index.ts')});
const composition = await selectComposition({serveUrl, id: 'WinCleanLaunch', browserExecutable});

for (const frame of frames) {
  const output = path.join(root, 'out', 'stills', `f${String(frame).padStart(4, '0')}.png`);
  await renderStill({composition, serveUrl, output, frame, browserExecutable, imageFormat: 'png'});
  console.log(output);
}
