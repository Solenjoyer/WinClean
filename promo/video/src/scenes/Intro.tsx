import React from 'react';
import {AbsoluteFill, interpolate, useCurrentFrame, useVideoConfig} from 'remotion';
import {Logo} from '../components/Logo';
import {fadeIn, lively, rise, smooth} from '../motion';
import {colors, fonts} from '../theme';

const tagline = 'See what your Windows PC is really doing.';

export const Intro: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  const ring = interpolate(frame, [10, 40], [0, 1], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  const dot = rise(frame, fps, 34, lively);
  const logoScale = 0.6 + 0.4 * rise(frame, fps, 10, smooth);
  const glow = interpolate(frame, [10, 30, 90], [0, 1, 0.6], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  const wordmark = rise(frame, fps, 42, smooth);
  const spacing = interpolate(wordmark, [0, 1], [0.32, -0.03]);

  return (
    <AbsoluteFill style={{alignItems: 'center', justifyContent: 'center'}}>
      <div style={{transform: `scale(${logoScale})`, opacity: fadeIn(frame, 8, 6)}}>
        <Logo size={220} ring={ring} dot={dot} glow={glow} />
      </div>
      <div
        style={{
          marginTop: 44,
          fontFamily: fonts.display,
          fontSize: 124,
          fontWeight: 600,
          letterSpacing: `${spacing}em`,
          color: colors.text,
          opacity: Math.min(1, wordmark * 1.5),
          transform: `translateY(${(1 - wordmark) * 30}px)`,
        }}
      >
        WinClean
      </div>
      <div style={{marginTop: 18, fontFamily: fonts.text, fontSize: 38, fontWeight: 400, color: colors.secondary}}>
        {tagline.split(' ').map((word, i) => {
          const progress = rise(frame, fps, 62 + i * 3, lively);
          return (
            <span
              key={word + i}
              style={{
                display: 'inline-block',
                marginRight: '0.28em',
                opacity: Math.min(1, progress * 1.4),
                transform: `translateY(${(1 - progress) * 24}px)`,
              }}
            >
              {word}
            </span>
          );
        })}
      </div>
    </AbsoluteFill>
  );
};
