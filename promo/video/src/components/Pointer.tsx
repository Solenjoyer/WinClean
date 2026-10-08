import React from 'react';
import {Easing, interpolate} from 'remotion';

export type PointerKey = {frame: number; x: number; y: number};

export type Click = {frame: number};

type Props = {frame: number; keys: PointerKey[]; clicks: Click[]; pressedFrom?: number; pressedTo?: number};

export const pointerAt = (frame: number, keys: PointerKey[]) => {
  if (frame <= keys[0].frame) {
    return {x: keys[0].x, y: keys[0].y};
  }
  for (let i = 0; i < keys.length - 1; i += 1) {
    const from = keys[i];
    const to = keys[i + 1];
    if (frame >= from.frame && frame <= to.frame) {
      const t = interpolate(frame, [from.frame, to.frame], [0, 1], {easing: Easing.inOut(Easing.cubic)});
      // A slight arc so the pointer does not travel in a dead straight line.
      const arc = Math.sin(t * Math.PI) * Math.min(40, Math.hypot(to.x - from.x, to.y - from.y) * 0.08);
      return {x: from.x + (to.x - from.x) * t, y: from.y + (to.y - from.y) * t - arc};
    }
  }
  const last = keys[keys.length - 1];
  return {x: last.x, y: last.y};
};

// A Windows arrow pointer with a click ripple. Coordinates are the tip of the arrow.
export const Pointer: React.FC<Props> = ({frame, keys, clicks, pressedFrom, pressedTo}) => {
  const {x, y} = pointerAt(frame, keys);
  const click = clicks.find((c) => frame >= c.frame && frame < c.frame + 14);
  const pressed = click ? frame < click.frame + 4 : pressedFrom !== undefined && pressedTo !== undefined && frame >= pressedFrom && frame < pressedTo;
  const ripple = click ? interpolate(frame, [click.frame, click.frame + 14], [0, 1]) : 0;
  const visible = interpolate(frame, [keys[0].frame, keys[0].frame + 8], [0, 1], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});

  return (
    <div style={{position: 'absolute', left: x, top: y, opacity: visible, pointerEvents: 'none'}}>
      {click ? (
        <div
          style={{
            position: 'absolute',
            left: -24 * ripple,
            top: -24 * ripple,
            width: 48 * ripple,
            height: 48 * ripple,
            borderRadius: '50%',
            border: '2px solid rgba(255, 255, 255, 0.8)',
            opacity: 1 - ripple,
          }}
        />
      ) : null}
      <svg width="30" height="36" viewBox="0 0 24 30" style={{display: 'block', transform: `scale(${pressed ? 0.9 : 1})`, transformOrigin: '2px 2px', filter: 'drop-shadow(0 2px 3px rgba(0, 0, 0, 0.5))'}}>
        <path d="M2 2 L2 23 L7.5 18 L11.5 27 L15.5 25.5 L11.5 16.5 L19 16.5 Z" fill="#FFFFFF" stroke="#111318" strokeWidth="1.6" strokeLinejoin="round" />
      </svg>
    </div>
  );
};
