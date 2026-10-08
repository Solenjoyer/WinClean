// One place for every boundary. The visuals and the soundtrack both read from here, so moving a
// beat keeps its sounds attached. Stage frames count from the moment the desktop appears.

export const FPS = 30;

export const INTRO = {from: 0, duration: 105} as const;

export const STAGE_FROM = 105;

export const STAGE = {
  overview: 0,
  processes: 195,
  storage: 405,
  cleanup: 585,
  health: 825,
  restoreClick: 935,
  minimizeClick: 975,
  widget: 975,
  end: 1185,
} as const;

export const OUTRO = {from: STAGE_FROM + STAGE.end, duration: 110} as const;

export const TOTAL_FRAMES = OUTRO.from + OUTRO.duration;

export const stage = (offset: number) => STAGE_FROM + offset;

export type PageKey = 'overview' | 'processes' | 'storage' | 'cleanup' | 'health';

export const PAGE_ORDER: PageKey[] = ['overview', 'processes', 'storage', 'cleanup', 'health'];

export const pageStart = (page: PageKey) => STAGE[page];

export const pageEnd = (page: PageKey) => {
  const index = PAGE_ORDER.indexOf(page);
  return index < PAGE_ORDER.length - 1 ? STAGE[PAGE_ORDER[index + 1]] : STAGE.widget;
};
