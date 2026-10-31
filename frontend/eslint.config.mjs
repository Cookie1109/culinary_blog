import js from '@eslint/js'
import jsxA11y from 'eslint-plugin-jsx-a11y'
import react from 'eslint-plugin-react'
import reactHooks from 'eslint-plugin-react-hooks'
import globals from 'globals'
import tseslint from 'typescript-eslint'
import { FlatCompat } from '@eslint/eslintrc'
import { fixupConfigRules, fixupPluginRules } from '@eslint/compat'
import nextPlugin from '@next/eslint-plugin-next'
import prettier from 'eslint-config-prettier'
import { fileURLToPath } from 'node:url'

const compat = new FlatCompat({ baseDirectory: fileURLToPath(new URL('.', import.meta.url)) })

const eslintConfiguration = tseslint.config(
  {
    ignores: ['.next/**', 'node_modules/**', 'next-env.d.ts', 'src/lib/api/schema.d.ts'],
  },
  js.configs.recommended,
  ...fixupConfigRules(compat.extends('airbnb')),
  ...tseslint.configs.recommended,
  {
    files: ['**/*.{js,jsx,mjs,cjs,ts,tsx}'],
    languageOptions: {
      globals: {
        ...globals.browser,
        ...globals.node,
        ...globals.es2022,
      },
      parserOptions: {
        ecmaFeatures: { jsx: true },
      },
    },
    plugins: {
      'jsx-a11y': fixupPluginRules(jsxA11y),
      react: fixupPluginRules(react),
      'react-hooks': fixupPluginRules(reactHooks),
      '@next/next': fixupPluginRules(nextPlugin),
    },
    settings: {
      react: { version: 'detect' },
      'import/resolver': { typescript: { project: './tsconfig.json' } },
      'import/core-modules': ['server-only'],
    },
    rules: {
      ...jsxA11y.flatConfigs.recommended.rules,
      'jsx-a11y/label-has-associated-control': ['error', { assert: 'either', depth: 5 }],
      ...react.configs.recommended.rules,
      ...react.configs['jsx-runtime'].rules,
      ...reactHooks.configs.flat.recommended.rules,
      ...nextPlugin.configs.recommended.rules,
      // Next route handlers and this repository's public modules use named exports.
      'import/prefer-default-export': 'off',
      'import/no-extraneous-dependencies': [
        'error',
        { devDependencies: ['**/*.config.*', 'scripts/**', 'tests/**', 'e2e/**'] },
      ],
      // Node22/Next15 support native iteration; retain the unsafe-syntax checks.
      'no-restricted-syntax': ['error', 'ForInStatement', 'LabeledStatement', 'WithStatement'],
      'no-console': ['error', { allow: ['warn', 'error'] }],
      // RHF bindings and typed link wrappers deliberately forward their validated props.
      'react/jsx-props-no-spreading': [
        'error',
        {
          exceptions: [
            'input',
            'textarea',
            'select',
            'button',
            'NextLink',
            'QueryNavLink',
            'PersonalOverview',
          ],
        },
      ],
      // Match the former Next 15 lint baseline. Enabling these newer rules requires
      // separate behavior-changing UI refactors outside the security remediation.
      'react-hooks/refs': 'off',
      'react-hooks/set-state-in-effect': 'off',
    },
  },
  {
    files: ['scripts/**', 'tests/**', 'e2e/**'],
    rules: {
      // Browser journeys, fixtures and cleanup intentionally await each step in order.
      'no-await-in-loop': 'off',
    },
  },
  {
    files: ['**/*.mjs'],
    rules: {
      // Node ESM package subpaths require their published file extensions.
      'import/extensions': ['error', 'ignorePackages', { js: 'always', mjs: 'always' }],
    },
  },
  {
    files: ['tests/**'],
    rules: {
      'no-use-before-define': ['error', { functions: false }],
      'import/extensions': [
        'error',
        'ignorePackages',
        { js: 'always', mjs: 'always', jsx: 'never', ts: 'never', tsx: 'never' },
      ],
    },
  },
  {
    files: ['src/components/ui/button.tsx'],
    rules: {
      // This typed wrapper defaults to button; callers may explicitly request submit/reset.
      'react/button-has-type': 'off',
    },
  },
  {
    files: ['**/*.{ts,tsx}'],
    rules: {
      'import/extensions': [
        'error',
        'ignorePackages',
        { ts: 'never', tsx: 'never', js: 'never', jsx: 'never' },
      ],
      'react/jsx-filename-extension': ['error', { extensions: ['.jsx', '.tsx'] }],
      'react/require-default-props': ['error', { functions: 'defaultArguments' }],
      'no-shadow': 'off',
      '@typescript-eslint/no-shadow': 'error',
      'no-use-before-define': 'off',
      '@typescript-eslint/no-use-before-define': ['error', { functions: false }],
    },
  },
  {
    files: ['src/features/culinary/**/*.tsx'],
    rules: {
      'react/no-unescaped-entities': 'off',
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
    },
  },
  prettier,
)

export default eslintConfiguration
