import React from 'react';

type Props = {size: number; ring: number; dot: number; glow?: number};

// The same geometry as eng/icon/winclean.svg, with the ring drawn progressively.
export const Logo: React.FC<Props> = ({size, ring, dot, glow = 0}) => {
  const arcLength = 2 * Math.PI * 72.5 * 0.75;
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 256 256"
      style={{
        display: 'block',
        filter: `drop-shadow(0 0 ${40 * glow}px rgba(76, 194, 255, ${0.55 * glow}))`,
      }}
    >
      <rect x="0" y="0" width="256" height="256" rx="56" fill="#1f5fa8" />
      <path
        d="M 76.7 179.3 A 72.5 72.5 0 1 1 179.3 179.3"
        fill="none"
        stroke="#ffffff"
        strokeWidth="30"
        strokeLinecap="round"
        strokeDasharray={arcLength}
        strokeDashoffset={arcLength * (1 - Math.min(1, Math.max(0, ring)))}
      />
      <circle cx="128" cy="128" r={17 * Math.max(0, dot)} fill="#ffffff" />
    </svg>
  );
};
