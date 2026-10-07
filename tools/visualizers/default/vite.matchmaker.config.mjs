import path from 'node:path';
import { fileURLToPath } from 'node:url';

const visualizerDirectory = path.dirname(fileURLToPath(import.meta.url));
const environmentsRoot = process.env.KAGGRICULTURE_ENVIRONMENTS_ROOT;
const compilerOptions = JSON.stringify({
  compilerOptions: {
    target: 'ES2022',
    module: 'ESNext',
    jsx: 'automatic',
    useDefineForClassFields: true,
  },
});

if (!environmentsRoot) {
  throw new Error('Set KAGGRICULTURE_ENVIRONMENTS_ROOT to the Kaggle Environments checkout.');
}

export default {
  root: visualizerDirectory,
  resolve: {
    alias: [
      { find: '@kaggle-environments/core', replacement: path.join(environmentsRoot, 'web/core/src/index.ts') },
      { find: 'react-dom/client', replacement: path.join(environmentsRoot, 'node_modules/react-dom/client.js') },
      { find: /^react-dom$/, replacement: path.join(environmentsRoot, 'node_modules/react-dom/index.js') },
      { find: /^react$/, replacement: path.join(environmentsRoot, 'node_modules/react/index.js') },
    ],
    dedupe: ['react', 'react-dom'],
  },
  server: {
    host: '0.0.0.0',
    port: 5191,
    strictPort: true,
    cors: true,
    proxy: {
      '/api/matchmaker': {
        target: process.env.KAGGRICULTURE_MATCHMAKER_API_TARGET ?? 'http://127.0.0.1:5190',
        changeOrigin: true,
      },
    },
    fs: { allow: [visualizerDirectory, environmentsRoot] },
  },
  esbuild: { tsconfigRaw: compilerOptions },
  optimizeDeps: { esbuildOptions: { tsconfigRaw: compilerOptions } },
};
