export type TimerUnit = 'hours' | 'minutes' | 'seconds'

export const TIMER_UNIT_SECONDS: Record<TimerUnit, number> = {
  hours: 3600,
  minutes: 60,
  seconds: 1,
}

const TIMER_UNIT_LABELS: Record<TimerUnit, string> = {
  hours: 'tiếng',
  minutes: 'phút',
  seconds: 'giây',
}

export function getTimerInput(timerMinutes: number | null): { value: number | null; unit: TimerUnit } {
  if (timerMinutes === null) return { value: null, unit: 'minutes' }
  const seconds = Math.round(timerMinutes * 60)
  if (seconds > 0 && seconds % 3600 === 0) return { value: seconds / 3600, unit: 'hours' }
  if (seconds % 60 === 0) return { value: seconds / 60, unit: 'minutes' }
  return { value: seconds, unit: 'seconds' }
}

/** Formats a step timer as hours, minutes and seconds, omitting empty parts. */
export function formatTimerDuration(timerMinutes: number): string {
  const total = Math.max(0, Math.round(timerMinutes * 60))
  const parts = [
    [Math.floor(total / 3600), TIMER_UNIT_LABELS.hours],
    [Math.floor((total % 3600) / 60), TIMER_UNIT_LABELS.minutes],
    [total % 60, TIMER_UNIT_LABELS.seconds],
  ] as const
  const text = parts
    .filter(([value]) => value > 0)
    .map(([value, label]) => `${value} ${label}`)
    .join(' ')
  return text || `0 ${TIMER_UNIT_LABELS.minutes}`
}

/** The longest timer a single step may carry (24 hours). */
export const MAX_STEP_TIMER_MINUTES = 24 * 60

/** Formats whole minutes as hours and minutes, dropping any remainder. */
export function formatMinutes(totalMinutes: number): string {
  const minutes = Math.floor(Math.max(0, totalMinutes))
  const hours = Math.floor(minutes / 60)
  const rest = minutes % 60
  if (hours === 0) return `${rest} phút`
  return rest === 0 ? `${hours} tiếng` : `${hours} tiếng ${rest} phút`
}
