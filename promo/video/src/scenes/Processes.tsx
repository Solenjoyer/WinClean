import React from 'react';
import {useCurrentFrame, useVideoConfig} from 'remotion';
import {PageHeader} from '../components/AppWindow';
import {Card} from '../components/Card';
import {Glyph} from '../components/Glyph';
import {card} from '../components/Page';
import {CONTENT} from '../layout';
import {GB, MB, formatBytes} from '../format';
import {fadeIn, lively, rise, smooth} from '../motion';
import {colors, numeric} from '../theme';

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
      {name: 'node.exe', detail: 'MCP server', cpu: 0.6, bytes: 226 * MB},
      {name: 'python.exe', detail: 'MCP server', cpu: 0.8, bytes: 142 * MB},
      {name: 'git.exe', detail: 'status --porcelain', cpu: 0.6, bytes: 21 * MB},
      {name: 'cmd.exe', detail: '', cpu: 0.2, bytes: 9.4 * MB},
    ],
  },
  {name: 'Visual Studio Code', category: 'Editor', tint: '#3B9BE0', processes: 9, cpu: 1.1, bytes: 1.02 * GB},
  {name: 'WSL virtual machine', category: 'Virtual machine', tint: '#E95420', processes: 1, cpu: 0.4, bytes: 896 * MB},
  {name: 'Node.js', category: 'Runtime', tint: '#5FA04E', processes: 2, cpu: 0.9, bytes: 612 * MB},
  {name: 'Microsoft Edge', category: 'Browser', tint: '#3AA7D8', processes: 6, cpu: 0.8, bytes: 540 * MB},
  {name: 'Windows Terminal', category: 'Terminal', tint: '#8A8A8A', processes: 2, cpu: 0.3, bytes: 148 * MB},
  {name: 'PowerShell', category: 'Shell', tint: '#5391FE', processes: 1, cpu: 0.1, bytes: 96 * MB},
];

const columns: React.CSSProperties = {display: 'grid', gridTemplateColumns: '1fr 90px 110px 200px 110px', alignItems: 'center'};
const rowHeight = 44;

export const TABLE = {x: 32, y: 140, w: 1636, h: 760};
export const EXPAND_AT = 60;
export const ROWS_FROM = 20;
export const ROW_EVERY = 5;

const firstRowY = TABLE.y + 16 + 36 + 8;
const claudeIndex = 3;

// Where the pointer must click to expand the Claude Code group, in frame pixels.
export const CHEVRON = {x: CONTENT.x + TABLE.x + 20 + 10, y: CONTENT.y + firstRowY + claudeIndex * rowHeight + rowHeight / 2};

