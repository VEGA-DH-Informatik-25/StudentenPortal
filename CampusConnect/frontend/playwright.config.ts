import { defineConfig, devices } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import path from 'node:path';

const frontendRoot = __dirname;
const backendRoot = path.resolve(frontendRoot, '../backend');
const e2eRoot = path.join(frontendRoot, '.playwright');
const e2eRunId = process.env['CAMPUSCONNECT_E2E_RUN_ID'] ?? `${Date.now()}-${process.pid}`;
const e2eDbPath = path.join(e2eRoot, `campusconnect-e2e-${e2eRunId}.db`);

mkdirSync(e2eRoot, { recursive: true });

export default defineConfig({
  testDir: './e2e',
  outputDir: path.join(e2eRoot, 'test-results'),
  timeout: 45_000,
  expect: {
    timeout: 10_000,
  },
  fullyParallel: false,
  reporter: process.env['CI']
    ? [
        ['github'],
        ['html', { open: 'never', outputFolder: path.join(e2eRoot, 'playwright-report') }],
      ]
    : [['list']],
  use: {
    baseURL: 'http://localhost:4300',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'tablet-portrait',
      use: {
        ...devices['iPad (gen 7)'],
        viewport: { width: 768, height: 1024 },
        isMobile: false,
        hasTouch: true,
      },
    },
    {
      name: 'tablet-landscape',
      use: {
        ...devices['iPad (gen 7) landscape'],
        viewport: { width: 1024, height: 768 },
        isMobile: false,
        hasTouch: true,
      },
    },
  ],
  webServer: [
    {
      command: 'dotnet run --project CampusConnect.API/CampusConnect.API.csproj --no-launch-profile --urls http://localhost:5136',
      cwd: backendRoot,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: 'http://localhost:5136',
        ConnectionStrings__CampusConnect: `Data Source=${e2eDbPath}`,
        DemoData__Enabled: 'true',
        Jwt__Audience: 'CampusConnect',
        Jwt__Issuer: 'CampusConnect',
        Jwt__Secret: 'CampusConnect-E2E-Secret-Key-For-Smoke-Tests-Only',
      },
      reuseExistingServer: false,
      timeout: 120_000,
      url: 'http://localhost:5136/swagger',
    },
    {
      command: 'node tools/e2e-static-server.mjs',
      cwd: frontendRoot,
      env: {
        E2E_API_BASE_URL: 'http://localhost:5136',
        E2E_FRONTEND_PORT: '4300',
      },
      reuseExistingServer: false,
      timeout: 120_000,
      url: 'http://localhost:4300/login',
    },
  ],
});
