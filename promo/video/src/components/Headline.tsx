import React from 'react';
import {useCurrentFrame, useVideoConfig} from 'remotion';
import {fadeIn, rise, lively} from '../motion';
import {colors, fonts} from '../theme';

export type Segment = {text: string; color?: string};

type Props = {
  segments: Segment[];
  eyebrow?: string;
  delay?: number;
  size?: number;
  top?: number;
  align?: 'left' | 'center';
  maxWidth?: number;
};

// Kinetic headline: every word springs up on its own, a few frames after the previous one.
export const Headline: React.FC<Props> = ({
  segments,
  eyebrow,
  delay = 0,
  size = 76,
  top = 100,
  align = 'left',
  maxWidth = 1680,
}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  let index = 0;

  return (
    <div
      style={{
        position: 'absolute',
        top,
        left: align === 'center' ? 0 : 120,
        right: align === 'center' ? 0 : undefined,
        textAlign: align,
        fontFamily: fonts.display,
      }}
    >
      {eyebrow ? (
        <div
          style={{
            opacity: fadeIn(frame, delay, 10),
            transform: `translateY(${(1 - fadeIn(frame, delay, 10)) * 10}px)`,
            fontFamily: fonts.text,
            fontSize: 22,
            fontWeight: 600,
            letterSpacing: '0.2em',
            textTransform: 'uppercase',
            color: colors.accent,
            marginBottom: 18,
          }}
        >
          {eyebrow}
        </div>
      ) : null}
      <div
        style={{
          display: 'inline-block',
          maxWidth,
          fontSize: size,
          fontWeight: 600,
          letterSpacing: '-0.025em',
          lineHeight: 1.08,
          color: colors.text,
        }}
      >
        {segments.map((segment, segmentIndex) =>
          segment.text.split(' ').map((word, wordIndex) => {
            const progress = rise(frame, fps, delay + 4 + index * 3, lively);
            index += 1;
            return (
              <span
                key={`${segmentIndex}-${wordIndex}`}
                style={{
                  display: 'inline-block',
                  marginRight: '0.26em',
                  color: segment.color ?? colors.text,
                  opacity: Math.min(1, progress * 1.4),
                  transform: `translateY(${(1 - progress) * 46}px)`,
                }}
              >
                {word}
              </span>
            );
          }),
        )}
      </div>
    </div>
  );
};
