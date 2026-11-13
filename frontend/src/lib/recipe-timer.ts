export type TimerUnit = 'hours' | 'minutes' | 'seconds'

export const TIMER_UNIT_SECONDS: Record<TimerUnit, number> = {
  hours: 3600,
  minutes: 60,
  seconds: 1,
}

const TIMER_UNIT_LABELS: Record<TimerUnit, string> = {
  hours: 'giờ',
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

export function formatTimerDuration(timerMinutes: number): string {
  const timer = getTimerInput(timerMinutes)
  return `${timer.value} ${TIMER_UNIT_LABELS[timer.unit]}`
}
