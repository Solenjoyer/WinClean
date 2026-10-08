import React from 'react';
import {AbsoluteFill, Easing, Sequence, interpolate, useCurrentFrame, useVideoConfig} from 'remotion';
import {AppWindow, WindowRect} from './components/AppWindow';
import {Captions, CaptionSpec} from './components/Caption';
import {Taskbar, Wallpaper} from './components/Desktop';
import {PageFrame} from './components/Page';
import {Click, Pointer, PointerKey, pointerAt} from './components/Pointer';
import {Terminal} from './components/Terminal';
import {WIDGET_H, WIDGET_W, WidgetCard} from './components/Widget';
import {RESTORED, SCREEN, TASKBAR_APP_X, TASKBAR_H, WINDOW_H, captionButton, railItemCenter} from './layout';
import {rise, smooth} from './motion';
import {CHEVRON} from './scenes/Processes';
import {CLEAN_BUTTON, DELETE_BUTTON} from './scenes/Cleanup';
import {Cleanup} from './scenes/Cleanup';
import {Health} from './scenes/Health';
import {Overview} from './scenes/Overview';
import {Processes} from './scenes/Processes';
import {Storage} from './scenes/Storage';
import {PAGE_ORDER, PageKey, STAGE, pageEnd, pageStart} from './timeline';

const MAXIMIZED: WindowRect = {x: 0, y: 0, w: SCREEN.w, h: WINDOW_H};

const pages: Record<PageKey, React.FC> = {overview: Overview, processes: Processes, storage: Storage, cleanup: Cleanup, health: Health};

export const WIDGET_REST = {x: SCREEN.w - WIDGET_W - 16, y: 16};
export const WIDGET_IN = 1000;
export const TERMINAL_IN = 1012;
export const DRAG_PRESS = 1060;
export const DRAG_RELEASE = 1095;

const RAIL_X = 96;
const railY = (index: number) => railItemCenter(index).y;
const restoreButton = captionButton(MAXIMIZED, 'restore');
const minimizeButton = captionButton(RESTORED, 'minimize');
const widgetGrip = {x: WIDGET_REST.x + 150, y: WIDGET_REST.y + 14};
const widgetDrop = {x: widgetGrip.x - 190, y: widgetGrip.y + 300};

export const pointerKeys: PointerKey[] = [
  {frame: 24, x: 1460, y: 700},
  {frame: 160, x: 1460, y: 700},
  {frame: 188, x: RAIL_X, y: railY(1)},
  {frame: 250, x: RAIL_X, y: railY(1)},
  {frame: 292, x: CHEVRON.x, y: CHEVRON.y},
  {frame: 350, x: CHEVRON.x, y: CHEVRON.y},
  {frame: 398, x: RAIL_X, y: railY(2)},
  {frame: 540, x: RAIL_X, y: railY(2)},
  {frame: 578, x: RAIL_X, y: railY(3)},
  {frame: 650, x: RAIL_X, y: railY(3)},
  {frame: 680, x: CLEAN_BUTTON.x, y: CLEAN_BUTTON.y},
  {frame: 706, x: CLEAN_BUTTON.x, y: CLEAN_BUTTON.y},
  {frame: 728, x: DELETE_BUTTON.x, y: DELETE_BUTTON.y},
  {frame: 790, x: DELETE_BUTTON.x, y: DELETE_BUTTON.y},
  {frame: 818, x: RAIL_X, y: railY(4)},
  {frame: 895, x: RAIL_X, y: railY(4)},
  {frame: 930, x: restoreButton.x, y: restoreButton.y},
  {frame: 955, x: restoreButton.x, y: restoreButton.y},
  {frame: 972, x: minimizeButton.x, y: minimizeButton.y},
  {frame: 1030, x: minimizeButton.x, y: minimizeButton.y},
  {frame: 1052, x: widgetGrip.x, y: widgetGrip.y},
  {frame: DRAG_PRESS, x: widgetGrip.x, y: widgetGrip.y},
  {frame: DRAG_RELEASE, x: widgetDrop.x, y: widgetDrop.y},
  {frame: STAGE.end, x: widgetDrop.x, y: widgetDrop.y},
];

export const clicks: Click[] = [193, 305, 403, 583, 685, 735, 823, STAGE.restoreClick, STAGE.minimizeClick].map((frame) => ({frame}));

const captions: CaptionSpec[] = [
  {from: 28, to: 165, text: 'What is using your CPU, memory and disk?', accent: 'CPU, memory disk?'},
  {from: 215, to: 290, text: 'Every process, grouped by application.', accent: 'grouped by application.'},
  {from: 335, to: 395, text: 'Recognised out of the box', pills: ['Claude Code', 'Codex', 'Cursor', 'VS Code', 'Visual Studio', 'JetBrains', 'Docker', 'WSL', 'Node', 'Python', 'Git']},
  {from: 428, to: 570, text: 'What is taking up space?', accent: 'taking up space?'},
  {from: 608, to: 690, text: 'Preview. Confirm. Clean.', accent: 'Clean.'},
  {from: 775, to: 818, text: 'Every run is logged. Nothing is deleted silently.'},
  {from: 848, to: 898, text: 'Healthy and up to date?', accent: 'up to date?'},
  {from: 903, to: 960, text: 'No account. No telemetry. No cloud.'},
  {from: 1015, to: 1105, text: 'Or pin it to the desktop as a widget.', accent: 'widget.'},
];

