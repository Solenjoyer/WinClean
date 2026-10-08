import React from 'react';
import {useVideoConfig} from 'remotion';
import {GB, MB, formatBytes} from '../format';
import {lively, rise, series} from '../motion';
import {colors, fonts, numeric} from '../theme';
import {Logo} from './Logo';
import {Sparkline} from './Sparkline';

export const WIDGET_W = 300;
export const WIDGET_H = 318;

const rows = [
  {label: 'CPU', color: colors.cpu, values: series(301, 240, 0.26, 0.18), value: (t: number) => `${Math.round(21 + 4 * t)}%`},
  {label: 'Memory', color: colors.memory, values: series(302, 240, 0.46, 0.05, 0), value: () => formatBytes(14.2 * GB)},
  {label: 'Disk', color: colors.disk, values: series(303, 240, 0.2, 0.2, 0.12), value: (t: number) => `${formatBytes((44 + 8 * t) * MB)}/s`},
  {label: 'Network', color: colors.network, values: series(304, 240, 0.3, 0.22, 0.1), value: (t: number) => `${formatBytes((11 + 3 * t) * MB)}/s`},
];

const tools = [
  {name: 'Claude Code', cpu: '11.8%', memory: formatBytes(1.18 * GB)},
  {name: 'Cursor', cpu: '4.2%', memory: formatBytes(2.41 * GB)},
  {name: 'Docker Desktop', cpu: '1.3%', memory: formatBytes(3.02 * GB)},
];

type Props = {frame: number; appearAt: number; x: number; y: number};

// The desktop widget as the application draws it: the headline rows with live history, the
// developer tools running right now and the system drive. Values scroll on every third frame.
export const WidgetCard: React.FC<Props> = ({frame, appearAt, x, y}) => {
  const {fps} = useVideoConfig();
  const appear = rise(frame, fps, appearAt, lively);
  const shift = Math.floor(Math.max(0, frame - appearAt) / 3);
  const pulse = 0.5 + 0.5 * Math.sin(frame / 9);

  return (
    <div
      style={{
        position: 'absolute',
        left: x,
        top: y,
        width: WIDGET_W,
        boxSizing: 'border-box',
        padding: '10px 14px 12px',
        borderRadius: 12,
        background: 'rgba(28, 34, 48, 0.95)',
        border: '1px solid rgba(255, 255, 255, 0.2)',
        boxShadow: '0 20px 50px rgba(0, 0, 0, 0.45)',
        fontFamily: fonts.text,
        color: colors.text,
        opacity: Math.min(1, appear * 1.5),
        transform: `scale(${0.9 + 0.1 * appear})`,
        transformOrigin: 'top right',
      }}
    >
      <div style={{display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6}}>
        <Logo size={14} ring={1} dot={1} />
        <span style={{fontSize: 12, color: colors.secondary}}>WinClean</span>
      </div>
      {rows.map((row, i) => (
        <div key={row.label} style={{display: 'grid', gridTemplateColumns: '72px 1fr 110px', alignItems: 'center', height: 30}}>
          <span style={{fontSize: 12, color: colors.secondary}}>{row.label}</span>
          <span style={{fontSize: 14, textAlign: 'right', paddingRight: 8, ...numeric}}>{row.value(pulse)}</span>
          <Sparkline id={`widget-${i}`} values={row.values.slice(shift % 180, (shift % 180) + 60)} progress={1} color={row.color} width={110} height={22} compact />
        </div>
      ))}
      <div style={{marginTop: 8, fontSize: 12, color: colors.secondary}}>Running now</div>
      {tools.map((tool, i) => {
        const show = rise(frame, fps, appearAt + 10 + i * 4, lively);
        return (
          <div key={tool.name} style={{display: 'grid', gridTemplateColumns: '1fr 60px 76px', alignItems: 'center', height: 24, opacity: Math.min(1, show * 1.5)}}>
            <span style={{fontSize: 14}}>{tool.name}</span>
            <span style={{fontSize: 14, textAlign: 'right', ...numeric}}>{tool.cpu}</span>
            <span style={{fontSize: 14, textAlign: 'right', ...numeric}}>{tool.memory}</span>
          </div>
        );
      })}
      <div style={{marginTop: 8, fontSize: 14, color: colors.secondary, ...numeric}}>C: 412 GB free</div>
    </div>
  );
};
