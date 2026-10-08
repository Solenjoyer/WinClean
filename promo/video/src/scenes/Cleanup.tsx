import React from 'react';
import {interpolate, useCurrentFrame, useVideoConfig} from 'remotion';
import {PageHeader} from '../components/AppWindow';
import {Card} from '../components/Card';
import {Check} from '../components/Check';
import {Glyph} from '../components/Glyph';
import {card, cardTitle} from '../components/Page';
import {CONTENT} from '../layout';
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

export const CHECKS_FROM = 20;
export const CHECK_EVERY = 9;
export const CLEAN_CLICK = 100;
export const DIALOG_AT = 104;
export const CONFIRM_CLICK = 150;
export const RESULT_AT = 165;

const LIST = {x: 32, y: 92, w: 1030, h: 820};
const DIALOG = {w: 640, h: 262};
const dialogLeft = (CONTENT.w - DIALOG.w) / 2;
const dialogTop = (CONTENT.h - DIALOG.h) / 2 - 20;

// Pointer targets in frame pixels.
export const CLEAN_BUTTON = {x: CONTENT.x + LIST.x + LIST.w - 24 - 80, y: CONTENT.y + LIST.y + 20 + 6 * 84 + 50};
export const DELETE_BUTTON = {x: CONTENT.x + dialogLeft + DIALOG.w - 30 - 90, y: CONTENT.y + dialogTop + DIALOG.h - 30 - 18};

