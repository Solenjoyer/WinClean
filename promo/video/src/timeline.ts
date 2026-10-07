// One place for every scene boundary. The visuals and the soundtrack both read from here,
// so moving a scene keeps its sounds attached.

export const FPS = 30;

export const SCENES = {
  intro: {from: 0, duration: 105},
  monitor: {from: 105, duration: 180},
  processes: {from: 285, duration: 210},
  storage: {from: 495, duration: 180},
  cleanup: {from: 675, duration: 225},
  health: {from: 900, duration: 150},
  outro: {from: 1050, duration: 105},
} as const;

export type SceneName = keyof typeof SCENES;

export const TOTAL_FRAMES = SCENES.outro.from + SCENES.outro.duration;

export const at = (scene: SceneName, offset: number) => SCENES[scene].from + offset;
