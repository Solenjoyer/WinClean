import React from 'react';

export type GlyphKind =
  | 'cpu' | 'memory' | 'disk' | 'network' | 'folder' | 'shield' | 'link' | 'chevron'
  | 'overview' | 'processes' | 'cleanup' | 'health' | 'hardware' | 'settings'
  | 'windows' | 'search' | 'taskview' | 'terminal' | 'wifi' | 'volume' | 'chevronUp'
  | 'minimize' | 'maximize' | 'restore' | 'close';

type Props = {kind: GlyphKind; color?: string; size?: number; rotate?: number};

// Small line icons drawn by hand so the video needs no icon font.
export const Glyph: React.FC<Props> = ({kind, color = 'currentColor', size = 24, rotate = 0}) => {
  const line = {
    fill: 'none',
    stroke: color,
    strokeWidth: 1.8,
    strokeLinecap: 'round' as const,
    strokeLinejoin: 'round' as const,
  };
  const thin = {...line, strokeWidth: 1.4};

  const body = (() => {
    switch (kind) {
      case 'cpu':
        return (
          <g {...line}>
            <rect x="6" y="6" width="12" height="12" rx="2" />
            <rect x="9.5" y="9.5" width="5" height="5" rx="1" />
            <path d="M9 2v4M15 2v4M9 18v4M15 18v4M2 9h4M2 15h4M18 9h4M18 15h4" />
          </g>
        );
      case 'memory':
        return (
          <g {...line}>
            <rect x="3" y="7" width="18" height="10" rx="2" />
            <path d="M7 7v10M11 7v10M15 7v10M19 7v10M3 20h18" />
          </g>
        );
      case 'disk':
        return (
          <g {...line}>
            <circle cx="12" cy="12" r="9" />
            <circle cx="12" cy="12" r="2.5" />
            <path d="M12 3v3M12 18v3" />
          </g>
        );
      case 'network':
        return (
          <g {...line}>
            <path d="M4 20l8-16 8 16" />
            <path d="M8 14h8" />
          </g>
        );
      case 'folder':
        return (
          <g {...line}>
            <path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
          </g>
        );
      case 'shield':
        return (
          <g {...line}>
            <path d="M12 3l7 3v5c0 5-3 8-7 10-4-2-7-5-7-10V6z" />
            <path d="M9 12l2 2 4-4" />
          </g>
        );
      case 'link':
        return (
          <g {...line}>
            <path d="M14 4h6v6" />
            <path d="M20 4l-9 9" />
            <path d="M18 13v5a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h5" />
          </g>
        );
      case 'chevron':
        return (
          <g {...line}>
            <path d="M9 6l6 6-6 6" />
          </g>
        );
      case 'chevronUp':
        return (
          <g {...line}>
            <path d="M6 15l6-6 6 6" />
          </g>
        );
      case 'overview':
        return (
          <g {...line}>
            <rect x="3" y="3" width="8" height="8" rx="1.5" />
            <rect x="13" y="3" width="8" height="8" rx="1.5" />
            <rect x="3" y="13" width="8" height="8" rx="1.5" />
            <rect x="13" y="13" width="8" height="8" rx="1.5" />
          </g>
        );
      case 'processes':
        return (
          <g {...line}>
            <path d="M8 6h13M8 12h13M8 18h13" />
            <circle cx="4" cy="6" r="1" fill={color} />
            <circle cx="4" cy="12" r="1" fill={color} />
            <circle cx="4" cy="18" r="1" fill={color} />
          </g>
        );
      case 'cleanup':
        return (
          <g {...line}>
            <path d="M14 3l7 7-9 9-7-7z" />
            <path d="M5 12l-2 2 7 7 2-2" />
            <path d="M12 8l4 4" />
          </g>
        );
      case 'health':
        return (
          <g {...line}>
            <path d="M3 12h4l2-5 3 10 3-7 1 2h5" />
          </g>
        );
      case 'hardware':
        return (
          <g {...line}>
            <rect x="4" y="4" width="16" height="16" rx="2" />
            <path d="M9 9h6v6H9z" />
          </g>
        );
      case 'settings':
        return (
          <g {...line}>
            <circle cx="12" cy="12" r="3" />
            <path d="M12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9l2.1 2.1M17 17l2.1 2.1M4.9 19.1L7 17M17 7l2.1-2.1" />
          </g>
        );
      case 'windows':
        return (
          <g fill={color}>
            <rect x="3" y="3" width="8.3" height="8.3" />
            <rect x="12.7" y="3" width="8.3" height="8.3" />
            <rect x="3" y="12.7" width="8.3" height="8.3" />
            <rect x="12.7" y="12.7" width="8.3" height="8.3" />
          </g>
        );
      case 'search':
        return (
          <g {...line}>
            <circle cx="10.5" cy="10.5" r="6.5" />
            <path d="M15.5 15.5L21 21" />
          </g>
        );
      case 'taskview':
        return (
          <g {...line}>
            <rect x="3" y="6" width="12" height="12" rx="1.5" />
            <path d="M9 3h12v12" />
          </g>
        );
      case 'terminal':
        return (
          <g {...line}>
            <rect x="3" y="4" width="18" height="16" rx="2" />
            <path d="M7 9l3 3-3 3M12 15h5" />
          </g>
        );
      case 'wifi':
        return (
          <g {...thin}>
            <path d="M2 9a15 15 0 0 1 20 0M5.5 12.5a10 10 0 0 1 13 0M9 16a5 5 0 0 1 6 0" />
            <circle cx="12" cy="19.5" r="1" fill={color} />
          </g>
        );
      case 'volume':
        return (
          <g {...thin}>
            <path d="M4 9h4l5-4v14l-5-4H4z" />
            <path d="M16 9a4 4 0 0 1 0 6M18.5 6.5a8 8 0 0 1 0 11" />
          </g>
        );
      case 'minimize':
        return (
          <g {...thin}>
            <path d="M6 12h12" />
          </g>
        );
      case 'maximize':
        return (
          <g {...thin}>
            <rect x="6" y="6" width="12" height="12" rx="1.5" />
          </g>
        );
      case 'restore':
        return (
          <g {...thin}>
            <rect x="5" y="8" width="11" height="11" rx="1.5" />
            <path d="M8 8V6.5A1.5 1.5 0 0 1 9.5 5h8A1.5 1.5 0 0 1 19 6.5v8a1.5 1.5 0 0 1-1.5 1.5H16" />
          </g>
        );
      case 'close':
        return (
          <g {...thin}>
            <path d="M6 6l12 12M18 6L6 18" />
          </g>
        );
      default:
        return null;
    }
  })();

  return (
    <svg width={size} height={size} viewBox="0 0 24 24" style={{display: 'block', transform: `rotate(${rotate}deg)`}}>
      {body}
    </svg>
  );
};
