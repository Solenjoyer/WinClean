import React from 'react';
import {useCurrentFrame, useVideoConfig} from 'remotion';
import {rise, snappy} from '../motion';
import {colors} from '../theme';

type Props = {delay: number; size?: number; round?: boolean};

// A checkbox that fills and draws its mark, like a Fluent CheckBox being ticked.
export const Check: React.FC<Props> = ({delay, size = 30, round = false}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const progress = rise(frame, fps, delay, snappy);
  const checked = frame >= delay;

  return (
    <svg width={size} height={size} viewBox="0 0 30 30" style={{display: 'block', flexShrink: 0}}>
      <rect
        x="1.5"
        y="1.5"
        width="27"
        height="27"
        rx={round ? 13.5 : 6}
        fill={checked ? colors.accent : 'transparent'}
        fillOpacity={checked ? Math.min(1, progress) : 0}
        stroke={checked ? colors.accent : 'rgba(243, 245, 249, 0.45)'}
        strokeWidth="2"
      />
      <path
        d="M 8 15.5 L 13 20.5 L 22 10.5"
        fill="none"
        stroke="#08121f"
        strokeWidth="3"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeDasharray={24}
        strokeDashoffset={24 * (1 - progress)}
      />
    </svg>
  );
};
