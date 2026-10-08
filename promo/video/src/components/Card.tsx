import React from 'react';
import {useCurrentFrame, useVideoConfig} from 'remotion';
import {rise, smooth} from '../motion';
import {colors} from '../theme';

type Props = {
  delay?: number;
  style?: React.CSSProperties;
  children?: React.ReactNode;
};

export const Card: React.FC<Props> = ({delay = 0, style, children}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const progress = rise(frame, fps, delay, smooth);

  return (
    <div
      style={{
        position: 'absolute',
        boxSizing: 'border-box',
        padding: 28,
        borderRadius: 18,
        background: colors.surface,
        border: `1px solid ${colors.stroke}`,
        boxShadow: '0 30px 70px rgba(0, 0, 0, 0.38), inset 0 1px 0 rgba(255, 255, 255, 0.07)',
        opacity: progress,
        transform: `translateY(${(1 - progress) * 56}px) scale(${0.965 + progress * 0.035})`,
        ...style,
      }}
    >
      {children}
    </div>
  );
};
