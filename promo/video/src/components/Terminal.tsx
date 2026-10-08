import React from 'react';
import {interpolate, useVideoConfig} from 'remotion';
import {rise, smooth} from '../motion';
import {colors} from '../theme';
import {Glyph} from './Glyph';

export const TERMINAL = {x: 300, y: 170, w: 1120, h: 520};

const lines = [
  {text: 'PS C:\\Users\\dev\\source\\winclean> claude', color: '#E6EDF3'},
  {text: '', color: ''},
  {text: '> Add a desktop widget that shows live metrics and the agents running right now', color: '#E6EDF3'},
  {text: '', color: ''},
  {text: '\u25CF Read src/WinClean/App.xaml.cs', color: colors.secondary},
  {text: '\u25CF Read src/WinClean/Services/Monitoring/MonitoringCoordinator.cs', color: colors.secondary},
  {text: '\u25CF Write src/WinClean/WidgetWindow.xaml', color: colors.secondary},
  {text: '\u25CF Bash dotnet build WinClean.slnx -c Release', color: colors.secondary},
  {text: '    Build succeeded.  0 Warning(s)  0 Error(s)', color: '#67C98D'},
  {text: '\u25CF Bash dotnet test tests/WinClean.Core.Tests -c Release', color: colors.secondary},
  {text: '    Passed!  Failed: 0, Passed: 333', color: '#67C98D'},
];

type Props = {frame: number; appearAt: number};

// A terminal with a Claude Code session, so the widget has something real to report on.
export const Terminal: React.FC<Props> = ({frame, appearAt}) => {
  const {fps} = useVideoConfig();
  const appear = rise(frame, fps, appearAt, smooth);
  const caret = Math.floor(frame / 15) % 2 === 0;

  return (
    <div
      style={{
        position: 'absolute',
        left: TERMINAL.x,
        top: TERMINAL.y,
        width: TERMINAL.w,
        height: TERMINAL.h,
        borderRadius: 8,
        overflow: 'hidden',
        background: '#0C0C0C',
        border: '1px solid rgba(255, 255, 255, 0.12)',
        boxShadow: '0 30px 80px rgba(0, 0, 0, 0.55)',
        opacity: appear,
        transform: `translateY(${(1 - appear) * 30}px) scale(${0.97 + 0.03 * appear})`,
      }}
    >
      <div style={{height: 40, background: '#1A1A1A', display: 'flex', alignItems: 'center', paddingLeft: 12, gap: 10, borderBottom: '1px solid rgba(255, 255, 255, 0.08)'}}>
        <div style={{display: 'flex', alignItems: 'center', gap: 8, padding: '6px 14px', borderRadius: 6, background: '#0C0C0C', fontSize: 13, color: colors.text}}>
          <Glyph kind="terminal" color={colors.text} size={14} />
          pwsh
        </div>
        <div style={{position: 'absolute', right: 0, top: 0, display: 'flex'}}>
          {(['minimize', 'maximize', 'close'] as const).map((kind) => (
            <div key={kind} style={{width: 46, height: 40, display: 'flex', alignItems: 'center', justifyContent: 'center'}}>
              <Glyph kind={kind} color={colors.text} size={16} />
            </div>
          ))}
        </div>
      </div>
      <div style={{padding: '18px 22px', fontFamily: '"Cascadia Mono", Consolas, "DejaVu Sans Mono", monospace', fontSize: 17, lineHeight: '28px', whiteSpace: 'pre'}}>
        {lines.map((line, i) => {
          const at = appearAt + 14 + i * 9;
          const visible = interpolate(frame, [at, at + 2], [0, 1], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
          return (
            <div key={i} style={{color: line.color, opacity: visible}}>
              {line.text}
            </div>
          );
        })}
        <div style={{color: '#E6EDF3', opacity: frame >= appearAt + 14 + lines.length * 9 ? 1 : 0}}>
          {'\u25CF Done in 2m 14s'}
          <span style={{opacity: caret ? 1 : 0}}>{' \u258C'}</span>
        </div>
      </div>
    </div>
  );
};
