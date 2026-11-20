import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'
import vm from 'node:vm'
import ts from 'typescript'

const source = readFileSync(new URL('../src/lib/recipe-timer.ts', import.meta.url), 'utf8')
const code = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS } }).outputText
const loaded = { exports: {} }
vm.runInNewContext(code, { module: loaded, exports: loaded.exports, Math })
const { formatMinutes, MAX_STEP_TIMER_MINUTES } = loaded.exports

test('recipe times are shown as hours and minutes without leftover seconds', () => {
  assert.equal(formatMinutes(0), '0 phút')
  assert.equal(formatMinutes(45), '45 phút')
  assert.equal(formatMinutes(60), '1 tiếng')
  assert.equal(formatMinutes(63), '1 tiếng 3 phút')
  assert.equal(formatMinutes(63.03), '1 tiếng 3 phút')
  assert.equal(formatMinutes(150), '2 tiếng 30 phút')
})

test('a single step timer is limited to 24 hours', () => {
  assert.equal(MAX_STEP_TIMER_MINUTES, 1440)
})
