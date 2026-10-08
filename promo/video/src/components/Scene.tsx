import React from 'react';
import {AbsoluteFill, Sequence, interpolate, useCurrentFrame} from 'remotion';

type Props = {from: number; duration: number; name: string; children: React.ReactNode};

// Full-frame scenes (intro, outro) fade in over a few frames and leave by scaling up and fading out.
export const Scene: React.FC<Props> = ({from, duration, name, children}) => (
  <Sequence from={from} durationInFrames={duration} name={name}>
    <Frame duration={duration}>{children}</Frame>
  </Sequence>
);

const Frame: React.FC<{duration: number; children: React.ReactNode}> = ({duration, children}) => {
  const frame = useCurrentFrame();
  const enter = interpolate(frame, [0, 6], [0, 1], {extrapolateRight: 'clamp'});
  const exit = interpolate(frame, [duration - 10, duration], [1, 0], {extrapolateLeft: 'clamp'});
  const drift = interpolate(frame, [0, duration], [1, 1.015]);
  const leave = interpolate(frame, [duration - 10, duration], [1, 1.04], {extrapolateLeft: 'clamp'});

  return (
    <AbsoluteFill style={{opacity: enter * exit, transform: `scale(${drift * leave})`}}>
      {children}
    </AbsoluteFill>
  );
};
