import React from 'react';
import {Audio, Sequence, interpolate, staticFile} from 'remotion';
import {CHECKS_FROM, CHECK_EVERY, DIALOG_AT, RESULT_AT} from './scenes/Cleanup';
import {DRIVERS_FROM, DRIVER_EVERY} from './scenes/Health';
import {EXPAND_AT, ROWS_FROM, ROW_EVERY} from './scenes/Processes';
import {BARS_FROM, BAR_EVERY, DEV_FROM, RISER_AT} from './scenes/Storage';
import {DRAG_RELEASE, TERMINAL_IN, WIDGET_IN, clicks, interstitials} from './Stage';
import {INTRO, OUTRO, PAGES, PageKey, STAGE, TOTAL_FRAMES, stage} from './timeline';

type Cue = {sound: string; frame: number; volume: number};

const cue = (sound: string, frame: number, volume = 0.6): Cue => ({sound, frame, volume});

const stagger = (sound: string, first: number, count: number, every: number, volume: number) =>
  Array.from({length: count}, (_, i) => cue(sound, first + i * every, volume));

const content = (key: PageKey, offset: number) => stage(PAGES[key].content + offset);

// Every effect is attached to the moment it underlines; the frames come from the scene files.
export const cues: Cue[] = [
  cue('impact', INTRO.from + 10, 0.9),
  cue('shimmer', INTRO.from + 10, 0.6),
  cue('whoosh', INTRO.from + 42, 0.7),
  ...stagger('tick', INTRO.from + 62, 8, 3, 0.3),

  ...interstitials.map((item) => cue('whoosh-soft', stage(item.from), 0.5)),
  ...stagger('tick', stage(PAGES.processes.text + 18), 11, 3, 0.22),

  cue('whoosh', stage(PAGES.overview.content - 4), 0.6),
  ...stagger('whoosh-soft', content('overview', 8), 4, 5, 0.4),
  ...stagger('pop', content('overview', 32), 5, 6, 0.3),

  ...clicks.map((click) => cue('click', stage(click.frame), 0.7)),

  ...stagger('pop', content('processes', ROWS_FROM), 10, ROW_EVERY, 0.3),
  ...stagger('tick', content('processes', EXPAND_AT + 4), 5, 3, 0.3),

  cue('riser', content('storage', RISER_AT), 0.5),
  cue('impact', content('storage', BARS_FROM - 1), 0.6),
  ...stagger('pop', content('storage', BARS_FROM), 7, BAR_EVERY, 0.3),
  ...stagger('tick', content('storage', DEV_FROM), 6, 6, 0.25),

  ...stagger('check', content('cleanup', CHECKS_FROM), 6, CHECK_EVERY, 0.5),
  cue('whoosh-soft', content('cleanup', DIALOG_AT), 0.5),
  cue('riser', content('cleanup', DIALOG_AT + 22), 0.55),
  cue('impact', content('cleanup', RESULT_AT), 0.95),
  cue('chime', content('cleanup', RESULT_AT + 2), 0.5),

  ...stagger('pop', content('health', 8), 3, 6, 0.3),
  ...stagger('tick', content('health', DRIVERS_FROM), 6, DRIVER_EVERY, 0.22),

  cue('whoosh-soft', stage(STAGE.restoreClick + 2), 0.45),
  cue('whoosh-down', stage(STAGE.minimizeClick + 2), 0.6),
  cue('pop', stage(WIDGET_IN), 0.5),
  cue('shimmer', stage(WIDGET_IN), 0.35),
  cue('whoosh-soft', stage(TERMINAL_IN), 0.4),
  ...stagger('tick', stage(TERMINAL_IN + 14), 11, 9, 0.18),
  cue('pop', stage(DRAG_RELEASE), 0.35),

  cue('impact', OUTRO.from + 4, 0.6),
  cue('shimmer', OUTRO.from + 4, 0.6),
  cue('whoosh', OUTRO.from + 20, 0.6),
  cue('chime', OUTRO.from + 40, 0.6),
  cue('tick', OUTRO.from + 50, 0.3),
];

const ducks = [INTRO.from + 10, content('storage', BARS_FROM - 1), content('cleanup', RESULT_AT), OUTRO.from + 4];

const bedVolume = (frame: number) => {
  let volume = interpolate(frame, [0, 20], [0.1, 0.5], {extrapolateRight: 'clamp'});
  for (const duck of ducks) {
    const env = interpolate(frame, [duck, duck + 3, duck + 30], [0, 1, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
    volume *= 1 - 0.55 * env;
  }
  volume *= interpolate(frame, [OUTRO.from + 40, TOTAL_FRAMES - 6], [1, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  return Math.max(0, volume);
};

export const Soundtrack: React.FC = () => (
  <>
    <Audio src={staticFile('audio/bed.wav')} volume={bedVolume} />
    {cues.map((item, index) => (
      <Sequence key={`${item.sound}-${index}`} from={item.frame} name={item.sound}>
        <Audio src={staticFile(`audio/${item.sound}.wav`)} volume={item.volume} />
      </Sequence>
    ))}
  </>
);
