import assert from 'node:assert/strict'
import test from 'node:test'
import { command } from './helpers/isolated-app.mjs'

test('structured command output excludes successful image pull diagnostics', async () => {
  const output = await command(process.execPath, [
    '-e',
    `process.stderr.write('Pulling image layer\\n'); process.stdout.write(JSON.stringify({ Id: 'synthetic-container' }))`,
  ])
  assert.deepEqual(JSON.parse(output), { Id: 'synthetic-container' })
})

test('failed commands retain both output streams in their diagnostics', async () => {
  await assert.rejects(
    command(process.execPath, [
      '-e',
      `process.stdout.write('partial output'); process.stderr.write('registry failure'); process.exitCode = 7`,
    ]),
    (error) => {
      assert.match(error.message, /partial output/)
      assert.match(error.message, /registry failure/)
      return true
    },
  )
})
