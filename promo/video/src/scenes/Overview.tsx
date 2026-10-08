import React from 'react';
import {interpolate, useCurrentFrame, useVideoConfig} from 'remotion';
import {PageHeader} from '../components/AppWindow';
import {Bar} from '../components/Bar';
import {Card} from '../components/Card';
import {Glyph, GlyphKind} from '../components/Glyph';
import {card, cardTitle, liveSlice} from '../components/Page';
import {Sparkline} from '../components/Sparkline';
import {GB, MB, formatBytes} from '../format';
import {countUp, fadeIn, rise, seeded, series, smooth} from '../motion';
import {colors, fonts, numeric} from '../theme';

type Metric = {key: GlyphKind; title: string; color: string; values: number[]; value: (t: number) => string; caption: string};

const metrics: Metric[] = [
  {key: 'cpu', title: 'CPU', color: colors.cpu, values: series(11, 240, 0.26, 0.18), value: (t) => `${Math.round(23 * t)}%`, caption: '4.52 GHz · 12 cores'},
  {key: 'memory', title: 'Memory', color: colors.memory, values: series(23, 240, 0.46, 0.05, 0), value: (t) => formatBytes(14.2 * GB * t), caption: '17.8 GB available of 32.0 GB'},
  {key: 'disk', title: 'Disk', color: colors.disk, values: series(37, 240, 0.18, 0.2, 0.12), value: (t) => `${Math.round(9 * t)}%`, caption: 'C: read 41.2 MB/s · write 7.0 MB/s'},
  {key: 'network', title: 'Network', color: colors.network, values: series(41, 240, 0.3, 0.22, 0.1), value: (t) => `${formatBytes(12.4 * MB * t)}/s`, caption: 'Down 10.6 MB/s · up 1.8 MB/s'},
];

const cores = Array.from({length: 12}, (_, i) => {
  const random = seeded(100 + i);
  return {base: 0.12 + random() * 0.35, phase: random() * 6, speed: 5 + random() * 5};
});

const topApplications = [
  {name: 'Docker Desktop', category: 'Container', bytes: 3.02 * GB},
  {name: 'Cursor', category: 'IDE', bytes: 2.41 * GB},
  {name: 'Google Chrome', category: 'Browser', bytes: 1.92 * GB},
  {name: 'Claude Code', category: 'AI agent', bytes: 1.18 * GB},
  {name: 'Visual Studio Code', category: 'Editor', bytes: 1.02 * GB},
];

const volumes = [
  {letter: 'C:', label: 'Windows', free: 412 * GB, total: 931 * GB},
  {letter: 'D:', label: 'Projects', free: 1210 * GB, total: 1863 * GB},
];

const Line: React.FC<{label: string; value: string; status?: string; delay: number}> = ({label, value, status, delay}) => {
  const frame = useCurrentFrame();
  const show = fadeIn(frame, delay, 8);
  return (
    <div style={{display: 'flex', justifyContent: 'space-between', fontSize: 15, height: 30, alignItems: 'center', opacity: show}}>
      <span style={{color: colors.secondary}}>{label}</span>
      <span style={{display: 'flex', alignItems: 'center', gap: 8, color: colors.text, ...numeric}}>
        {status ? <span style={{width: 8, height: 8, borderRadius: 4, background: status}} /> : null}
        {value}
      </span>
    </div>
  );
};

