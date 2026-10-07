import React from 'react';

type Props = {kind: 'cpu' | 'memory' | 'disk' | 'network' | 'folder' | 'shield' | 'link' | 'chevron'; color?: string; size?: number; rotate?: number};

// Small line icons drawn by hand so the video needs no icon font.
export const Glyph: React.FC<Props> = ({kind, color = 'currentColor', size = 24, rotate = 0}) => {
  const common = {
    fill: 'none',
    stroke: color,
    strokeWidth: 1.8,
    strokeLinecap: 'round' as const,
    strokeLinejoin: 'round' as const,
  };
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" style={{display: 'block', transform: `rotate(${rotate}deg)`}}>
      {kind === 'cpu' ? (
        <g {...common}>
          <rect x="6" y="6" width="12" height="12" rx="2" />
          <rect x="9.5" y="9.5" width="5" height="5" rx="1" />
          <path d="M9 2v4M15 2v4M9 18v4M15 18v4M2 9h4M2 15h4M18 9h4M18 15h4" />
        </g>
      ) : null}
      {kind === 'memory' ? (
        <g {...common}>
          <rect x="3" y="7" width="18" height="10" rx="2" />
          <path d="M7 7v10M11 7v10M15 7v10M19 7v10M3 20h18" />
        </g>
      ) : null}
      {kind === 'disk' ? (
        <g {...common}>
          <circle cx="12" cy="12" r="9" />
          <circle cx="12" cy="12" r="2.5" />
          <path d="M12 3v3M12 18v3" />
        </g>
      ) : null}
      {kind === 'network' ? (
        <g {...common}>
          <path d="M4 20l8-16 8 16" />
          <path d="M8 14h8" />
        </g>
      ) : null}
      {kind === 'folder' ? (
        <g {...common}>
          <path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
        </g>
      ) : null}
      {kind === 'shield' ? (
        <g {...common}>
          <path d="M12 3l7 3v5c0 5-3 8-7 10-4-2-7-5-7-10V6z" />
          <path d="M9 12l2 2 4-4" />
        </g>
      ) : null}
      {kind === 'link' ? (
        <g {...common}>
          <path d="M14 4h6v6" />
          <path d="M20 4l-9 9" />
          <path d="M18 13v5a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h5" />
        </g>
      ) : null}
      {kind === 'chevron' ? (
        <g {...common}>
          <path d="M9 6l6 6-6 6" />
        </g>
      ) : null}
    </svg>
  );
};
