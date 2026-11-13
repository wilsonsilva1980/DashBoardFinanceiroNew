import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import { existsSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { Agent } from 'node:https';

const certificate = fileURLToPath(new URL('../artifacts/dev/localhost.pem', import.meta.url));
export default defineConfig({
  plugins: [react()],
  server: { host: 'localhost', port: 5173, strictPort: true, proxy: { '/api': {
    target: process.env.FINANCEIRO_API_URL || 'https://localhost:7264', secure: false,
    ...(existsSync(certificate) ? { agent: new Agent({ ca: readFileSync(certificate) }) } : {}),
  } } },
  test: { include: ['tests/unit/**/*.test.ts'], environment: 'node' },
});
