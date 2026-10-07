import React from 'react';
import {Audio, Sequence, interpolate, staticFile} from 'remotion';
import {SCENES, TOTAL_FRAMES, at} from './timeline';

type Cue = {sound: string; frame: number; volume: number};

const cue = (sound: string, frame: number, volume = 0.6): Cue => ({sound, frame, volume});

const stagger = (sound: string, first: number, count: number, every: number, volume: number) =>
  Array.from({length: count}, (_, i) => cue(sound, first + i * every, volume));

// Every effect is attached to the moment it underlines; the frames mirror the scene files.
export const cues: Cue[] = [
  cue('impact', at('intro', 10), 0.9),
  cue('shimmer', at('intro', 10), 0.6),
  cue('whoosh', at('intro', 42), 0.7),
  ...stagger('tick', at('intro', 62), 7, 3, 0.3),

  cue('whoosh-soft', at('monitor', 8), 0.5),
  ...stagger('whoosh-soft', at('monitor', 30), 4, 6, 0.45),
  cue('pop', at('monitor', 66), 0.4),
  cue('pop', at('monitor', 72), 0.4),

  cue('whoosh-soft', at('processes', 6), 0.5),
  ...stagger('pop', at('processes', 30), 7, 5, 0.45),
  cue('click', at('processes', 80), 0.7),
  ...stagger('tick', at('processes', 84), 4, 3, 0.35),
  ...stagger('tick', at('processes', 128), 11, 4, 0.3),

  cue('whoosh-soft', at('storage', 6), 0.5),
  cue('riser', at('storage', 18), 0.55),
  cue('impact', at('storage', 58), 0.7),
  ...stagger('pop', at('storage', 58), 5, 6, 0.4),
  ...stagger('tick', at('storage', 70), 6, 6, 0.3),

  cue('whoosh-soft', at('cleanup', 6), 0.5),
  ...stagger('check', at('cleanup', 22), 6, 9, 0.5),
  cue('click', at('cleanup', 90), 0.7),
  cue('whoosh-soft', at('cleanup', 100), 0.5),
  cue('riser', at('cleanup', 111), 0.6),
  cue('click', at('cleanup', 135), 0.7),
  cue('impact', at('cleanup', 150), 0.95),
  cue('chime', at('cleanup', 152), 0.5),

  cue('whoosh-soft', at('health', 6), 0.5),
  cue('pop', at('health', 16), 0.4),
  cue('pop', at('health', 22), 0.4),
  ...stagger('check', at('health', 68), 4, 9, 0.55),

  cue('impact', at('outro', 4), 0.6),
  cue('shimmer', at('outro', 4), 0.6),
  cue('whoosh', at('outro', 20), 0.6),
  cue('chime', at('outro', 40), 0.6),
  cue('tick', at('outro', 50), 0.3),
];

const ducks = [at('intro', 10), at('storage', 58), at('cleanup', 150), at('outro', 4)];

const bedVolume = (frame: number) => {
  let volume = interpolate(frame, [0, 20], [0.1, 0.5], {extrapolateRight: 'clamp'});
  for (const duck of ducks) {
    const env = interpolate(frame, [duck, duck + 3, duck + 30], [0, 1, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
    volume *= 1 - 0.55 * env;
  }
  const ending = SCENES.outro.from + 40;
  volume *= interpolate(frame, [ending, TOTAL_FRAMES - 6], [1, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
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