const widgetFinal = {x: WIDGET_REST.x + (widgetDrop.x - widgetGrip.x), y: WIDGET_REST.y + (widgetDrop.y - widgetGrip.y)};
const widgetCenter = {x: widgetFinal.x + WIDGET_W / 2, y: widgetFinal.y + WIDGET_H / 2};

type CameraKey = {frame: number; scale: number; x: number; y: number};

// Gentle push-ins on what matters, back to rest before every navigation click, then a real zoom on the widget.
const cameraKeys: CameraKey[] = [
  {frame: 0, scale: 1, x: 960, y: 540},
  {frame: 40, scale: 1, x: 960, y: 540},
  {frame: 130, scale: 1.05, x: 760, y: 380},
  {frame: 188, scale: 1, x: 960, y: 540},
  {frame: 215, scale: 1, x: 960, y: 540},
  {frame: 330, scale: 1.05, x: 620, y: 460},
  {frame: 398, scale: 1, x: 960, y: 540},
  {frame: 430, scale: 1, x: 960, y: 540},
  {frame: 520, scale: 1.04, x: 760, y: 520},
  {frame: 578, scale: 1, x: 960, y: 540},
  {frame: 600, scale: 1, x: 960, y: 540},
  {frame: 700, scale: 1.02, x: 700, y: 520},
  {frame: 760, scale: 1.07, x: 1380, y: 520},
  {frame: 818, scale: 1, x: 960, y: 540},
  {frame: 845, scale: 1, x: 960, y: 540},
  {frame: 900, scale: 1.03, x: 700, y: 400},
  {frame: 928, scale: 1, x: 960, y: 540},
  {frame: 1105, scale: 1, x: 960, y: 540},
  {frame: 1170, scale: 1.75, x: widgetCenter.x, y: widgetCenter.y},
  {frame: STAGE.end, scale: 1.75, x: widgetCenter.x, y: widgetCenter.y},
];

const cameraAt = (frame: number) => {
  for (let i = 0; i < cameraKeys.length - 1; i += 1) {
    const from = cameraKeys[i];
    const to = cameraKeys[i + 1];
    if (frame >= from.frame && frame <= to.frame) {
      const t = interpolate(frame, [from.frame, to.frame], [0, 1], {easing: Easing.inOut(Easing.cubic)});
      return {scale: from.scale + (to.scale - from.scale) * t, x: from.x + (to.x - from.x) * t, y: from.y + (to.y - from.y) * t};
    }
  }
  const last = cameraKeys[cameraKeys.length - 1];
  return {scale: last.scale, x: last.x, y: last.y};
};

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

// The desktop with the WinClean window: the window opens, the pointer walks through the pages,
// the window is restored and minimised, and the widget takes over.
export const DesktopStage: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  const open = rise(frame, fps, 0, smooth);
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

  const page = [...PAGE_ORDER].reverse().find((key) => frame >= pageStart(key)) ?? 'overview';
  const camera = cameraAt(frame);
  const stageOpacity = interpolate(frame, [0, 8], [0, 1], {extrapolateRight: 'clamp'}) * interpolate(frame, [STAGE.end - 10, STAGE.end], [1, 0], {extrapolateLeft: 'clamp'});

  const grip = pointerAt(DRAG_PRESS, pointerKeys);
  const pointer = pointerAt(Math.min(frame, DRAG_RELEASE), pointerKeys);
  const dragging = frame >= DRAG_PRESS;
  const widgetPosition = dragging ? {x: WIDGET_REST.x + pointer.x - grip.x, y: WIDGET_REST.y + pointer.y - grip.y} : WIDGET_REST;

  return (
    <AbsoluteFill style={{opacity: stageOpacity}}>
      <div style={{position: 'absolute', inset: 0, transform: `scale(${camera.scale})`, transformOrigin: `${camera.x}px ${camera.y}px`}}>
        <Wallpaper />
        {frame >= TERMINAL_IN - 2 ? <Terminal frame={frame} appearAt={TERMINAL_IN} /> : null}
        <AppWindow rect={rect} opacity={windowOpacity} page={page} pageChangedAt={pageStart(page)}>
          {PAGE_ORDER.map((key) => {
            const start = pageStart(key);
            const end = pageEnd(key);
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
      <Captions frame={frame} captions={captions} />
    </AbsoluteFill>
  );
};
