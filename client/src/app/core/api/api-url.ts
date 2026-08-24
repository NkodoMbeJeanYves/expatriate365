import { inject } from '@angular/core';
import { APP_CONFIG } from '@core/config/app-config.token';

export function apiUrl(path: string): string {
  const config = inject(APP_CONFIG);
  return `${config.apiUrl}/api/v1${path}`;
}
