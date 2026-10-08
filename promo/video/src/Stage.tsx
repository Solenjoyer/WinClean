import React from 'react';
import {AbsoluteFill, Easing, Sequence, interpolate, useCurrentFrame, useVideoConfig} from 'remotion';
import {AppWindow, WindowRect} from './components/AppWindow';
import {Taskbar, Wallpaper} from './components/Desktop';
import {Interstitials, InterstitialSpec} from './components/Interstitial';
import {PageFrame} from './components/Page';
import {Click, Pointer, PointerKey, pointerAt} from './components/Pointer';
import {Terminal} from './components/Terminal';
import {WIDGET_H, WIDGET_W, WidgetCard} from './components/Widget';
import {RESTORED, SCREEN, TASKBAR_APP_X, TASKBAR_H, WINDOW_H, captionButton, railItemCenter} from './layout';
import {rise, smooth} from './motion';
import {CHEVRON, EXPAND_AT} from './scenes/Processes';
import {CLEAN_BUTTON, CLEAN_CLICK, CONFIRM_CLICK, DELETE_BUTTON} from './scenes/Cleanup';
import {Cleanup} from './scenes/Cleanup';
import {Health} from './scenes/Health';
import {Overview} from './scenes/Overview';
import {Processes} from './scenes/Processes';
import {Storage} from './scenes/Storage';
import {PAGES, PAGE_ORDER, PageKey, STAGE, contentEnd} from './timeline';

const MAXIMIZED: WindowRect = {x: 0, y: 0, w: SCREEN.w, h: WINDOW_H};

const pages: Record<PageKey, React.FC> = {overview: Overview, processes: Processes, storage: Storage, cleanup: Cleanup, health: Health};

export const WIDGET_REST = {x: SCREEN.w - WIDGET_W - 16, y: 16};
export const WIDGET_IN = STAGE.widgetContent + 10;
export const TERMINAL_IN = STAGE.widgetContent + 22;
export const DRAG_PRESS = STAGE.widgetContent + 70;
export const DRAG_RELEASE = DRAG_PRESS + 35;
const ZOOM_FROM = DRAG_RELEASE + 10;
const ZOOM_TO = ZOOM_FROM + 55;

const RAIL_X = 96;
const railY = (index: number) => railItemCenter(index).y;
const restoreButton = captionButton(MAXIMIZED, 'restore');
const minimizeButton = captionButton(RESTORED, 'minimize');
const widgetGrip = {x: WIDGET_REST.x + 150, y: WIDGET_REST.y + 14};
const widgetDrop = {x: widgetGrip.x - 190, y: widgetGrip.y + 300};

const chevronClick = PAGES.processes.content + EXPAND_AT;
const cleanClick = PAGES.cleanup.content + CLEAN_CLICK;
const confirmClick = PAGES.cleanup.content + CONFIRM_CLICK;

export const pointerKeys: PointerKey[] = [
  {frame: PAGES.overview.content + 20, x: 1460, y: 700},
  {frame: PAGES.processes.click - 38, x: 1460, y: 700},
  {frame: PAGES.processes.click - 6, x: RAIL_X, y: railY(1)},
  {frame: PAGES.processes.content + 30, x: RAIL_X, y: railY(1)},
  {frame: chevronClick - 8, x: CHEVRON.x, y: CHEVRON.y},
  {frame: chevronClick + 60, x: CHEVRON.x, y: CHEVRON.y},
  {frame: PAGES.storage.click - 6, x: RAIL_X, y: railY(2)},
  {frame: PAGES.cleanup.click - 48, x: RAIL_X, y: railY(2)},
  {frame: PAGES.cleanup.click - 6, x: RAIL_X, y: railY(3)},
  {frame: cleanClick - 40, x: RAIL_X, y: railY(3)},
  {frame: cleanClick - 6, x: CLEAN_BUTTON.x, y: CLEAN_BUTTON.y},
  {frame: cleanClick + 20, x: CLEAN_BUTTON.x, y: CLEAN_BUTTON.y},
  {frame: confirmClick - 8, x: DELETE_BUTTON.x, y: DELETE_BUTTON.y},
  {frame: confirmClick + 60, x: DELETE_BUTTON.x, y: DELETE_BUTTON.y},
  {frame: PAGES.health.click - 6, x: RAIL_X, y: railY(4)},
  {frame: STAGE.restoreClick - 40, x: RAIL_X, y: railY(4)},
  {frame: STAGE.restoreClick - 6, x: restoreButton.x, y: restoreButton.y},
  {frame: STAGE.restoreClick + 20, x: restoreButton.x, y: restoreButton.y},
  {frame: STAGE.minimizeClick - 4, x: minimizeButton.x, y: minimizeButton.y},
  {frame: DRAG_PRESS - 30, x: minimizeButton.x, y: minimizeButton.y},
  {frame: DRAG_PRESS - 8, x: widgetGrip.x, y: widgetGrip.y},
  {frame: DRAG_PRESS, x: widgetGrip.x, y: widgetGrip.y},
  {frame: DRAG_RELEASE, x: widgetDrop.x, y: widgetDrop.y},
  {frame: STAGE.end, x: widgetDrop.x, y: widgetDrop.y},
];

export const clicks: Click[] = [
  PAGES.processes.click,
  chevronClick,
  PAGES.storage.click,
  PAGES.cleanup.click,
  cleanClick,
  confirmClick,
  PAGES.health.click,
  STAGE.restoreClick,
  STAGE.minimizeClick,
].map((frame) => ({frame}));

