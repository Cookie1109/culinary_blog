import assert from 'node:assert/strict'
import test from 'node:test'
import { ESLint } from 'eslint'

test('Airbnb, Hooks, accessibility and Next rules reject controlled failures', async () => {
  const eslint = new ESLint()
  const cases = [
    ['eqeqeq', 'export const compare = (value: number) => value == 1'],
    ['no-eval', 'export const evaluate = (value: string) => eval(value)'],
    ['no-script-url', 'export const target = "javascript:alert(1)"'],
    [
      'react-hooks/rules-of-hooks',
      'import { useState } from "react"; export function Probe({ active }: { active: boolean }) { if (active) useState(0); return null }',
    ],
    [
      'jsx-a11y/label-has-associated-control',
      'export function Probe() { return <label>Missing control</label> }',
    ],
    [
      '@next/next/no-img-element',
      'export function Probe() { return <img src="/image.png" alt="Fixture" /> }',
    ],
  ]
  for (const [rule, code] of cases) {
    const [result] = await eslint.lintText(code, { filePath: 'src/quality-probe.tsx' })
    assert.ok(
      result.messages.some((message) => message.ruleId === rule),
      `${rule} must remain active`,
    )
  }
})
