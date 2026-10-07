// Mirrors the application's formatting: binary units with the labels Explorer uses,
// three significant digits.

const units = ['B', 'KB', 'MB', 'GB', 'TB'];

export const formatBytes = (bytes: number): string => {
  let value = Math.max(0, bytes);
  let unit = 0;
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit += 1;
  }
  const digits = value >= 100 ? 0 : value >= 10 ? 1 : 2;
  return `${value.toFixed(unit === 0 ? 0 : digits)} ${units[unit]}`;
};

export const GB = 1024 ** 3;
export const MB = 1024 ** 2;

export const formatCount = (value: number): string => Math.round(value).toLocaleString('en-US');