export const interstitials: InterstitialSpec[] = [
  {from: PAGES.overview.text, to: PAGES.overview.content, text: 'What is using your CPU, memory and disk?', accent: 'CPU, memory disk?'},
  {
    from: PAGES.processes.text,
    to: PAGES.processes.content,
    text: 'Every process, grouped by application.',
    accent: 'grouped by application.',
    pills: ['Claude Code', 'Codex', 'Cursor', 'VS Code', 'Visual Studio', 'JetBrains', 'Docker', 'WSL', 'Node', 'Python', 'Git'],
  },
  {from: PAGES.storage.text, to: PAGES.storage.content, text: 'What is taking up space?', accent: 'taking up space?'},
  {from: PAGES.cleanup.text, to: PAGES.cleanup.content, text: 'Preview. Confirm. Clean.', accent: 'Clean.'},
  {from: PAGES.health.text, to: PAGES.health.content, text: 'Healthy and up to date?', accent: 'up to date?', subtitle: 'No account. No telemetry. No cloud.'},
  {from: STAGE.widgetText, to: STAGE.widgetContent, text: 'Or pin it to the desktop as a widget.', accent: 'widget.'},
];

const widgetFinal = {x: WIDGET_REST.x + (widgetDrop.x - widgetGrip.x), y: WIDGET_REST.y + (widgetDrop.y - widgetGrip.y)};
const widgetCenter = {x: widgetFinal.x + WIDGET_W / 2, y: widgetFinal.y + WIDGET_H / 2};

const mix = (a: WindowRect, b: WindowRect, t: number): WindowRect => ({
  x: a.x + (b.x - a.x) * t,
  y: a.y + (b.y - a.y) * t,
  w: a.w + (b.w - a.w) * t,
  h: a.h + (b.h - a.h) * t,
});

const scaleRect = (rect: WindowRect, s: number): WindowRect => ({
  x: rect.x + (rect.w * (1 - s)) / 2,
  y: rect.y + (rect.h * (1 - s)) / 2,
  w: rect.w * s,
  h: rect.h * s,
});

// The desktop with the WinClean window: the window opens, the pointer walks through the pages with
// a centred sentence between them, the window is restored and minimised, and the widget takes over.
// The camera moves once, at the end, to bring the widget close.
export const DesktopStage: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  const open = rise(frame, fps, PAGES.overview.content - 6, smooth);
  const restore = rise(frame, fps, STAGE.restoreClick + 2, smooth);
  const minimize = rise(frame, fps, STAGE.minimizeClick + 2, smooth);
  const base = scaleRect(mix(MAXIMIZED, RESTORED, restore), 0.96 + 0.04 * open);
  const shrink = 1 - 0.92 * minimize;
  const rect: WindowRect = {
    x: base.x + (TASKBAR_APP_X - base.x) * minimize,
    y: base.y + (SCREEN.h - TASKBAR_H - base.y) * minimize,
    w: base.w * shrink,
    h: base.h * shrink,
  };
  const windowOpacity = open * (1 - minimize);

  const page = [...PAGE_ORDER].reverse().find((key) => frame >= PAGES[key].click) ?? 'overview';
  const zoom = interpolate(frame, [ZOOM_FROM, ZOOM_TO], [1, 1.6], {easing: Easing.inOut(Easing.cubic), extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  const stageOpacity = interpolate(frame, [0, 8], [0, 1], {extrapolateRight: 'clamp'}) * interpolate(frame, [STAGE.end - 10, STAGE.end], [1, 0], {extrapolateLeft: 'clamp'});

  const grip = pointerAt(DRAG_PRESS, pointerKeys);
  const pointer = pointerAt(Math.min(frame, DRAG_RELEASE), pointerKeys);
  const widgetPosition = frame >= DRAG_PRESS ? {x: WIDGET_REST.x + pointer.x - grip.x, y: WIDGET_REST.y + pointer.y - grip.y} : WIDGET_REST;

  return (
    <AbsoluteFill style={{opacity: stageOpacity}}>
      <div style={{position: 'absolute', inset: 0, transform: `scale(${zoom})`, transformOrigin: `${widgetCenter.x}px ${widgetCenter.y}px`}}>
        <Wallpaper />
        {frame >= TERMINAL_IN - 2 ? <Terminal frame={frame} appearAt={TERMINAL_IN} /> : null}
        <AppWindow rect={rect} opacity={windowOpacity} page={page} pageChangedAt={PAGES[page].click}>
          {PAGE_ORDER.map((key) => {
            const start = PAGES[key].content;
            const end = contentEnd(key);
            const Component = pages[key];
            return (
              <Sequence key={key} from={start} durationInFrames={end - start} layout="none" name={key}>
                <PageFrame duration={end - start}>
                  <Component />
                </PageFrame>
              </Sequence>
            );
          })}
        </AppWindow>
        {frame >= WIDGET_IN - 2 ? <WidgetCard frame={frame} appearAt={WIDGET_IN} x={widgetPosition.x} y={widgetPosition.y} /> : null}
        <Taskbar appRunning />
        <Pointer frame={frame} keys={pointerKeys} clicks={clicks} pressedFrom={DRAG_PRESS} pressedTo={DRAG_RELEASE} />
      </div>
      <Interstitials frame={frame} items={interstitials} />
    </AbsoluteFill>
  );
};
