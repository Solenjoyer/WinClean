import React from 'react';
import {interpolate, useCurrentFrame, useVideoConfig} from 'remotion';
import {CAPTION_W, CONTENT, RAIL_W, SCREEN, TITLE_H, WINDOW_H, railItemCenter} from '../layout';
import {rise, smooth} from '../motion';
import {PAGE_ORDER, PageKey} from '../timeline';
import {colors, fonts} from '../theme';
import {Glyph, GlyphKind} from './Glyph';
import {Logo} from './Logo';

export type WindowRect = {x: number; y: number; w: number; h: number};

type Props = {
  rect: WindowRect;
  opacity: number;
  page: PageKey;
  pageChangedAt: number;
  children: React.ReactNode;
};

const rail: {key: PageKey | 'hardware' | 'settings'; label: string; glyph: GlyphKind}[] = [
  {key: 'overview', label: 'Overview', glyph: 'overview'},
  {key: 'processes', label: 'Processes', glyph: 'processes'},
  {key: 'storage', label: 'Storage', glyph: 'disk'},
  {key: 'cleanup', label: 'Cleanup', glyph: 'cleanup'},
  {key: 'health', label: 'Health', glyph: 'health'},
  {key: 'hardware', label: 'Hardware', glyph: 'hardware'},
  {key: 'settings', label: 'Settings', glyph: 'settings'},
];

// The WinClean window: title bar, navigation rail with the sliding indicator, and the content
// area. It is laid out at full size and scaled when the window is not maximised.
export const AppWindow: React.FC<Props> = ({rect, opacity, page, pageChangedAt, children}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const scale = rect.w / SCREEN.w;
  const maximized = rect.w >= SCREEN.w - 1;
  const index = PAGE_ORDER.indexOf(page);
  const previous = Math.max(0, index - 1);
  const slide = rise(frame, fps, pageChangedAt, smooth);
  const indicatorY = interpolate(slide, [0, 1], [railItemCenter(previous).y, railItemCenter(index).y]);

  return (
    <div
      style={{
        position: 'absolute',
        left: rect.x,
        top: rect.y,
        width: rect.w,
        height: rect.h,
        opacity,
        borderRadius: maximized ? 0 : 8,
        overflow: 'hidden',
        boxShadow: maximized ? 'none' : '0 30px 80px rgba(0, 0, 0, 0.55)',
        border: maximized ? 'none' : '1px solid rgba(255, 255, 255, 0.12)',
      }}
    >
      <div style={{position: 'absolute', left: 0, top: 0, width: SCREEN.w, height: WINDOW_H, transform: `scale(${scale})`, transformOrigin: '0 0', background: colors.content, fontFamily: fonts.text}}>
        <div style={{position: 'absolute', left: 0, top: 0, width: SCREEN.w, height: TITLE_H, background: colors.chrome, display: 'flex', alignItems: 'center', paddingLeft: 14, gap: 10}}>
          <Logo size={16} ring={1} dot={1} />
          <span style={{fontSize: 13, color: colors.text}}>WinClean</span>
          <div style={{position: 'absolute', right: 0, top: 0, display: 'flex'}}>
            {(['minimize', maximized ? 'maximize' : 'restore', 'close'] as GlyphKind[]).map((kind) => (
              <div key={kind} style={{width: CAPTION_W, height: TITLE_H, display: 'flex', alignItems: 'center', justifyContent: 'center'}}>
                <Glyph kind={kind} color={colors.text} size={18} />
              </div>
            ))}
          </div>
        </div>

        <div style={{position: 'absolute', left: 0, top: TITLE_H, width: RAIL_W, height: WINDOW_H - TITLE_H, background: colors.rail, borderRight: '1px solid rgba(255, 255, 255, 0.06)'}}>
          <div
            style={{
              position: 'absolute',
              left: 6,
              top: indicatorY - TITLE_H - 8,
              width: 3,
              height: 16,
              borderRadius: 2,
              background: colors.accent,
            }}
          />
          {rail.map((item, i) => {
            const selected = item.key === page;
            const center = railItemCenter(i);
            return (
              <div
                key={item.key}
                style={{
                  position: 'absolute',
                  left: 12,
                  top: center.y - TITLE_H - 18,
                  width: RAIL_W - 24,
                  height: 36,
                  borderRadius: 6,
                  background: selected ? 'rgba(255, 255, 255, 0.07)' : 'transparent',
                  display: 'flex',
                  alignItems: 'center',
                  gap: 12,
                  paddingLeft: 14,
                  color: colors.text,
                  fontSize: 14,
                  fontWeight: selected ? 600 : 400,
                }}
              >
                <Glyph kind={item.glyph} color={colors.text} size={17} />
                {item.label}
              </div>
            );
          })}
        </div>

        <div style={{position: 'absolute', left: CONTENT.x, top: CONTENT.y, width: CONTENT.w, height: CONTENT.h, overflow: 'hidden'}}>{children}</div>
      </div>
    </div>
  );
};

export const PageHeader: React.FC<{title: string; right?: string; opacity?: number}> = ({title, right, opacity = 1}) => (
  <div style={{position: 'absolute', left: 32, right: 32, top: 26, display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', opacity}}>
    <div style={{fontFamily: fonts.display, fontSize: 28, fontWeight: 600, letterSpacing: '-0.01em', color: colors.text}}>{title}</div>
    {right ? <div style={{fontSize: 15, color: colors.tertiary}}>{right}</div> : null}
  </div>
);
