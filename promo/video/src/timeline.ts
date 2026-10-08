// One place for every boundary. The visuals and the soundtrack both read from here, so moving a
// beat keeps its sounds attached. Stage frames count from the moment the desktop appears.
// Every page is introduced by a centred text (`text`) before its content is revealed (`content`);
// the pointer clicks the navigation rail two frames before the text (`click`).

export const FPS = 30;

export const INTRO = {from: 0, duration: 105} as const;

export const STAGE_FROM = 105;

export type PageKey = 'overview' | 'processes' | 'storage' | 'cleanup' | 'health';

export const PAGES: Record<PageKey, {click: number; text: number; content: number}> = {
  overview: {click: 0, text: 0, content: 50},
  processes: {click: 228, text: 230, content: 300},
  storage: {click: 478, text: 480, content: 530},
  cleanup: {click: 688, text: 690, content: 740},
  health: {click: 980, text: 982, content: 1032},
};

export const PAGE_ORDER: PageKey[] = ['overview', 'processes', 'storage', 'cleanup', 'health'];

export const STAGE = {
  restoreClick: 1140,
  minimizeClick: 1180,
  widgetText: 1210,
  widgetContent: 1260,
  end: 1440,
} as const;

export const OUTRO = {from: STAGE_FROM + STAGE.end, duration: 110} as const;

export const TOTAL_FRAMES = OUTRO.from + OUTRO.duration;

export const stage = (offset: number) => STAGE_FROM + offset;

// The page content stays mounted until the next page is clicked; the last one until the widget text.
export const contentEnd = (page: PageKey) => {
  const index = PAGE_ORDER.indexOf(page);
  return index < PAGE_ORDER.length - 1 ? PAGES[PAGE_ORDER[index + 1]].click : STAGE.widgetText;
};
