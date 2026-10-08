import React from 'react';
import {AbsoluteFill, useCurrentFrame} from 'remotion';
import {SCREEN, TASKBAR_APP_X, TASKBAR_H} from '../layout';
import {colors, fonts} from '../theme';
import {Glyph, GlyphKind} from './Glyph';
import {Logo} from './Logo';

// A Windows 11 desktop: a bloom-like wallpaper and a centred taskbar with the clock.
export const Wallpaper: React.FC = () => {
  const frame = useCurrentFrame();
  const drift = Math.sin(frame / 160) * 30;
  return (
    <AbsoluteFill style={{background: 'linear-gradient(160deg, #06152E 0%, #0B2A5B 45%, #061327 100%)'}}>
      <div
        style={{
          position: 'absolute',
          left: 420 + drift,
          top: -140,
          width: 1500,
          height: 1300,
          borderRadius: '50%',
          background: 'radial-gradient(closest-side, rgba(70, 150, 255, 0.55), rgba(70, 150, 255, 0.12) 55%, rgba(70, 150, 255, 0) 100%)',
          transform: 'rotate(-18deg) scaleX(0.75)',
        }}
      />
      <div
        style={{
          position: 'absolute',
          left: 1040 - drift,
          top: 180,
          width: 1200,
          height: 1100,
          borderRadius: '50%',
          background: 'radial-gradient(closest-side, rgba(160, 120, 255, 0.4), rgba(160, 120, 255, 0) 100%)',
          transform: 'rotate(24deg) scaleX(0.6)',
        }}
      />
      <div
        style={{
          position: 'absolute',
          left: 160,
          top: 420,
          width: 1100,
          height: 900,
          borderRadius: '50%',
          background: 'radial-gradient(closest-side, rgba(110, 200, 255, 0.35), rgba(110, 200, 255, 0) 100%)',
          transform: 'rotate(12deg) scaleX(0.8)',
        }}
      />
    </AbsoluteFill>
  );
};

const pinned: {kind: GlyphKind; color: string}[] = [
  {kind: 'search', color: '#F3F5F9'},
  {kind: 'taskview', color: '#F3F5F9'},
  {kind: 'folder', color: '#F7C948'},
  {kind: 'terminal', color: '#F3F5F9'},
];

export const Taskbar: React.FC<{appRunning: boolean}> = ({appRunning}) => (
  <div
    style={{
      position: 'absolute',
      left: 0,
      top: SCREEN.h - TASKBAR_H,
      width: SCREEN.w,
      height: TASKBAR_H,
      background: colors.taskbar,
      borderTop: '1px solid rgba(255, 255, 255, 0.08)',
      backdropFilter: 'blur(20px)',
      fontFamily: fonts.text,
    }}
  >
    <div style={{position: 'absolute', left: TASKBAR_APP_X - 5 * 48 - 12, top: 4, display: 'flex', gap: 8}}>
      <TaskbarButton>
        <Glyph kind="windows" color="#4CC2FF" size={22} />
      </TaskbarButton>
      {pinned.map((item) => (
        <TaskbarButton key={item.kind}>
          <Glyph kind={item.kind} color={item.color} size={22} />
        </TaskbarButton>
      ))}
      <TaskbarButton active={appRunning}>
        <Logo size={22} ring={1} dot={1} />
      </TaskbarButton>
    </div>
    <div style={{position: 'absolute', right: 18, top: 0, height: TASKBAR_H, display: 'flex', alignItems: 'center', gap: 14, color: colors.text}}>
      <Glyph kind="chevronUp" color={colors.text} size={16} />
      <Glyph kind="wifi" color={colors.text} size={18} />
      <Glyph kind="volume" color={colors.text} size={18} />
      <div style={{textAlign: 'right', fontSize: 13, lineHeight: '16px', marginLeft: 6}}>
        <div>09:41</div>
        <div>08/10/2026</div>
      </div>
    </div>
  </div>
);

const TaskbarButton: React.FC<{active?: boolean; children: React.ReactNode}> = ({active = false, children}) => (
  <div
    style={{
      width: 40,
      height: 40,
      borderRadius: 6,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: active ? 'rgba(255, 255, 255, 0.08)' : 'transparent',
      position: 'relative',
    }}
  >
    {children}
    {active ? (
      <div style={{position: 'absolute', left: 14, bottom: 1, width: 12, height: 3, borderRadius: 2, background: colors.accent}} />
    ) : null}
  </div>
);
