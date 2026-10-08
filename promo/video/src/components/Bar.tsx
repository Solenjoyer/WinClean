import React from 'react';
import {colors} from '../theme';

type Props = {fraction: number; color?: string; height?: number; width?: number | string};

// Hex colours get a translucent glow; anything else is drawn flat.
const glowFor = (color: string) => (color.startsWith('#') ? `0 0 18px ${color}66` : 'none');

export const Bar: React.FC<Props> = ({fraction, color = colors.accent, height = 8, width = '100%'}) => (
  <div
    style={{
      width,
      height,
      borderRadius: height / 2,
      background: 'rgba(255, 255, 255, 0.08)',
      overflow: 'hidden',
    }}
  >
    <div
      style={{
        width: `${Math.max(0, Math.min(1, fraction)) * 100}%`,
        height: '100%',
        borderRadius: height / 2,
        background: color,
        boxShadow: glowFor(color),
      }}
    />
  </div>
);
