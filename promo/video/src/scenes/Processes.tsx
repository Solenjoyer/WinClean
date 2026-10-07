import React from 'react';
import {interpolate, useCurrentFrame, useVideoConfig} from 'remotion';
import {Card} from '../components/Card';
import {Glyph} from '../components/Glyph';
import {Headline} from '../components/Headline';
import {GB, MB, formatBytes} from '../format';
import {fadeIn, lively, rise, smooth} from '../motion';
import {colors, fonts, numeric} from '../theme';

type Group = {
  name: string;
  category: string;
  tint: string;
  processes: number;
  cpu: number;
  bytes: number;
  children?: {name: string; detail: string; cpu: number; bytes: number}[];
};

const groups: Group[] = [
  {name: 'Docker Desktop', category: 'Container', tint: '#2496ED', processes: 6, cpu: 1.3, bytes: 3.02 * GB},
  {name: 'Cursor', category: 'IDE', tint: '#7C7CF0', processes: 7, cpu: 4.2, bytes: 2.41 * GB},
  {name: 'Google Chrome', category: 'Browser', tint: '#E8B339', processes: 14, cpu: 2.8, bytes: 1.92 * GB},
  {
    name: 'Claude Code',
    category: 'AI agent',
    tint: '#D97757',
    processes: 5,
    cpu: 11.8,
    bytes: 1.18 * GB,
    children: [
      {name: 'node.exe', detail: '@anthropic-ai\\claude-code\\cli.js', cpu: 9.6, bytes: 812 * MB},
      {name: 'python.exe', detail: 'MCP server', cpu: 1.4, bytes: 142 * MB},
      {name: 'git.exe', detail: 'status --porcelain', cpu: 0.6, bytes: 21 * MB},
      {name: 'cmd.exe', detail: '', cpu: 0.2, bytes: 9.4 * MB},
    ],
  },
  {name: 'Visual Studio Code', category: 'Editor', tint: '#3B9BE0', processes: 9, cpu: 1.1, bytes: 1.02 * GB},
  {name: 'WSL virtual machine', category: 'Virtual machine', tint: '#E95420', processes: 1, cpu: 0.4, bytes: 896 * MB},
  {name: 'Node.js', category: 'Runtime', tint: '#5FA04E', processes: 2, cpu: 0.9, bytes: 612 * MB},
];

const recognised = ['Claude Code', 'Codex', 'Cursor', 'VS Code', 'Visual Studio', 'JetBrains', 'Docker', 'WSL', 'Node', 'Python', 'Git'];

const columns: React.CSSProperties = {display: 'grid', gridTemplateColumns: '1fr 150px 100px 130px 230px', alignItems: 'center'};
const rowHeight = 46;
const expandAt = 80;

