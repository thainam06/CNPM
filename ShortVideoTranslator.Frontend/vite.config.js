import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'
import { readFileSync, writeFileSync } from 'node:fs'
import { resolve } from 'node:path'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
const buildId = Date.now().toString(36)
return ({
  define: { 'import.meta.env.SMARTTOUR_BUILD_ID': JSON.stringify(buildId) },
  plugins: [react(), {
    name: 'smarttour-offline-shell',
    apply: 'build',
    writeBundle(options, bundle) {
      const path = resolve(options.dir, 'sw.js')
      const assets = Object.keys(bundle).filter(name => name.startsWith('assets/')).map(name => '/' + name)
      writeFileSync(path, `self.__BUILD_ID = ${JSON.stringify(buildId)};\nself.__PRECACHE = ${JSON.stringify(assets)};\n` + readFileSync(path, 'utf8'))
    },
  }],
  server: {
    port: 5173,
    strictPort: true,
    proxy: { '/api': { target: loadEnv(mode, process.cwd(), '').API_PROXY_TARGET || 'http://localhost:5132', changeOrigin: true } },
  },
  preview: {
    port: 5173,
    strictPort: true,
    proxy: { '/api': { target: loadEnv(mode, process.cwd(), '').API_PROXY_TARGET || 'http://localhost:5132', changeOrigin: true } },
  },
})})
