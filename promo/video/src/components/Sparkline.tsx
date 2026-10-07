import React from 'react';
import {colors} from '../theme';

type Props = {
  values: number[];
  progress: number;
  color: string;
  width: number;
  height: number;
  id: string;
};

// Smooth path through the revealed points, with a soft area fill and a glowing stroke.
export const Sparkline: React.FC<Props> = ({values, progress, color, width, height, id}) => {
  const visible = Math.max(2, Math.floor(values.length * Math.min(1, progress)));
  const step = width / (values.length - 1);
  const points = values.slice(0, visible).map((value, i) => ({
    x: i * step,
    y: height - value * (height - 6) - 3,
  }));

  let path = `M ${points[0].x} ${points[0].y}`;
  for (let i = 0; i < points.length - 1; i += 1) {
    const current = points[i];
    const next = points[i + 1];
    const controlX = (current.x + next.x) / 2;
    path += ` C ${controlX} ${current.y}, ${controlX} ${next.y}, ${next.x} ${next.y}`;
  }
  const last = points[points.length - 1];
  const area = `${path} L ${last.x} ${height} L 0 ${height} Z`;
  const pulse = 1 + 0.25 * Math.sin(progress * 40);

  return (
    <svg width={width} height={height} style={{overflow: 'visible', display: 'block'}}>
      <defs>
        <linearGradient id={`${id}-fill`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={color} stopOpacity={0.32} />
          <stop offset="100%" stopColor={color} stopOpacity={0} />
        </linearGradient>
      </defs>
      <path d={area} fill={`url(#${id}-fill)`} />
      <path d={path} fill="none" stroke={color} strokeWidth={10} strokeOpacity={0.18} strokeLinecap="round" />
      <path d={path} fill="none" stroke={color} strokeWidth={3} strokeLinejoin="round" strokeLinecap="round" />
      <circle cx={last.x} cy={last.y} r={9 * pulse} fill={color} fillOpacity={0.25} />
      <circle cx={last.x} cy={last.y} r={4.5} fill={colors.text} />
    </svg>
  );
};