export const Processes: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const expand = rise(frame, fps, expandAt, smooth);
  const maxBytes = groups[0].bytes;

  let rowIndex = 0;

  return (
    <>
      <Headline
        eyebrow="Processes"
        segments={[{text: 'Every process,'}, {text: 'grouped by application.', color: colors.accent}]}
        delay={4}
      />

      <Card delay={18} style={{left: 120, top: 300, width: 1680, height: 586, padding: '20px 28px'}}>
        <div style={{...columns, fontSize: 17, fontWeight: 600, letterSpacing: '0.04em', textTransform: 'uppercase', color: colors.tertiary, height: 36}}>
          <div>Name</div>
          <div>Status</div>
          <div style={{textAlign: 'right'}}>PID</div>
          <div style={{textAlign: 'right'}}>CPU</div>
          <div style={{textAlign: 'right'}}>Memory</div>
        </div>
        <div style={{height: 1, background: colors.stroke, margin: '4px 0 6px'}} />

        {groups.map((group, i) => {
          const delay = 30 + i * 5;
          const show = rise(frame, fps, delay, lively);
          const expanded = Boolean(group.children);
          const childrenHeight = expanded ? (group.children?.length ?? 0) * rowHeight * expand : 0;
          const index = rowIndex;
          rowIndex += 1;
          const liveCpu = group.cpu * (1 + 0.08 * Math.sin(frame / 6 + i));
          return (
            <div key={group.name} style={{opacity: Math.min(1, show * 1.5), transform: `translateX(${(1 - show) * -24}px)`}}>
              <div style={{...columns, height: rowHeight, fontSize: 22, color: colors.text}}>
                <div style={{display: 'flex', alignItems: 'center', gap: 14}}>
                  <Glyph kind="chevron" color={colors.tertiary} size={20} rotate={expanded ? 90 * expand : 0} />
                  <div
                    style={{
                      width: 26,
                      height: 26,
                      borderRadius: 7,
                      background: group.tint,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      fontSize: 15,
                      fontWeight: 700,
                      color: '#08121f',
                    }}
                  >
                    {group.name[0]}
                  </div>
                  <span style={{fontWeight: 600}}>{group.name}</span>
                  <span style={{fontSize: 16, padding: '3px 9px', borderRadius: 6, border: `1px solid ${colors.stroke}`, color: colors.secondary}}>
                    {group.category}
                  </span>
                  <span style={{fontSize: 18, color: colors.tertiary}}>
                    {group.processes} {group.processes === 1 ? 'process' : 'processes'}
                  </span>
                </div>
                <div style={{fontSize: 18, color: colors.tertiary}}>{group.name === 'Google Chrome' ? '' : ''}</div>
                <div style={{textAlign: 'right', color: colors.tertiary, ...numeric}}>{index === 0 ? '' : ''}</div>
                <div style={{textAlign: 'right', ...numeric}}>{liveCpu.toFixed(1)}%</div>
                <div style={{display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: 14, ...numeric}}>
                  <div style={{width: 90, height: 6, borderRadius: 3, background: 'rgba(255,255,255,0.08)', overflow: 'hidden'}}>
                    <div style={{width: `${(group.bytes / maxBytes) * 100 * show}%`, height: '100%', background: colors.memory}} />
                  </div>
                  <span>{formatBytes(group.bytes)}</span>
                </div>
              </div>
              {group.children ? (
                <div style={{height: childrenHeight, overflow: 'hidden'}}>
                  {group.children.map((child, j) => {
                    const childShow = fadeIn(frame, expandAt + 4 + j * 3, 8);
                    return (
                      <div key={child.name} style={{...columns, height: rowHeight, fontSize: 20, color: colors.secondary, opacity: childShow}}>
                        <div style={{display: 'flex', alignItems: 'center', gap: 14, paddingLeft: 66}}>
                          <span style={{color: colors.text}}>{child.name}</span>
                          <span style={{fontSize: 17, color: colors.tertiary, fontFamily: 'Consolas, "DejaVu Sans Mono", monospace'}}>{child.detail}</span>
                        </div>
                        <div />
                        <div style={{textAlign: 'right', color: colors.tertiary, ...numeric}}>{18204 + j * 412}</div>
                        <div style={{textAlign: 'right', ...numeric}}>{child.cpu.toFixed(1)}%</div>
                        <div style={{textAlign: 'right', ...numeric}}>{formatBytes(child.bytes)}</div>
                      </div>
                    );
                  })}
                </div>
              ) : null}
            </div>
          );
        })}
      </Card>

      <div style={{position: 'absolute', left: 120, top: 924, display: 'flex', alignItems: 'center', gap: 12}}>
        <span
          style={{
            fontFamily: fonts.text,
            fontSize: 21,
            color: colors.secondary,
            marginRight: 10,
            opacity: fadeIn(frame, 120, 10),
          }}
        >
          Recognised out of the box
        </span>
        {recognised.map((name, i) => {
          const show = rise(frame, fps, 128 + i * 4, lively);
          return (
            <span
              key={name}
              style={{
                padding: '9px 16px',
                borderRadius: 999,
                border: `1px solid ${colors.stroke}`,
                background: colors.surfaceRaised,
                fontSize: 21,
                fontWeight: 500,
                color: colors.text,
                opacity: Math.min(1, show * 1.5),
                transform: `scale(${0.6 + 0.4 * show})`,
              }}
            >
              {name}
            </span>
          );
        })}
      </div>

      <div style={{position: 'absolute', right: 120, top: 100, fontSize: 21, color: colors.tertiary, opacity: fadeIn(frame, 40, 12)}}>
        {`Updated ${interpolate(frame, [0, 210], [0, 7], {extrapolateRight: 'clamp'}).toFixed(0)} s ago`}
      </div>
    </>
  );
};
