import React from 'react';
import {useCurrentFrame} from 'remotion';
import {PageHeader} from '../components/AppWindow';
import {Card} from '../components/Card';
import {Glyph} from '../components/Glyph';
import {card, cardTitle} from '../components/Page';
import {fadeIn} from '../motion';
import {colors, fonts, numeric} from '../theme';

const drivers = [
  {device: 'NVIDIA GeForce RTX 4070', provider: 'NVIDIA', version: '581.29', date: '15 Sep 2026 · 3 weeks ago', source: 'nvidia.com'},
  {device: 'Intel Wi-Fi 6E AX211', provider: 'Intel', version: '23.70.0.6', date: '2 Aug 2026 · 2 months ago', source: 'intel.com'},
  {device: 'Realtek High Definition Audio', provider: 'Realtek', version: '6.0.9780.1', date: '6 May 2026 · 5 months ago', source: 'realtek.com'},
  {device: 'Samsung NVMe Controller', provider: 'Samsung', version: '3.3.0.2003', date: '11 Mar 2026 · 7 months ago', source: 'samsung.com'},
  {device: 'Intel Ethernet Controller I225-V', provider: 'Intel', version: '2.1.4.3', date: '20 Feb 2026 · 8 months ago', source: 'intel.com'},
  {device: 'Intel Chipset SATA/PCIe RST', provider: 'Intel', version: '20.2.0.1026', date: '9 Jun 2026 · 4 months ago', source: 'intel.com'},
];

export const DRIVERS_FROM = 36;
export const DRIVER_EVERY = 5;

const Line: React.FC<{label: string; value: string; status?: string; delay: number}> = ({label, value, status, delay}) => {
  const frame = useCurrentFrame();
  const show = fadeIn(frame, delay, 8);
  return (
    <div style={{display: 'flex', justifyContent: 'space-between', fontSize: 15, height: 32, alignItems: 'center', opacity: show, transform: `translateX(${(1 - show) * -8}px)`}}>
      <span style={{color: colors.secondary}}>{label}</span>
      <span style={{display: 'flex', alignItems: 'center', gap: 8, color: colors.text, ...numeric}}>
        {status ? <span style={{width: 8, height: 8, borderRadius: 4, background: status, boxShadow: `0 0 8px ${status}`}} /> : null}
        {value}
      </span>
    </div>
  );
};

export const Health: React.FC = () => {
  const frame = useCurrentFrame();

  return (
    <>
      <PageHeader title="Health" right="Support dates from data bundled with WinClean 0.1.0, updated 29 Sep 2026" />

      <Card delay={8} style={{...card, left: 32, top: 92, width: 540, height: 304}}>
        <div style={{display: 'flex', alignItems: 'center', gap: 12}}>
          <Glyph kind="windows" color={colors.accent} size={26} />
          <div style={{fontFamily: fonts.display, fontSize: 20, fontWeight: 600, color: colors.text}}>Windows 11 Pro</div>
        </div>
        <div style={{marginTop: 12}}>
          <Line label="Version" value="25H2" delay={16} />
          <Line label="Build" value="26200.6584" delay={20} />
          <Line label="Installed" value="14 Jun 2026" delay={24} />
          <Line label="Support" value="Until 12 Oct 2027" status={colors.success} delay={28} />
          <Line label="Edition type" value="Client, Pro" delay={32} />
        </div>
      </Card>

      <Card delay={14} style={{...card, left: 592, top: 92, width: 540, height: 304}}>
        <div style={cardTitle}>Updates</div>
        <div style={{marginTop: 10}}>
          <Line label="Last check" value="Today, 09:12" delay={22} />
          <Line label="Last install" value="1 Oct 2026 · KB5066835" delay={26} />
          <Line label="Pending restart" value="No" status={colors.success} delay={30} />
        </div>
        <div style={{position: 'absolute', left: 20, bottom: 18, display: 'flex', alignItems: 'center', gap: 12, opacity: fadeIn(frame, 36, 8)}}>
          <div style={{padding: '8px 16px', borderRadius: 6, border: `1px solid ${colors.stroke}`, fontSize: 14, color: colors.text}}>Check now</div>
          <span style={{fontSize: 12, color: colors.tertiary}}>Contacts Microsoft Update.</span>
        </div>
      </Card>

      <Card delay={20} style={{...card, left: 1152, top: 92, width: 516, height: 304}}>
        <div style={cardTitle}>System</div>
        <div style={{marginTop: 10}}>
          <Line label="System drive" value="412 GB free of 931 GB" delay={28} />
          <Line label="Last restart" value="2 days ago" delay={32} />
          <Line label="Firmware" value="UEFI" delay={36} />
          <Line label="Secure Boot" value="On" status={colors.success} delay={40} />
          <Line label="TPM" value="2.0" status={colors.success} delay={44} />
        </div>
      </Card>

      <Card delay={26} style={{...card, left: 32, top: 420, width: 1636, height: 540}}>
        <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center'}}>
          <div style={cardTitle}>Drivers</div>
          <div style={{display: 'flex', gap: 6}}>
            {['Display', 'Network', 'Audio', 'Storage', 'Chipset'].map((group) => (
              <span key={group} style={{padding: '4px 10px', borderRadius: 999, border: `1px solid ${colors.stroke}`, fontSize: 12, color: colors.secondary}}>
                {group}
              </span>
            ))}
          </div>
        </div>
        <div style={{marginTop: 14, display: 'grid', gridTemplateColumns: '1fr 150px 150px 260px 160px', fontSize: 12, fontWeight: 600, letterSpacing: '0.04em', textTransform: 'uppercase', color: colors.tertiary, height: 30, alignItems: 'center'}}>
          <div>Device</div>
          <div>Provider</div>
          <div>Version</div>
          <div>Date</div>
          <div>Source</div>
        </div>
        <div style={{height: 1, background: colors.stroke}} />
        {drivers.map((driver, i) => {
          const show = fadeIn(frame, DRIVERS_FROM + i * DRIVER_EVERY, 8);
          return (
            <div key={driver.device} style={{display: 'grid', gridTemplateColumns: '1fr 150px 150px 260px 160px', alignItems: 'center', height: 60, borderBottom: i < drivers.length - 1 ? `1px solid ${colors.stroke}` : 'none', fontSize: 15, color: colors.text, opacity: show, transform: `translateY(${(1 - show) * 8}px)`}}>
              <div>{driver.device}</div>
              <div style={{color: colors.secondary}}>{driver.provider}</div>
              <div style={{...numeric}}>{driver.version}</div>
              <div style={{color: colors.secondary, ...numeric}}>{driver.date}</div>
              <div style={{display: 'flex', alignItems: 'center', gap: 6, color: colors.accent}}>
                {driver.source}
                <Glyph kind="link" color={colors.accent} size={14} />
              </div>
            </div>
          );
        })}
      </Card>
    </>
  );
};
