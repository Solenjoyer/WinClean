import React from 'react';
import {AbsoluteFill, interpolate, useCurrentFrame, useVideoConfig} from 'remotion';
import {Card} from '../components/Card';
import {Check} from '../components/Check';
import {Glyph} from '../components/Glyph';
import {Headline} from '../components/Headline';
import {GB, MB, formatBytes, formatCount} from '../format';
import {countUp, fadeIn, lively, rise, smooth, snappy} from '../motion';
import {colors, fonts, numeric} from '../theme';

const categories = [
  {name: 'User temporary files', detail: '612 files', files: 612, bytes: 1.84 * GB, risk: 'Safe', admin: false},
  {name: 'Browser caches', detail: '1,120 files · Chrome, Edge, Firefox', files: 1120, bytes: 2.37 * GB, risk: 'Safe', admin: false},
  {name: 'Package manager caches', detail: '418 files · npm, pip, NuGet', files: 418, bytes: 3.12 * GB, risk: 'Safe', admin: false},
  {name: 'IDE caches and logs', detail: '203 files · VS Code, Cursor, JetBrains', files: 203, bytes: 641 * MB, risk: 'Safe', admin: false},
  {name: 'Windows Update cache', detail: '57 files', files: 57, bytes: 4.21 * GB, risk: 'Safe', admin: true},
  {name: 'Docker', detail: '8 dangling images and the build cache', files: 8, bytes: 2.06 * GB, risk: 'Caution', admin: false},
];

const checksFrom = 22;
const checkEvery = 9;
const dialogAt = 100;
const confirmAt = 135;
const resultAt = 150;

