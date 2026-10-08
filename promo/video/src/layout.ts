// Geometry of the fake desktop and the maximised application window, in frame pixels.

export const SCREEN = {w: 1920, h: 1080};
export const TASKBAR_H = 48;
export const TITLE_H = 40;
export const RAIL_W = 220;
export const CONTENT = {x: RAIL_W, y: TITLE_H, w: SCREEN.w - RAIL_W, h: SCREEN.h - TASKBAR_H - TITLE_H};
export const PAD = 32;
export const WINDOW_H = SCREEN.h - TASKBAR_H;

export const RESTORED = {x: 210, y: 100, w: 1500, h: Math.round(WINDOW_H * (1500 / SCREEN.w))};

export const CAPTION_W = 46;

export const railItemCenter = (index: number) => ({x: 108, y: TITLE_H + 24 + index * 44 + 18});

// Pointer tip positions of the caption buttons for a window at the given rectangle.
export const captionButton = (rect: {x: number; y: number; w: number}, button: 'minimize' | 'restore' | 'close') => {
  const scale = rect.w / SCREEN.w;
  const slot = button === 'close' ? 0.5 : button === 'restore' ? 1.5 : 2.5;
  return {x: rect.x + rect.w - CAPTION_W * scale * slot, y: rect.y + (TITLE_H / 2) * scale};
};

export const TASKBAR_APP_X = 1058;