export const Processes: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const expand = rise(frame, fps, EXPAND_AT, smooth);
  const maxBytes = groups[0].bytes;

  return (
    <>
      <PageHeader title="Processes" right="212 processes · 37 groups" />

      <div style={{position: 'absolute', left: 32, top: 86, display: 'flex', alignItems: 'center', gap: 12, opacity: fadeIn(frame, 6, 8)}}>
        <div style={{width: 320, height: 34, borderRadius: 6, border: `1px solid ${colors.stroke}`, background: 'rgba(255,255,255,0.04)', display: 'flex', alignItems: 'center', gap: 8, paddingLeft: 10, fontSize: 14, color: colors.tertiary}}>
          <Glyph kind="search" color={colors.tertiary} size={15} />
          Search
        </div>
        <Toggle label="Group by application" on />
        <Toggle label="Show system processes" on={false} />
        <div style={{marginLeft: 'auto'}} />
      </div>

      <Card delay={12} style={{...card, left: TABLE.x, top: TABLE.y, width: TABLE.w, height: TABLE.h, padding: '16px 20px'}}>
        <div style={{...columns, fontSize: 12, fontWeight: 600, letterSpacing: '0.04em', textTransform: 'uppercase', color: colors.tertiary, height: 36}}>
          <div>Name</div>
          <div style={{textAlign: 'right'}}>PID</div>
          <div style={{textAlign: 'right'}}>CPU</div>
          <div style={{textAlign: 'right'}}>Memory</div>
          <div style={{textAlign: 'right'}}>I/O</div>
        </div>
        <div style={{height: 1, background: colors.stroke, margin: '0 0 7px'}} />

        {groups.map((group, i) => {
          const show = rise(frame, fps, ROWS_FROM + i * ROW_EVERY, lively);
          const expanded = Boolean(group.children);
          const childrenHeight = expanded ? (group.children?.length ?? 0) * rowHeight * expand : 0;
          const liveCpu = group.cpu * (1 + 0.08 * Math.sin(frame / 6 + i));
          return (
            <div key={group.name} style={{opacity: Math.min(1, show * 1.5), transform: `translateX(${(1 - show) * -16}px)`}}>
              <div style={{...columns, height: rowHeight, fontSize: 15, color: colors.text}}>
                <div style={{display: 'flex', alignItems: 'center', gap: 10}}>
                  <Glyph kind="chevron" color={colors.tertiary} size={16} rotate={expanded ? 90 * expand : 0} />
                  <div style={{width: 20, height: 20, borderRadius: 5, background: group.tint, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 11, fontWeight: 700, color: '#08121f'}}>{group.name[0]}</div>
                  <span style={{fontWeight: 600}}>{group.name}</span>
                  <span style={{fontSize: 12, padding: '2px 7px', borderRadius: 5, border: `1px solid ${colors.stroke}`, color: colors.secondary}}>{group.category}</span>
                  <span style={{fontSize: 13, color: colors.tertiary}}>
                    {group.processes} {group.processes === 1 ? 'process' : 'processes'}
                  </span>
                </div>
                <div />
                <div style={{textAlign: 'right', ...numeric}}>{liveCpu.toFixed(1)}%</div>
                <div style={{display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: 10, ...numeric}}>
                  <div style={{width: 70, height: 5, borderRadius: 3, background: 'rgba(255,255,255,0.08)', overflow: 'hidden'}}>
                    <div style={{width: `${(group.bytes / maxBytes) * 100 * show}%`, height: '100%', background: colors.memory}} />
                  </div>
                  <span>{formatBytes(group.bytes)}</span>
                </div>
                <div style={{textAlign: 'right', color: colors.tertiary, ...numeric}}>{i === 3 ? '2.1 MB/s' : i === 0 ? '640 KB/s' : ''}</div>
              </div>
              {group.children ? (
                <div style={{height: childrenHeight, overflow: 'hidden'}}>
                  {group.children.map((child, j) => {
                    const childShow = fadeIn(frame, EXPAND_AT + 4 + j * 3, 8);
                    return (
                      <div key={`${child.name}-${j}`} style={{...columns, height: rowHeight, fontSize: 14, color: colors.secondary, opacity: childShow}}>
                        <div style={{display: 'flex', alignItems: 'center', gap: 10, paddingLeft: 56}}>
                          <span style={{color: colors.text}}>{child.name}</span>
                          <span style={{fontSize: 12, color: colors.tertiary, fontFamily: 'Consolas, "DejaVu Sans Mono", monospace'}}>{child.detail}</span>
                        </div>
                        <div style={{textAlign: 'right', color: colors.tertiary, ...numeric}}>{18204 + j * 412}</div>
                        <div style={{textAlign: 'right', ...numeric}}>{child.cpu.toFixed(1)}%</div>
                        <div style={{textAlign: 'right', ...numeric}}>{formatBytes(child.bytes)}</div>
                        <div style={{textAlign: 'right', color: colors.tertiary, ...numeric}}>{j === 0 ? '2.1 MB/s' : ''}</div>
                      </div>
                    );
                  })}
                </div>
              ) : null}
            </div>
          );
        })}
      </Card>
    </>
  );
};

const Toggle: React.FC<{label: string; on: boolean}> = ({label, on}) => (
  <div style={{display: 'flex', alignItems: 'center', gap: 8, fontSize: 14, color: colors.text}}>
    <div style={{width: 36, height: 18, borderRadius: 9, background: on ? colors.accent : 'transparent', border: `1px solid ${on ? colors.accent : 'rgba(255,255,255,0.4)'}`, position: 'relative'}}>
      <div style={{position: 'absolute', top: 3, left: on ? 20 : 3, width: 12, height: 12, borderRadius: 6, background: on ? '#08121f' : colors.text}} />
    </div>
    {label}
  </div>
);
