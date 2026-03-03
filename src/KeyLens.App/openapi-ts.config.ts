import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
    input: '../KeyLens.Api/openapi-v1.json',
    output: './src/api',
    plugins: [
        {
            name: '@hey-api/client-axios',
            runtimeConfigPath: '../utils/axios-config.ts',
        },
        '@tanstack/react-query'
    ],
});
