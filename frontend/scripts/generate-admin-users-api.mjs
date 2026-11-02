import assert from 'node:assert/strict'
import { readFile, writeFile } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import openapiTS, { astToString } from 'openapi-typescript'
import { format, resolveConfig } from 'prettier'

assert.ok(process.argv[2], 'Usage: npm run generate:users-api -- <OpenAPI JSON exported from the API>')
const document = JSON.parse(await readFile(process.argv[2], 'utf8'))
const path = '/api/v1/admin/users'
const operation = document.paths?.[path]?.get
assert.equal(operation?.operationId, 'ListAdminUsers', 'The API must expose the user-directory operation')
const components = { securitySchemes: document.components?.securitySchemes }
const visited = new Set()
function collect(value) {
  if (!value || typeof value !== 'object') return
  if (value.$ref) {
    const match = /^#\/components\/([^/]+)\/([^/]+)$/.exec(value.$ref)
    assert.ok(match, `Unsupported contract reference: ${value.$ref}`)
    if (!visited.has(value.$ref)) {
      visited.add(value.$ref)
      const [, group, name] = match
      const definition = document.components?.[group]?.[name]
      assert.ok(definition, `Missing contract definition: ${value.$ref}`)
      components[group] ??= {}
      components[group][name] = definition
      collect(definition)
    }
  }
  for (const child of Object.values(value)) collect(child)
}
collect(operation)
const schema = {
  openapi: document.openapi,
  info: document.info,
  paths: { [path]: { get: operation } },
  components,
}
const target = fileURLToPath(new URL('../src/lib/api/admin-users-schema.d.ts', import.meta.url))
const generated = astToString(await openapiTS(schema))
await writeFile(target, await format(generated, { ...(await resolveConfig(target)), filepath: target }))
process.stdout.write('Generated user-directory types from the running API contract.\n')
