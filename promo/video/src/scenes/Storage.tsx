import React from 'react';
import {useCurrentFrame, useVideoConfig} from 'remotion';
import {Bar} from '../components/Bar';
import {Card} from '../components/Card';
import {Glyph} from '../components/Glyph';
import {Headline} from '../components/Headline';
import {GB, formatBytes} from '../format';
import {countUp, fadeIn, lively, rise, smooth} from '../motion';
import {colors, fonts, numeric} from '../theme';

const folders = [
  {path: 'C:\\Users\\dev\\source', bytes: 128 * GB},
  {path: 'C:\\Users\\dev\\AppData\\Local', bytes: 71.4 * GB},
  {path: 'C:\\Program Files', bytes: 46.8 * GB},
  {path: 'C:\\Windows', bytes: 31.2 * GB},
  {path: 'C:\\Users\\dev\\Downloads', bytes: 18.7 * GB},
];

const developer = [
  {name: 'node_modules', count: '37 folders', bytes: 38.4 * GB, note: '12 untouched for 7 months'},
  {name: 'Docker Desktop', count: 'docker_data.vhdx', bytes: 24.1 * GB, note: ''},
  {name: 'WSL · Ubuntu', count: 'ext4.vhdx', bytes: 18.6 * GB, note: ''},
  {name: '.venv', count: '12 folders', bytes: 6.2 * GB, note: '4 untouched for 90 days'},
  {name: 'NuGet packages', count: '', bytes: 4.8 * GB, note: ''},
  {name: 'npm cache', count: '', bytes: 3.1 * GB, note: ''},
];

export const Storage: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const used = countUp(frame, 24, 50, 519 * GB);
  const total = 931 * GB;

  return (
    <>
      <Headline eyebrow="Storage" segments={[{text: 'What is'}, {text: 'taking up space?', color: colors.disk}]} delay={4} />

      <Card delay={14} style={{left: 120, top: 290, width: 1680, height: 112, padding: '24px 28px'}}>
        <div style={{display: 'flex', alignItems: 'baseline', justifyContent: 'space-between'}}>
          <div style={{fontFamily: fonts.display, fontSize: 30, fontWeight: 600, color: colors.text}}>
            C: <span style={{color: colors.secondary, fontWeight: 400}}>Windows · NVMe SSD</span>
          </div>
          <div style={{fontSize: 26, color: colors.secondary, ...numeric}}>
            {formatBytes(total - used)} free of {formatBytes(total)}
          </div>
        </div>
        <div style={{marginTop: 18}}>
          <Bar fraction={used / total} color={colors.disk} height={12} />
        </div>
      </Card>

      <Card delay={28} style={{left: 120, top: 430, width: 1000, height: 520}}>
        <div style={{fontSize: 22, fontWeight: 500, color: colors.secondary}}>Largest folders</div>
        <div style={{marginTop: 22, display: 'flex', flexDirection: 'column', gap: 20}}>
          {folders.map((folder, i) => {
            const grow = rise(frame, fps, 58 + i * 6, smooth);
            const show = fadeIn(frame, 54 + i * 6, 8);
            return (
              <div key={folder.path} style={{opacity: show, transform: `translateX(${(1 - show) * -16}px)`}}>
                <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: 23, color: colors.text, ...numeric}}>
                  <span style={{display: 'flex', alignItems: 'center', gap: 12}}>
                    <Glyph kind="folder" color={colors.tertiary} size={22} />
                    {folder.path}
                  </span>
                  <span style={{color: colors.secondary}}>{formatBytes(folder.bytes * Math.max(0.01, grow))}</span>
                </div>
                <div style={{marginTop: 10}}>
                  <Bar fraction={(folder.bytes / (128 * GB)) * grow} color="rgba(243, 245, 249, 0.55)" />
                </div>
              </div>
            );
          })}
        </div>
      </Card>

      <Card delay={36} style={{left: 1144, top: 430, width: 656, height: 520}}>
        <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center'}}>
          <div style={{fontSize: 22, fontWeight: 500, color: colors.secondary}}>Developer storage</div>
          <div style={{fontSize: 18, color: colors.disk, ...numeric, opacity: fadeIn(frame, 110, 10)}}>95.2 GB</div>
        </div>
        <div style={{marginTop: 18, display: 'flex', flexDirection: 'column'}}>
          {developer.map((item, i) => {
            const show = rise(frame, fps, 70 + i * 6, lively);
            return (
              <div
                key={item.name}
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  height: 66,
                  borderBottom: i < developer.length - 1 ? `1px solid ${colors.stroke}` : 'none',
                  opacity: Math.min(1, show * 1.5),
                  transform: `translateY(${(1 - show) * 14}px)`,
                }}
              >
                <div>
                  <div style={{fontSize: 23, color: colors.text}}>
                    {item.name}
                    {item.count ? <span style={{marginLeft: 12, fontSize: 18, color: colors.tertiary}}>{item.count}</span> : null}
                  </div>
                  {item.note ? <div style={{marginTop: 3, fontSize: 17, color: colors.caution}}>{item.note}</div> : null}
                </div>
                <div style={{fontSize: 23, color: colors.secondary, ...numeric}}>{formatBytes(item.bytes)}</div>
              </div>
            );
          })}
        </div>
      </Card>
    </>
  );
};
