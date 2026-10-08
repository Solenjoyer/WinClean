import React from 'react';
import {interpolate, useVideoConfig} from 'remotion';
import {lively, rise} from '../motion';
import {colors, fonts} from '../theme';

export type CaptionSpec = {from: number; to: number; text: string; accent?: string; pills?: string[]};

type Props = {frame: number; captions: CaptionSpec[]};

// Lower-third captions: one sentence at a time, each word springing up, over a dark pill.
export const Captions: React.FC<Props> = ({frame, captions}) => {
  const {fps} = useVideoConfig();
  return (
    <>
      {captions.map((caption) => {
        if (frame < caption.from - 2 || frame > caption.to + 10) {
          return null;
        }
        const local = frame - caption.from;
        const out = interpolate(frame, [caption.to, caption.to + 8], [1, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
        const inProgress = rise(frame, fps, caption.from, lively);
        const words = caption.text.split(' ');
        return (
          <div
            key={caption.from}
            style={{
              position: 'absolute',
              left: 0,
              right: 0,
              top: caption.pills ? 850 : 926,
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              gap: 14,
              opacity: Math.min(1, inProgress * 1.5) * out,
              transform: `translateY(${(1 - inProgress) * 24 + (1 - out) * 16}px)`,
            }}
          >
            <div
              style={{
                padding: '14px 30px',
                borderRadius: 16,
                background: 'rgba(8, 11, 18, 0.84)',
                border: '1px solid rgba(255, 255, 255, 0.1)',
                boxShadow: '0 20px 50px rgba(0, 0, 0, 0.45)',
                fontFamily: fonts.display,
                fontSize: 40,
                fontWeight: 600,
                letterSpacing: '-0.02em',
                color: colors.text,
                whiteSpace: 'nowrap',
              }}
            >
              {words.map((word, i) => {
                const progress = rise(frame, fps, caption.from + 2 + i * 2, lively);
                const highlighted = caption.accent !== undefined && caption.accent.split(' ').includes(word);
                return (
                  <span
                    key={i}
                    style={{
                      display: 'inline-block',
                      marginRight: '0.24em',
                      color: highlighted ? colors.accent : colors.text,
                      opacity: Math.min(1, progress * 1.4),
                      transform: `translateY(${(1 - progress) * 22}px)`,
                    }}
                  >
                    {word}
                  </span>
                );
              })}
            </div>
            {caption.pills ? (
              <div style={{display: 'flex', gap: 10}}>
                {caption.pills.map((pill, i) => {
                  const progress = rise(frame, fps, caption.from + 10 + i * 3, lively);
                  return (
                    <span
                      key={pill}
                      style={{
                        padding: '8px 16px',
                        borderRadius: 999,
                        background: 'rgba(8, 11, 18, 0.84)',
                        border: '1px solid rgba(255, 255, 255, 0.14)',
                        fontSize: 21,
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
            {local < 0 ? null : null}
          </div>
        );
      })}
    </>
  );
};