export const Cleanup: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  const ticked = Math.min(categories.length, Math.max(0, Math.floor((frame - CHECKS_FROM) / CHECK_EVERY) + 1));
  const selectedBytes = categories.slice(0, ticked).reduce((sum, c) => sum + c.bytes, 0);
  const selectedFiles = categories.slice(0, ticked).reduce((sum, c) => sum + c.files, 0);

  const dialog = rise(frame, fps, DIALOG_AT, snappy);
  const dialogOut = interpolate(frame, [CONFIRM_CLICK + 6, CONFIRM_CLICK + 14], [1, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  const confirmPressed = frame >= CONFIRM_CLICK && frame < CONFIRM_CLICK + 6;
  const cleanPressed = frame >= CLEAN_CLICK && frame < CLEAN_CLICK + 6;
  const freed = countUp(frame, RESULT_AT, 40, 14.0 * GB);
  const burst = rise(frame, fps, RESULT_AT, smooth);
  const result = rise(frame, fps, RESULT_AT, lively);

  return (
    <>
      <PageHeader title="Cleanup" right="Analysed just now · 14.2 GB reclaimable" />

      <Card delay={8} style={{...card, left: LIST.x, top: LIST.y, width: LIST.w, height: LIST.h, padding: '12px 24px'}}>
        {categories.map((category, i) => {
          const show = rise(frame, fps, 12 + i * 4, smooth);
          return (
            <div key={category.name} style={{display: 'flex', alignItems: 'center', gap: 16, height: 84, borderBottom: `1px solid ${colors.stroke}`, opacity: show, transform: `translateX(${(1 - show) * -14}px)`}}>
              <Check delay={CHECKS_FROM + i * CHECK_EVERY} size={22} />
              <div style={{flex: 1}}>
                <div style={{display: 'flex', alignItems: 'center', gap: 8, fontSize: 16, color: colors.text}}>
                  {category.name}
                  {category.admin ? <Glyph kind="shield" color={colors.accent} size={16} /> : null}
                </div>
                <div style={{marginTop: 3, fontSize: 13, color: colors.tertiary}}>{category.detail}</div>
              </div>
              <div style={{display: 'flex', alignItems: 'center', gap: 8, width: 100, fontSize: 13, color: colors.secondary}}>
                <span style={{width: 7, height: 7, borderRadius: 4, background: category.risk === 'Safe' ? colors.success : colors.caution}} />
                {category.risk}
              </div>
              <div style={{width: 100, textAlign: 'right', fontSize: 16, color: colors.text, ...numeric}}>{formatBytes(category.bytes)}</div>
            </div>
          );
        })}
        <div style={{display: 'flex', alignItems: 'center', justifyContent: 'space-between', height: 100, opacity: fadeIn(frame, 24, 10)}}>
          <div style={{fontSize: 15, color: colors.secondary, ...numeric}}>
            {ticked} categories · {formatCount(selectedFiles)} files · <span style={{color: colors.text, fontWeight: 600}}>{formatBytes(selectedBytes)}</span> selected
          </div>
          <div style={{display: 'flex', gap: 10}}>
            <div style={{padding: '9px 18px', borderRadius: 6, border: `1px solid ${colors.stroke}`, fontSize: 14, color: colors.text}}>Preview</div>
            <div style={{padding: '9px 18px', borderRadius: 6, background: colors.accent, color: '#08121f', fontSize: 14, fontWeight: 600, transform: `scale(${cleanPressed ? 0.96 : 1})`}}>
              Clean selected
            </div>
          </div>
        </div>
      </Card>

      <Card delay={14} style={{...card, left: 1086, top: 92, width: 582, height: 820, overflow: 'hidden'}}>
        <div style={cardTitle}>Result</div>
        <div style={{position: 'absolute', inset: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 20}}>
          <div style={{position: 'absolute', fontSize: 15, color: colors.tertiary, opacity: 1 - fadeIn(frame, RESULT_AT - 10, 6)}}>Nothing deleted yet</div>
          <div
            style={{
              position: 'absolute',
              width: 600,
              height: 600,
              borderRadius: 300,
              background: `radial-gradient(closest-side, ${colors.accent}55, ${colors.accent}00)`,
              transform: `scale(${burst * 1.2})`,
              opacity: burst * (1 - interpolate(frame, [RESULT_AT + 10, RESULT_AT + 60], [0, 0.5], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'})),
            }}
          />
          <div style={{textAlign: 'center', opacity: Math.min(1, result * 1.5), transform: `scale(${0.8 + 0.2 * result})`}}>
            <div style={{fontFamily: fonts.display, fontSize: 84, fontWeight: 600, letterSpacing: '-0.03em', color: colors.text, ...numeric}}>{formatBytes(freed)}</div>
            <div style={{marginTop: 2, fontSize: 24, fontWeight: 500, color: colors.accent}}>freed</div>
            <div style={{marginTop: 26, fontSize: 14, lineHeight: 1.6, color: colors.secondary, opacity: fadeIn(frame, RESULT_AT + 30, 12), ...numeric}}>
              2,391 files deleted · 22 skipped (in use) · 5 access denied
              <br />
              Log: cleanup-20261008-094122.log
            </div>
          </div>
        </div>
      </Card>

      <div style={{position: 'absolute', inset: 0, background: 'rgba(4, 8, 14, 0.5)', opacity: Math.min(1, dialog * 1.5) * dialogOut}}>
        <div
          style={{
            position: 'absolute',
            left: dialogLeft,
            top: dialogTop,
            width: DIALOG.w,
            height: DIALOG.h,
            boxSizing: 'border-box',
            padding: 30,
            borderRadius: 10,
            background: '#1E2535',
            border: `1px solid ${colors.stroke}`,
            boxShadow: '0 40px 100px rgba(0, 0, 0, 0.6)',
            transform: `scale(${0.92 + 0.08 * dialog})`,
          }}
        >
          <div style={{fontFamily: fonts.display, fontSize: 24, fontWeight: 600, color: colors.text, ...numeric}}>Delete 2,418 files?</div>
          <div style={{marginTop: 10, fontSize: 15, lineHeight: 1.5, color: colors.secondary, ...numeric}}>14.2 GB on C: will be deleted permanently. This cannot be undone.</div>
          <div style={{marginTop: 10, fontSize: 13, color: colors.tertiary}}>Chrome is running. Files in use will be skipped.</div>
          <div style={{position: 'absolute', right: 30, bottom: 30, display: 'flex', gap: 10}}>
            <div style={{padding: '9px 18px', borderRadius: 6, border: `1px solid ${colors.stroke}`, fontSize: 14, color: colors.text}}>Cancel</div>
            <div style={{padding: '9px 18px', borderRadius: 6, background: colors.accent, color: '#08121f', fontSize: 14, fontWeight: 600, transform: `scale(${confirmPressed ? 0.95 : 1})`, ...numeric}}>
              Delete 2,418 files
            </div>
          </div>
        </div>
      </div>
    </>
  );
};
