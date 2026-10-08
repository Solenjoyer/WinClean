import React from 'react';
import {AbsoluteFill, interpolate, useCurrentFrame, useVideoConfig} from 'remotion';
import {Glyph} from '../components/Glyph';
import {Logo} from '../components/Logo';
import {fadeIn, lively, rise, smooth} from '../motion';
import {colors, fonts} from '../theme';

export const Outro: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const logo = rise(frame, fps, 4, lively);
  const glow = interpolate(frame, [4, 30], [0, 0.9], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  const wordmark = rise(frame, fps, 20, smooth);
  const line = rise(frame, fps, 34, smooth);
  const url = rise(frame, fps, 48, lively);

  return (
    <AbsoluteFill style={{alignItems: 'center', justifyContent: 'center'}}>
      <div style={{transform: `scale(${0.5 + 0.5 * logo})`, opacity: Math.min(1, logo * 1.5)}}>
        <Logo size={170} ring={1} dot={1} glow={glow} />
      </div>
      <div style={{marginTop: 34, fontFamily: fonts.display, fontSize: 96, fontWeight: 600, letterSpacing: '-0.03em', color: colors.text, opacity: wordmark, transform: `translateY(${(1 - wordmark) * 24}px)`}}>
        WinClean
      </div>
      <div style={{marginTop: 8, fontSize: 34, color: colors.secondary, opacity: line, transform: `translateY(${(1 - line) * 16}px)`}}>Free and open source. MIT licence.</div>
      <div
        style={{
          marginTop: 40,
          padding: '16px 34px',
          borderRadius: 999,
          border: `1px solid ${colors.stroke}`,
          background: colors.surfaceRaised,
          fontFamily: fonts.text,
          fontSize: 34,
          fontWeight: 500,
          color: colors.accent,
          opacity: Math.min(1, url * 1.5),
          transform: `scale(${0.8 + 0.2 * url})`,
          boxShadow: `0 0 50px ${colors.accent}33`,
        }}
      >
        github.com/Solenjoyer/WinClean
      </div>
      <div style={{marginTop: 28, display: 'flex', alignItems: 'center', gap: 12, fontSize: 24, color: colors.tertiary, opacity: fadeIn(frame, 62, 12)}}>
        <Glyph kind="windows" color={colors.tertiary} size={22} />
        For Windows 10 and 11 · x64 and ARM64 · portable or installer
      </div>
    </AbsoluteFill>
  );
};
