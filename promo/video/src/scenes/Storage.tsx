import React from 'react';
import {useCurrentFrame, useVideoConfig} from 'remotion';
import {PageHeader} from '../components/AppWindow';
import {Bar} from '../components/Bar';
import {Card} from '../components/Card';
import {Glyph} from '../components/Glyph';
import {card, cardTitle} from '../components/Page';
import {GB, formatBytes} from '../format';
import {countUp, fadeIn, lively, rise, smooth} from '../motion';
import {colors, fonts, numeric} from '../theme';

const folders = [
  {name: 'Users\\dev\\source', bytes: 128 * GB, files: '412,800 files'},
  {name: 'Users\\dev\\AppData\\Local', bytes: 71.4 * GB, files: '398,120 files'},
  {name: 'Program Files', bytes: 46.8 * GB, files: '186,400 files'},
  {name: 'Windows', bytes: 31.2 * GB, files: '164,900 files'},
  {name: 'Users\\dev\\Downloads', bytes: 18.7 * GB, files: '1,204 files'},
  {name: 'ProgramData', bytes: 12.3 * GB, files: '48,200 files'},
  {name: 'Users\\dev\\Videos', bytes: 9.6 * GB, files: '86 files'},
];

const developer = [
  {name: 'node_modules', count: '37 folders', bytes: 38.4 * GB, note: '12 untouched for 7 months'},
  {name: 'Docker Desktop', count: 'docker_data.vhdx', bytes: 24.1 * GB, note: ''},
  {name: 'WSL · Ubuntu', count: 'ext4.vhdx', bytes: 18.6 * GB, note: ''},
  {name: '.venv', count: '12 folders', bytes: 6.2 * GB, note: '4 untouched for 90 days'},
  {name: 'NuGet packages', count: '', bytes: 4.8 * GB, note: ''},
  {name: 'npm cache', count: '', bytes: 3.1 * GB, note: ''},
];

export const RISER_AT = 20;
export const BARS_FROM = 60;
export const BAR_EVERY = 6;
export const DEV_FROM = 70;

export const Storage: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const used = countUp(frame, 10, 45, 519 * GB);

  return (
    <>
      <PageHeader title="Storage" right="C: scanned 2 min ago · 1,284,204 files" />

      {[
        {letter: 'C:', label: 'Windows · NVMe SSD', total: 931 * GB, usedBytes: used},
        {letter: 'D:', label: 'Projects · SATA SSD', total: 1863 * GB, usedBytes: 653 * GB * countUp(frame, 14, 45, 1)},
      ].map((drive, i) => (
        <Card key={drive.letter} delay={6 + i * 4} style={{...card, left: 32 + i * 440, top: 86, width: 420, height: 112}}>
          <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'baseline'}}>
            <div style={{fontFamily: fonts.display, fontSize: 18, fontWeight: 600, color: colors.text}}>
              {drive.letter} <span style={{fontSize: 14, fontWeight: 400, color: colors.secondary}}>{drive.label}</span>
            </div>
            <div style={{fontSize: 14, color: colors.secondary, ...numeric}}>
              {formatBytes(drive.total - drive.usedBytes)} free of {formatBytes(drive.total)}
            </div>
          </div>
          <div style={{marginTop: 14}}>
            <Bar fraction={drive.usedBytes / drive.total} color={i === 0 ? colors.disk : 'rgba(243, 245, 249, 0.55)'} height={8} />
          </div>
          <div style={{position: 'absolute', right: 20, bottom: 14, fontSize: 13, color: colors.accent}}>Scan</div>
        </Card>
      ))}

      <Card delay={16} style={{...card, left: 32, top: 220, width: 1000, height: 740}}>
        <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center'}}>
          <div style={cardTitle}>Largest folders</div>
          <div style={{fontSize: 13, color: colors.tertiary, ...numeric}}>C:\ · 519 GB used</div>
        </div>
        <div style={{marginTop: 16, display: 'flex', flexDirection: 'column', gap: 14}}>
          {folders.map((folder, i) => {
            const grow = rise(frame, fps, BARS_FROM + i * BAR_EVERY, smooth);
            const show = fadeIn(frame, BARS_FROM - 4 + i * BAR_EVERY, 8);
            return (
              <div key={folder.name} style={{opacity: show, transform: `translateX(${(1 - show) * -12}px)`}}>
                <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: 15, color: colors.text, ...numeric}}>
                  <span style={{display: 'flex', alignItems: 'center', gap: 10}}>
                    <Glyph kind="folder" color={colors.tertiary} size={16} />
                    {folder.name}
                    <span style={{fontSize: 12, color: colors.tertiary}}>{folder.files}</span>
                  </span>
                  <span style={{display: 'flex', alignItems: 'center', gap: 10, color: colors.secondary}}>
                    {formatBytes(folder.bytes * Math.max(0.01, grow))}
                    <Glyph kind="chevron" color={colors.tertiary} size={14} />
                  </span>
                </div>
                <div style={{marginTop: 7}}>
                  <Bar fraction={(folder.bytes / (128 * GB)) * grow} color="rgba(243, 245, 249, 0.5)" height={6} />
                </div>
              </div>
            );
          })}
        </div>
      </Card>

      <div style={{position: 'absolute', left: 1052, top: 220, display: 'flex', gap: 4, opacity: fadeIn(frame, 22, 8)}}>
        {['Largest files', 'File types', 'Applications', 'Developer storage'].map((tab, i) => (
          <div key={tab} style={{padding: '8px 14px', borderRadius: 6, fontSize: 14, color: i === 3 ? colors.text : colors.secondary, fontWeight: i === 3 ? 600 : 400, background: i === 3 ? 'rgba(255,255,255,0.07)' : 'transparent'}}>
            {tab}
          </div>
        ))}
      </div>

      <Card delay={26} style={{...card, left: 1052, top: 266, width: 616, height: 694}}>
        <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center'}}>
          <div style={cardTitle}>Developer storage</div>
          <div style={{fontSize: 14, color: colors.disk, ...numeric, opacity: fadeIn(frame, DEV_FROM + 40, 10)}}>95.2 GB</div>
        </div>
        <div style={{marginTop: 8}}>
          {developer.map((item, i) => {
            const show = rise(frame, fps, DEV_FROM + i * 6, lively);
            return (
              <div key={item.name} style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center', height: 64, borderBottom: i < developer.length - 1 ? `1px solid ${colors.stroke}` : 'none', opacity: Math.min(1, show * 1.5), transform: `translateY(${(1 - show) * 10}px)`}}>
                <div>
                  <div style={{fontSize: 15, color: colors.text}}>
                    {item.name}
                    {item.count ? <span style={{marginLeft: 8, fontSize: 12, color: colors.tertiary}}>{item.count}</span> : null}
                  </div>
                  {item.note ? <div style={{marginTop: 2, fontSize: 12, color: colors.caution}}>{item.note}</div> : null}
                </div>
                <div style={{display: 'flex', alignItems: 'center', gap: 14, fontSize: 15, color: colors.secondary, ...numeric}}>
                  {formatBytes(item.bytes)}
                  <span style={{fontSize: 12, color: colors.accent}}>Add to cleanup</span>
                </div>
              </div>
            );
          })}
        </div>
        <div style={{position: 'absolute', left: 20, bottom: 18, fontSize: 13, color: colors.tertiary}}>Marker files decide: package.json next to node_modules, pyvenv.cfg inside .venv.</div>
      </Card>
    </>
  );
};
