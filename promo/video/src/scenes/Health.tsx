import React from 'react';
import {useCurrentFrame, useVideoConfig} from 'remotion';
import {Card} from '../components/Card';
import {Check} from '../components/Check';
import {Glyph} from '../components/Glyph';
import {Headline} from '../components/Headline';
import {fadeIn, lively, rise} from '../motion';
import {colors, fonts, numeric} from '../theme';

const facts = [
  {label: 'Edition', value: 'Windows 11 Pro'},
  {label: 'Version', value: '24H2 · Build 26100.4652'},
  {label: 'Support', value: 'Supported until 13 Oct 2026', status: colors.success},
  {label: 'Restart', value: 'Not pending', status: colors.success},
  {label: 'Last update check', value: 'Today, 09:12'},
];

const drivers = [
  {device: 'NVIDIA GeForce RTX 4070', version: '581.29', date: '3 weeks ago', source: 'nvidia.com'},
  {device: 'Intel Wi-Fi 6E AX211', version: '23.70.0.6', date: '2 months ago', source: 'intel.com'},
  {device: 'Realtek High Definition Audio', version: '6.0.9780.1', date: '5 months ago', source: 'realtek.com'},
];

const promises = ['No account', 'No telemetry', 'No cloud', 'No background service'];

export const Health: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  return (
    <>
      <Headline eyebrow="Health" segments={[{text: 'Healthy and'}, {text: 'up to date?', color: colors.success}]} delay={4} />

      <Card delay={14} style={{left: 120, top: 290, width: 1000, height: 350}}>
        <div style={{fontSize: 22, fontWeight: 500, color: colors.secondary}}>Windows</div>
        <div style={{marginTop: 16, display: 'flex', flexDirection: 'column', gap: 14}}>
          {facts.map((fact, i) => {
            const show = fadeIn(frame, 24 + i * 5, 8);
            return (
              <div key={fact.label} style={{display: 'flex', justifyContent: 'space-between', fontSize: 24, opacity: show, transform: `translateX(${(1 - show) * -12}px)`}}>
                <span style={{color: colors.secondary}}>{fact.label}</span>
                <span style={{display: 'flex', alignItems: 'center', gap: 12, color: colors.text, ...numeric}}>
                  {fact.status ? <span style={{width: 10, height: 10, borderRadius: 5, background: fact.status, boxShadow: `0 0 12px ${fact.status}`}} /> : null}
                  {fact.value}
                </span>
              </div>
            );
          })}
        </div>
      </Card>

      <Card delay={20} style={{left: 1144, top: 290, width: 656, height: 350}}>
        <div style={{fontSize: 22, fontWeight: 500, color: colors.secondary}}>Drivers</div>
        <div style={{marginTop: 14, display: 'flex', flexDirection: 'column'}}>
          {drivers.map((driver, i) => {
            const show = fadeIn(frame, 34 + i * 6, 8);
            return (
              <div
                key={driver.device}
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  height: 88,
                  borderBottom: i < drivers.length - 1 ? `1px solid ${colors.stroke}` : 'none',
                  opacity: show,
                  transform: `translateY(${(1 - show) * 10}px)`,
                }}
              >
                <div>
                  <div style={{fontSize: 22, color: colors.text}}>{driver.device}</div>
                  <div style={{marginTop: 4, fontSize: 18, color: colors.tertiary, ...numeric}}>
                    {driver.version} · {driver.date}
                  </div>
                </div>
                <div style={{display: 'flex', alignItems: 'center', gap: 8, fontSize: 19, color: colors.accent}}>
                  {driver.source}
                  <Glyph kind="link" color={colors.accent} size={18} />
                </div>
              </div>
            );
          })}
        </div>
      </Card>

      <div style={{position: 'absolute', left: 120, top: 690, display: 'flex', gap: 26}}>
        {promises.map((promise, i) => {
          const delay = 66 + i * 9;
          const show = rise(frame, fps, delay, lively);
          return (
            <div
              key={promise}
              style={{
                width: 400,
                height: 220,
                boxSizing: 'border-box',
                padding: 30,
                borderRadius: 18,
                background: colors.surface,
                border: `1px solid ${colors.stroke}`,
                display: 'flex',
                flexDirection: 'column',
                justifyContent: 'space-between',
                opacity: Math.min(1, show * 1.5),
                transform: `translateY(${(1 - show) * 40}px) scale(${0.94 + 0.06 * show})`,
              }}
            >
              <Check delay={delay + 2} size={44} round />
              <div style={{fontFamily: fonts.display, fontSize: 40, fontWeight: 600, letterSpacing: '-0.02em', color: colors.text}}>{promise}</div>
            </div>
          );
        })}
      </div>
    </>
  );
};
