import React from 'react';
import {interpolate, useCurrentFrame, useVideoConfig} from 'remotion';
import {Bar} from '../components/Bar';
import {Card} from '../components/Card';
import {Glyph} from '../components/Glyph';
import {Headline} from '../components/Headline';
import {Sparkline} from '../components/Sparkline';
import {GB, MB, formatBytes} from '../format';
import {countUp, fadeIn, rise, seeded, series, smooth} from '../motion';
import {colors, fonts, numeric} from '../theme';

type Metric = {
  key: 'cpu' | 'memory' | 'disk' | 'network';
  title: string;
  color: string;
  values: number[];
  value: (t: number) => string;
  caption: string;
};

const metrics: Metric[] = [
  {
    key: 'cpu',
    title: 'CPU',
    color: colors.cpu,
    values: series(11, 60, 0.26, 0.18),
    value: (t) => `${Math.round(23 * t)}%`,
    caption: '4.52 GHz · 12 cores',
  },
  {
    key: 'memory',
    title: 'Memory',
    color: colors.memory,
    values: series(23, 60, 0.46, 0.05, 0),
    value: (t) => `${formatBytes(14.2 * GB * t)}`,
    caption: '17.8 GB available of 32.0 GB',
  },
  {
    key: 'disk',
    title: 'Disk',
    color: colors.disk,
    values: series(37, 60, 0.18, 0.2, 0.12),
    value: (t) => `${Math.round(9 * t)}%`,
    caption: 'C: 412 GB free · 48.2 MB/s',
  },
  {
    key: 'network',
    title: 'Network',
    color: colors.network,
    values: series(41, 60, 0.3, 0.22, 0.1),
    value: (t) => `${formatBytes(12.4 * MB * t)}/s`,
    caption: 'Wi-Fi · 1.8 MB/s up',
  },
];

const cores = Array.from({length: 12}, (_, i) => {
  const random = seeded(100 + i);
  return {base: 0.12 + random() * 0.35, phase: random() * 6, speed: 5 + random() * 5};
});

const topApplications = [
  {name: 'Docker Desktop', bytes: 3.02 * GB},
  {name: 'Cursor', bytes: 2.41 * GB},
  {name: 'Google Chrome', bytes: 1.92 * GB},
];

export const Monitor: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  return (
    <>
      <Headline
        eyebrow="Monitor"
        segments={[
          {text: 'What is using your'},
          {text: 'CPU,', color: colors.cpu},
          {text: 'memory', color: colors.memory},
          {text: 'and'},
          {text: 'disk?', color: colors.disk},
        ]}
        delay={6}
      />

      {metrics.map((metric, i) => {
        const delay = 30 + i * 6;
        const reveal = interpolate(frame, [delay + 6, delay + 70], [0.05, 1], {
          extrapolateLeft: 'clamp',
          extrapolateRight: 'clamp',
        });
        const count = countUp(frame, delay + 6, 45, 1);
        return (
          <Card key={metric.key} delay={delay} style={{left: 120 + i * 414, top: 330, width: 390, height: 240}}>
            <div style={{display: 'flex', alignItems: 'center', gap: 10, color: colors.secondary, fontSize: 22, fontWeight: 500}}>
              <Glyph kind={metric.key} color={metric.color} size={24} />
              {metric.title}
            </div>
            <div
              style={{
                marginTop: 10,
                fontFamily: fonts.display,
                fontSize: 62,
                fontWeight: 600,
                letterSpacing: '-0.02em',
                color: colors.text,
                ...numeric,
              }}
            >
              {metric.value(count)}
            </div>
            <div style={{position: 'absolute', left: 28, right: 28, top: 140}}>
              <Sparkline id={metric.key} values={metric.values} progress={reveal} color={metric.color} width={334} height={48} />
            </div>
            <div style={{position: 'absolute', left: 28, bottom: 20, fontSize: 19, color: colors.tertiary, ...numeric}}>
              {metric.caption}
            </div>
          </Card>
        );
      })}

      <Card delay={64} style={{left: 120, top: 600, width: 1056, height: 330}}>
        <div style={{fontSize: 22, fontWeight: 500, color: colors.secondary}}>CPU per core</div>
        <div style={{position: 'absolute', left: 28, right: 28, bottom: 28, top: 84, display: 'flex', alignItems: 'flex-end', gap: 14}}>
          {cores.map((core, i) => {
            const grow = rise(frame, fps, 76 + i * 2, smooth);
            const live = core.base + 0.22 * (0.5 + 0.5 * Math.sin(frame / core.speed + core.phase));
            const height = Math.max(0.04, live) * grow;
            return (
              <div key={i} style={{flex: 1, height: '100%', display: 'flex', flexDirection: 'column', justifyContent: 'flex-end', gap: 10}}>
                <div
                  style={{
                    height: `${height * 100}%`,
                    borderRadius: 6,
                    background: `linear-gradient(180deg, ${colors.cpu}, ${colors.cpu}66)`,
                    boxShadow: `0 0 20px ${colors.cpu}40`,
                  }}
                />
                <div style={{textAlign: 'center', fontSize: 16, color: colors.tertiary, ...numeric}}>{i}</div>
              </div>
            );
          })}
        </div>
      </Card>

      <Card delay={70} style={{left: 1200, top: 600, width: 600, height: 330}}>
        <div style={{fontSize: 22, fontWeight: 500, color: colors.secondary}}>Top applications by memory</div>
        <div style={{marginTop: 24, display: 'flex', flexDirection: 'column', gap: 22}}>
          {topApplications.map((app, i) => {
            const show = fadeIn(frame, 84 + i * 6, 10);
            const grow = rise(frame, fps, 84 + i * 6, smooth);
            return (
              <div key={app.name} style={{opacity: show, transform: `translateX(${(1 - show) * 20}px)`}}>
                <div style={{display: 'flex', justifyContent: 'space-between', fontSize: 23, color: colors.text, ...numeric}}>
                  <span>{app.name}</span>
                  <span style={{color: colors.secondary}}>{formatBytes(app.bytes)}</span>
                </div>
                <div style={{marginTop: 10}}>
                  <Bar fraction={(app.bytes / (3.4 * GB)) * grow} color={colors.memory} />
                </div>
              </div>
            );
          })}
        </div>
      </Card>
    </>
  );
};
