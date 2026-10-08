import React from 'react';
import {interpolate, useCurrentFrame} from 'remotion';
import {CONTENT} from '../layout';
import {colors} from '../theme';

// A page inside the content area: it fades and rises in when the navigation changes, and fades
// out just before the next page takes over.
export const PageFrame: React.FC<{duration: number; children: React.ReactNode}> = ({duration, children}) => {
  const frame = useCurrentFrame();
  const enter = interpolate(frame, [0, 9], [0, 1], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  const exit = interpolate(frame, [duration - 6, duration], [1, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  return (
    <div style={{position: 'absolute', left: 0, top: 0, width: CONTENT.w, height: CONTENT.h, opacity: enter * exit, transform: `translateY(${(1 - enter) * 10}px)`}}>
      {children}
    </div>
  );
};

// Cards inside the application are flatter than the ones in the full-frame scenes.
export const card: React.CSSProperties = {
  padding: 20,
  borderRadius: 12,
  boxShadow: 'none',
  background: 'rgba(255, 255, 255, 0.04)',
  border: `1px solid ${colors.stroke}`,
};

export const cardTitle: React.CSSProperties = {fontSize: 14, fontWeight: 600, color: colors.secondary};

export const liveSlice = (values: number[], frame: number, start: number, window = 60, every = 3) => {
  const shift = Math.floor(Math.max(0, frame - start) / every) % Math.max(1, values.length - window);
  return values.slice(shift, shift + window);
};
