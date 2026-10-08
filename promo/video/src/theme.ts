// The palette follows the application's dark theme: Fluent surfaces, the Windows accent,
// and one colour per metric series.

export const colors = {
  background: '#0A0E16',
  surface: 'rgba(255, 255, 255, 0.045)',
  surfaceRaised: 'rgba(255, 255, 255, 0.07)',
  stroke: 'rgba(255, 255, 255, 0.09)',
  text: '#F3F5F9',
  secondary: 'rgba(243, 245, 249, 0.62)',
  tertiary: 'rgba(243, 245, 249, 0.36)',
  accent: '#4CC2FF',
  accentDeep: '#1F5FA8',
  cpu: '#6EB4F0',
  memory: '#B19CF2',
  disk: '#67C98D',
  gpu: '#F0A060',
  network: '#5CC9DB',
  success: '#6CCB5F',
  caution: '#F7C948',
  critical: '#FF99A4',
  // The application window, tinted like Mica over the wallpaper.
  chrome: '#151A25',
  rail: '#171D29',
  content: '#1C2232',
  taskbar: 'rgba(22, 27, 38, 0.9)',
};

export const fonts = {
  display: '"Inter Display", "Inter", "Segoe UI Variable Display", "Segoe UI", sans-serif',
  text: '"Inter", "Segoe UI Variable Text", "Segoe UI", sans-serif',
};

export const numeric: React.CSSProperties = {
  fontVariantNumeric: 'tabular-nums',
  fontFeatureSettings: '"tnum" 1, "cv11" 1',
};
