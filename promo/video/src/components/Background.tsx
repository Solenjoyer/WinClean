import React from 'react';
import {AbsoluteFill, useCurrentFrame} from 'remotion';
import {colors} from '../theme';

export const Background: React.FC = () => {
  const frame = useCurrentFrame();
  const driftX = Math.sin(frame / 110) * 70;
  const driftY = Math.cos(frame / 140) * 50;

  return (
    <AbsoluteFill style={{backgroundColor: colors.background}}>
      <div
        style={{
          position: 'absolute',
          left: 260 + driftX,
          top: -320 + driftY,
          width: 1500,
          height: 1100,
          background: 'radial-gradient(closest-side, rgba(76, 194, 255, 0.17), rgba(76, 194, 255, 0))',
        }}
      />
      <div
        style={{
          position: 'absolute',
          left: 1000 - driftX,
          top: 320 - driftY,
          width: 1400,
          height: 1100,
          background: 'radial-gradient(closest-side, rgba(177, 156, 242, 0.11), rgba(177, 156, 242, 0))',
        }}
      />
      <AbsoluteFill
        style={{
          backgroundImage: 'radial-gradient(rgba(255, 255, 255, 0.07) 1px, transparent 1px)',
          backgroundSize: '32px 32px',
          backgroundPosition: '16px 16px',
        }}
      />
      <AbsoluteFill
        style={{
          background: 'radial-gradient(ellipse at center, rgba(0, 0, 0, 0) 50%, rgba(0, 0, 0, 0.6) 100%)',
        }}
      />
    </AbsoluteFill>
  );
};
