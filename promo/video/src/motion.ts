import {Easing, interpolate, spring} from 'remotion';

// Springs without overshoot for layout, with a little for pops.
export const smooth = {damping: 200, stiffness: 120, mass: 1};
export const lively = {damping: 14, stiffness: 150, mass: 0.8};
export const snappy = {damping: 18, stiffness: 260, mass: 0.6};

export const rise = (frame: number, fps: number, delay: number, config = smooth) =>
  spring({frame: Math.max(0, frame - delay), fps, config});

export const fadeIn = (frame: number, delay: number, length = 8) =>
  interpolate(frame, [delay, delay + length], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

export const countUp = (frame: number, start: number, length: number, to: number, from = 0) =>
  interpolate(frame, [start, start + length], [from, to], {
    easing: Easing.out(Easing.cubic),
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

// Deterministic noise so every render is identical.
export const seeded = (seed: number) => {
  let state = seed >>> 0;
  return () => {
    state = (state + 0x6d2b79f5) >>> 0;
    let t = state;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
};

export const series = (seed: number, count: number, base: number, swing: number, spikes = 0.08) => {
  const random = seeded(seed);
  const values: number[] = [];
  let level = base;
  for (let i = 0; i < count; i += 1) {
    level += (random() - 0.5) * swing;
    level += (base - level) * 0.15;
    const spike = random() < spikes ? random() * swing * 3 : 0;
    values.push(Math.min(1, Math.max(0.02, level + spike)));
  }
  return values;
};
