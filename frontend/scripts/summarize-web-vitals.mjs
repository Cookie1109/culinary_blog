import { readFileSync } from 'node:fs'
import { pathToFileURL } from 'node:url'
import { parseWebVital, vitalNames } from '../src/lib/web-vitals.ts'

export function summarizeWebVitals(records, { build, from, to }) {
  if (
    !build ||
    !Number.isFinite(Date.parse(from)) ||
    !Number.isFinite(Date.parse(to)) ||
    Date.parse(from) >= Date.parse(to)
  ) {
    throw new Error('Expected build and a valid increasing --from/--to UTC window are required.')
  }
  const latest = new Map()
  let excluded = 0
  for (const record of records.filter((entry) => entry.event === 'web-vital')) {
    const { build: actualBuild, receivedAt } = record
    const sample = Object.fromEntries(
      Object.entries(record).filter(([key]) => !['event', 'build', 'receivedAt'].includes(key)),
    )
    const parsed = parseWebVital(sample)
    if (!parsed || !Number.isFinite(Date.parse(receivedAt))) throw new Error('Invalid web-vital record.')
    if (
      actualBuild !== build ||
      Date.parse(receivedAt) < Date.parse(from) ||
      Date.parse(receivedAt) > Date.parse(to)
    ) {
      excluded += 1
    } else {
      const key = `${sample.name}:${sample.id}`
      const previous = latest.get(key)
      if (!previous || Date.parse(receivedAt) >= Date.parse(previous.receivedAt)) latest.set(key, record)
    }
  }
  const groups = new Map()
  for (const sample of latest.values()) {
    const key = `${sample.route}:${sample.device}:${sample.name}`
    if (!groups.has(key))
      groups.set(key, { route: sample.route, device: sample.device, name: sample.name, values: [] })
    groups.get(key).values.push(sample.value)
  }
  const thresholds = { LCP: 2500, CLS: 0.1, INP: 200 }
  return {
    build,
    from,
    to,
    distinctMetrics: latest.size,
    excluded,
    missingMetrics: vitalNames.filter((name) => ![...latest.values()].some((sample) => sample.name === name)),
    acceptance:
      'Pending verification of deployment identity and sample adequacy; descriptive p75 alone does not close D9-03.',
    groups: [...groups.values()].map(({ values, ...group }) => {
      values.sort((a, b) => a - b)
      const p75 = values[Math.ceil(values.length * 0.75) - 1]
      return {
        ...group,
        samples: values.length,
        p75,
        threshold: thresholds[group.name],
        withinThreshold: p75 <= thresholds[group.name],
      }
    }),
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const [, , file, build, from, to] = process.argv
  const records = readFileSync(file, 'utf8')
    .split(/\r?\n/)
    .filter(Boolean)
    .map((line) => JSON.parse(line))
  process.stdout.write(`${JSON.stringify(summarizeWebVitals(records, { build, from, to }), null, 2)}\n`)
}
