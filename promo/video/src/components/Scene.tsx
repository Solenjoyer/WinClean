import React from 'react';
import {AbsoluteFill, Sequence, interpolate, useCurrentFrame} from 'remotion';
import {SCENES, SceneName} from '../timeline';

type Props = {name: SceneName; children: React.ReactNode};

// Each scene fades in over a few frames and leaves by scaling up and fading out,
// so hard cuts between sequences are never visible.
export const Scene: React.FC<Props> = ({name, children}) => {
  const {from, duration} = SCENES[name];
  return (
    <Sequence from={from} durationInFrames={duration} name={name}>
      <Frame duration={duration}>{children}</Frame>
    </Sequence>
  );
};

const Frame: React.FC<{duration: number; children: React.ReactNode}> = ({duration, children}) => {
  const frame = useCurrentFrame();
  const enter = interpolate(frame, [0, 6], [0, 1], {extrapolateRight: 'clamp'});
  const exit = interpolate(frame, [duration - 10, duration], [1, 0], {extrapolateLeft: 'clamp'});
  const drift = interpolate(frame, [0, duration], [1, 1.015]);
  const leave = interpolate(frame, [duration - 10, duration], [1, 1.04], {extrapolateLeft: 'clamp'});

  return (
    <AbsoluteFill
      style={{
        opacity: enter * exit,
        transform: `scale(${drift * leave})`,
        padding: '0 120px',
      }}
    >
      {children}
    </AbsoluteFill>
  );
};
