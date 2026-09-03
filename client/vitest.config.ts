import { defineConfig } from 'vitest/config';
import { resolve } from 'path';

const src = resolve(__dirname, 'src');

export default defineConfig({
  root: 'src',
  build: {
    rollupOptions: {
      input: 'src/index.html',
    },
  },
  assetsInclude: ['**/*.html'],
  resolve: {
    alias: {
      '@core': resolve(src, 'app/core'),
      '@models': resolve(src, 'app/core/models'),
      '@service': resolve(src, 'app/core/services'),
      '@store': resolve(src, 'app/core/stores'),
      '@shared': resolve(src, 'app/shared'),
      '@env': resolve(src, 'environments'),
      '../../environments/environment': resolve(src, 'environments/environment'),
      '../environments/environment': resolve(src, 'environments/environment'),
      '@auth': resolve(src, 'app/features/auth'),
      '@contributions': resolve(src, 'app/features/contributions'),
      '@payments': resolve(src, 'app/features/payments'),
      '@elections': resolve(src, 'app/features/elections'),
      '@members': resolve(src, 'app/features/members'),
      '@analytics': resolve(src, 'app/features/analytics'),
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['src/test-setup.ts'],
    include: ['src/**/*.spec.ts'],
    exclude: ['src/app/app.spec.ts'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'html'],
      include: ['src/app/**/*.ts'],
      exclude: ['src/app/**/*.spec.ts', 'src/app/app.routes.ts'],
    },
  },
});