export const Overview: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  return (
    <>
      <PageHeader title="Overview" right={`Updated ${1 + (Math.floor(frame / 30) % 2)} s ago`} />

      {metrics.map((metric, i) => {
        const delay = 8 + i * 5;
        const reveal = interpolate(frame, [delay + 4, delay + 60], [0.05, 1], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
        const count = countUp(frame, delay + 4, 40, 1);
        return (
          <Card key={metric.key} delay={delay} style={{...card, left: 32 + i * 414, top: 92, width: 394, height: 212}}>
            <div style={{display: 'flex', alignItems: 'center', gap: 8, ...cardTitle}}>
              <Glyph kind={metric.key} color={metric.color} size={16} />
              {metric.title}
            </div>
            <div style={{marginTop: 6, fontFamily: fonts.display, fontSize: 40, fontWeight: 600, letterSpacing: '-0.02em', color: colors.text, ...numeric}}>{metric.value(count)}</div>
            <div style={{position: 'absolute', left: 20, right: 20, top: 104}}>
              <Sparkline id={`ov-${metric.key}`} values={liveSlice(metric.values, frame, delay + 60)} progress={reveal} color={metric.color} width={354} height={40} />
            </div>
            <div style={{position: 'absolute', left: 20, bottom: 14, fontSize: 13, color: colors.tertiary, ...numeric}}>{metric.caption}</div>
          </Card>
        );
      })}

      <Card delay={30} style={{...card, left: 32, top: 324, width: 1062, height: 300}}>
        <div style={cardTitle}>CPU per core</div>
        <div style={{position: 'absolute', left: 20, right: 20, top: 52, bottom: 20, display: 'flex', alignItems: 'flex-end', gap: 12}}>
          {cores.map((core, i) => {
            const grow = rise(frame, fps, 40 + i * 2, smooth);
            const live = core.base + 0.22 * (0.5 + 0.5 * Math.sin(frame / core.speed + core.phase));
            return (
              <div key={i} style={{flex: 1, height: '100%', display: 'flex', flexDirection: 'column', justifyContent: 'flex-end', gap: 8}}>
                <div style={{height: `${Math.max(0.04, live) * grow * 100}%`, borderRadius: 4, background: `linear-gradient(180deg, ${colors.cpu}, ${colors.cpu}66)`}} />
                <div style={{textAlign: 'center', fontSize: 12, color: colors.tertiary, ...numeric}}>{i}</div>
              </div>
            );
          })}
        </div>
      </Card>

      <Card delay={36} style={{...card, left: 1114, top: 324, width: 554, height: 300}}>
        <div style={cardTitle}>Top applications by memory</div>
        <div style={{marginTop: 12, display: 'flex', flexDirection: 'column', gap: 11}}>
          {topApplications.map((app, i) => {
            const show = fadeIn(frame, 46 + i * 5, 8);
            const grow = rise(frame, fps, 46 + i * 5, smooth);
            return (
              <div key={app.name} style={{opacity: show}}>
                <div style={{display: 'flex', justifyContent: 'space-between', fontSize: 15, color: colors.text, ...numeric}}>
                  <span>
                    {app.name}
                    <span style={{marginLeft: 8, fontSize: 12, color: colors.tertiary}}>{app.category}</span>
                  </span>
                  <span style={{color: colors.secondary}}>{formatBytes(app.bytes)}</span>
                </div>
                <div style={{marginTop: 5}}>
                  <Bar fraction={(app.bytes / (3.4 * GB)) * grow} color={colors.memory} height={6} />
                </div>
              </div>
            );
          })}
        </div>
      </Card>

      <Card delay={44} style={{...card, left: 32, top: 644, width: 520, height: 316}}>
        <div style={cardTitle}>Volumes</div>
        <div style={{marginTop: 14, display: 'flex', flexDirection: 'column', gap: 18}}>
          {volumes.map((volume, i) => {
            const grow = rise(frame, fps, 54 + i * 6, smooth);
            return (
              <div key={volume.letter}>
                <div style={{display: 'flex', justifyContent: 'space-between', fontSize: 15, color: colors.text, ...numeric}}>
                  <span>
                    <strong>{volume.letter}</strong> <span style={{color: colors.secondary}}>{volume.label}</span>
                  </span>
                  <span style={{color: colors.secondary}}>
                    {formatBytes(volume.free)} free of {formatBytes(volume.total)}
                  </span>
                </div>
                <div style={{marginTop: 8}}>
                  <Bar fraction={((volume.total - volume.free) / volume.total) * grow} color="rgba(243, 245, 249, 0.55)" height={8} />
                </div>
              </div>
            );
          })}
        </div>
        <div style={{position: 'absolute', left: 20, bottom: 18, fontSize: 14, color: colors.tertiary, opacity: fadeIn(frame, 70, 10)}}>Cleanup potential 14.2 GB · analysed 2 h ago</div>
      </Card>

      <Card delay={50} style={{...card, left: 572, top: 644, width: 522, height: 316}}>
        <div style={cardTitle}>Health</div>
        <div style={{marginTop: 10}}>
          <Line label="Windows 11 Pro" value="25H2 · 26200.6584" delay={60} />
          <Line label="Support" value="Until 12 Oct 2027" status={colors.success} delay={65} />
          <Line label="Restart" value="Not pending" status={colors.success} delay={70} />
          <Line label="Last update check" value="Today, 09:12" delay={75} />
          <Line label="Secure Boot" value="On · UEFI · TPM 2.0" status={colors.success} delay={80} />
        </div>
      </Card>

      <Card delay={56} style={{...card, left: 1114, top: 644, width: 554, height: 316}}>
        <div style={cardTitle}>System</div>
        <div style={{marginTop: 10}}>
          <Line label="Uptime" value="2 days 5 hours" delay={66} />
          <Line label="Processes" value="212 · 2,840 threads · 96,120 handles" delay={71} />
          <Line label="Commit charge" value="18.4 GB of 48.0 GB" delay={76} />
          <Line label="GPU" value="3% · 1.2 GB dedicated" delay={81} />
          <Line label="Temperatures" value="Not available · sensors off" delay={86} />
        </div>
      </Card>
    </>
  );
};
