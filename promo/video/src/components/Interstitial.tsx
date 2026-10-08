import React from 'react';
import {AbsoluteFill, useVideoConfig} from 'remotion';
import {fadeIn, lively, rise} from '../motion';
import {colors, fonts} from '../theme';

export type InterstitialSpec = {from: number; to: number; text: string; accent?: string; subtitle?: string; pills?: string[]};

type Props = {frame: number; items: InterstitialSpec[]};

// Between two features the desktop dims and blurs, and one sentence takes the centre of the frame.
export const Interstitials: React.FC<Props> = ({frame, items}) => {
  const {fps} = useVideoConfig();
  return (
    <>
      {items.map((item) => {
        if (frame < item.from || frame > item.to) {
          return null;
        }
        const opacity = fadeIn(frame, item.from, 8) * (1 - fadeIn(frame, item.to - 8, 8));
        const accent = item.accent?.split(' ') ?? [];
        return (
          <AbsoluteFill
            key={item.from}
            style={{
              background: 'rgba(6, 9, 15, 0.86)',
              backdropFilter: 'blur(16px)',
              alignItems: 'center',
              justifyContent: 'center',
              opacity,
            }}
          >
            <div
              style={{
                maxWidth: 1500,
                textAlign: 'center',
                fontFamily: fonts.display,
                fontSize: 76,
                fontWeight: 600,
                letterSpacing: '-0.025em',
                lineHeight: 1.1,
                color: colors.text,
              }}
            >
              {item.text.split(' ').map((word, i) => {
                const progress = rise(frame, fps, item.from + 4 + i * 2, lively);
                return (
                  <span
                    key={i}
                    style={{
                      display: 'inline-block',
                      marginRight: '0.25em',
                      color: accent.includes(word) ? colors.accent : colors.text,
                      opacity: Math.min(1, progress * 1.4),
                      transform: `translateY(${(1 - progress) * 40}px)`,
                    }}
                  >
                    {word}
                  </span>
                );
              })}
            </div>
            {item.subtitle ? (
              <div style={{marginTop: 22, fontSize: 36, color: colors.secondary, opacity: fadeIn(frame, item.from + 18, 10), transform: `translateY(${(1 - fadeIn(frame, item.from + 18, 10)) * 14}px)`}}>
                {item.subtitle}
              </div>
            ) : null}
            {item.pills ? (
              <div style={{marginTop: 34, display: 'flex', gap: 12, justifyContent: 'center', flexWrap: 'wrap', maxWidth: 1500}}>
                {item.pills.map((pill, i) => {
                  const progress = rise(frame, fps, item.from + 18 + i * 3, lively);
                  return (
                    <span
                      key={pill}
                      style={{
                        padding: '10px 20px',
                        borderRadius: 999,
                        background: colors.surfaceRaised,
                        border: `1px solid ${colors.stroke}`,
                        fontSize: 24,
                        fontWeight: 500,
                        color: colors.text,
                        opacity: Math.min(1, progress * 1.5),
                        transform: `scale(${0.6 + 0.4 * progress})`,
                      }}
                    >
                      {pill}
                    </span>
                  );
                })}
              </div>
            ) : null}
          </AbsoluteFill>
        );
      })}
    </>
  );
};
