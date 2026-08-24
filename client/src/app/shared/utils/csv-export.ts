export function toCsv<T extends Record<string, unknown>>(rows: T[], headers?: Partial<Record<keyof T, string>>): string {
  if (!rows.length) return '';
  const keys = Object.keys(rows[0]) as (keyof T)[];
  const headerRow = keys.map((k) => (headers?.[k] ?? String(k))).join(',');
  const dataRows = rows.map((row) =>
    keys.map((k) => {
      const val = row[k];
      const str = val == null ? '' : String(val);
      return str.includes(',') || str.includes('"') ? `"${str.replace(/"/g, '""')}"` : str;
    }).join(',')
  );
  return [headerRow, ...dataRows].join('\n');
}

export function downloadCsv(csv: string, filename: string): void {
  const BOM = '﻿';
  const blob = new Blob([BOM + csv], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}
