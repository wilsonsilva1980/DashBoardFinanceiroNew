import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/e2e', outputDir: '../artifacts/browser-tests', fullyParallel: false, workers: 1,
  timeout: 30_000, reporter: 'list',
  use: { baseURL: 'http://localhost:5173', ...devices['Desktop Chrome'], trace: 'retain-on-failure', screenshot: 'only-on-failure' },
  webServer: { command: 'npm run dev', url: 'http://localhost:5173', reuseExistingServer: !process.env.CI, timeout: 30_000 },
});