export const Cleanup: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  const ticked = Math.min(categories.length, Math.max(0, Math.floor((frame - checksFrom) / checkEvery) + 1));
  const selectedBytes = categories.slice(0, ticked).reduce((sum, c) => sum + c.bytes, 0);
  const selectedFiles = categories.slice(0, ticked).reduce((sum, c) => sum + c.files, 0);

  const dialog = rise(frame, fps, dialogAt, snappy);
  const dialogOut = interpolate(frame, [confirmAt + 6, confirmAt + 14], [1, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  const pressed = frame >= confirmAt && frame < confirmAt + 6;
  const buttonPressed = frame >= 90 && frame < 96;
  const freed = countUp(frame, resultAt, 40, 14.0 * GB);
  const burst = rise(frame, fps, resultAt, smooth);
  const result = rise(frame, fps, resultAt, lively);

  return (
    <>
      <Headline eyebrow="Cleanup" segments={[{text: 'Preview.'}, {text: 'Confirm.'}, {text: 'Clean.', color: colors.accent}]} delay={4} />

      <Card delay={12} style={{left: 120, top: 290, width: 1000, height: 660, padding: '18px 28px'}}>
        {categories.map((category, i) => {
          const show = rise(frame, fps, 18 + i * 4, smooth);
          return (
            <div
              key={category.name}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 20,
                height: 84,
                borderBottom: `1px solid ${colors.stroke}`,
                opacity: show,
                transform: `translateX(${(1 - show) * -18}px)`,
              }}
            >
              <Check delay={checksFrom + i * checkEvery} />
              <div style={{flex: 1}}>
                <div style={{display: 'flex', alignItems: 'center', gap: 10, fontSize: 24, color: colors.text}}>
                  {category.name}
                  {category.admin ? <Glyph kind="shield" color={colors.accent} size={22} /> : null}
                </div>
                <div style={{marginTop: 4, fontSize: 18, color: colors.tertiary}}>{category.detail}</div>
              </div>
              <div style={{display: 'flex', alignItems: 'center', gap: 10, width: 120, fontSize: 18, color: colors.secondary}}>
                <span style={{width: 9, height: 9, borderRadius: 5, background: category.risk === 'Safe' ? colors.success : colors.caution}} />
                {category.risk}
              </div>
              <div style={{width: 120, textAlign: 'right', fontSize: 24, color: colors.text, ...numeric}}>{formatBytes(category.bytes)}</div>
            </div>
          );
        })}
        <div style={{display: 'flex', alignItems: 'center', justifyContent: 'space-between', height: 100, opacity: fadeIn(frame, 30, 10)}}>
          <div style={{fontSize: 22, color: colors.secondary, ...numeric}}>
            {ticked} categories · {formatCount(selectedFiles)} files · <span style={{color: colors.text, fontWeight: 600}}>{formatBytes(selectedBytes)}</span> selected
          </div>
          <div
            style={{
              padding: '14px 28px',
              borderRadius: 10,
              background: colors.accent,
              color: '#08121f',
              fontSize: 22,
              fontWeight: 600,
              transform: `scale(${buttonPressed ? 0.96 : 1})`,
              boxShadow: buttonPressed ? 'none' : `0 10px 30px ${colors.accent}55`,
            }}
          >
            Clean selected
          </div>
        </div>
      </Card>

      <Card delay={20} style={{left: 1144, top: 290, width: 656, height: 660}}>
        <div style={{fontSize: 22, fontWeight: 500, color: colors.secondary}}>Result</div>
        <AbsoluteFill style={{alignItems: 'center', justifyContent: 'center', padding: 28}}>
          <div style={{opacity: 1 - fadeIn(frame, resultAt - 10, 6), fontSize: 22, color: colors.tertiary, position: 'absolute'}}>Nothing deleted yet</div>
          <div
            style={{
              position: 'absolute',
              width: 700,
              height: 700,
              borderRadius: 350,
              background: `radial-gradient(closest-side, ${colors.accent}55, ${colors.accent}00)`,
              transform: `scale(${burst * 1.2})`,
              opacity: burst * (1 - interpolate(frame, [resultAt + 10, resultAt + 60], [0, 0.5], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'})),
            }}
          />
          <div style={{textAlign: 'center', opacity: Math.min(1, result * 1.5), transform: `scale(${0.8 + 0.2 * result})`}}>
            <div style={{fontFamily: fonts.display, fontSize: 120, fontWeight: 600, letterSpacing: '-0.03em', color: colors.text, ...numeric}}>
              {formatBytes(freed)}
            </div>
            <div style={{marginTop: 4, fontSize: 36, fontWeight: 500, color: colors.accent}}>freed</div>
            <div style={{marginTop: 34, fontSize: 21, lineHeight: 1.5, color: colors.secondary, opacity: fadeIn(frame, resultAt + 30, 12), ...numeric}}>
              2,391 files deleted · 22 skipped (in use) · 5 access denied
              <br />
              Every run is logged. Nothing is deleted silently.
            </div>
          </div>
        </AbsoluteFill>
      </Card>

      <AbsoluteFill
        style={{
          alignItems: 'center',
          justifyContent: 'center',
          background: 'rgba(4, 8, 14, 0.55)',
          opacity: Math.min(1, dialog * 1.5) * dialogOut,
          pointerEvents: 'none',
        }}
      >
        <div
          style={{
            width: 720,
            padding: 36,
            borderRadius: 16,
            background: '#161C27',
            border: `1px solid ${colors.stroke}`,
            boxShadow: '0 40px 100px rgba(0, 0, 0, 0.6)',
            transform: `scale(${0.9 + 0.1 * dialog})`,
          }}
        >
          <div style={{fontFamily: fonts.display, fontSize: 36, fontWeight: 600, color: colors.text, ...numeric}}>Delete 2,418 files?</div>
          <div style={{marginTop: 14, fontSize: 23, lineHeight: 1.5, color: colors.secondary, ...numeric}}>
            14.2 GB on C: will be deleted permanently. This cannot be undone.
          </div>
          <div style={{marginTop: 18, fontSize: 20, color: colors.tertiary}}>Chrome is running. Files in use will be skipped.</div>
          <div style={{marginTop: 32, display: 'flex', justifyContent: 'flex-end', gap: 12}}>
            <div style={{padding: '12px 24px', borderRadius: 8, border: `1px solid ${colors.stroke}`, fontSize: 21, color: colors.text}}>Cancel</div>
            <div
              style={{
                padding: '12px 24px',
                borderRadius: 8,
                background: colors.accent,
                color: '#08121f',
                fontSize: 21,
                fontWeight: 600,
                transform: `scale(${pressed ? 0.95 : 1})`,
                ...numeric,
              }}
            >
              Delete 2,418 files
            </div>
          </div>
        </div>
      </AbsoluteFill>
    </>
  );
};
