import type { RecurrenceRule, GeneratedSession } from '../types';

/**
 * Pure function — no React/Mantine imports.
 * Generates a chronologically sorted list of sessions from one or more recurrence rules.
 * Rules with incomplete configuration are silently skipped.
 * There is no hard limit on the number of occurrences per rule.
 */
export function generateSessions(rules: RecurrenceRule[]): GeneratedSession[] {
  const all: GeneratedSession[] = [];

  for (const rule of rules) {
    if (rule.dayOfWeek === null || !rule.startTime || !rule.seriesStartDate) continue;
    if (rule.endCondition === 'count' && rule.occurrences < 1) continue;
    if (rule.endCondition === 'date' && !rule.endDate) continue;

    const [hourStr, minStr] = rule.startTime.split(':');
    const hours = parseInt(hourStr, 10);
    const minutes = parseInt(minStr, 10);
    if (isNaN(hours) || isNaN(minutes)) continue;

    // Parse YYYY-MM-DD string as local midnight to avoid UTC offset shifting the day
    const current = new Date(`${rule.seriesStartDate}T00:00:00`);
    if (isNaN(current.getTime())) continue;
    while (current.getDay() !== rule.dayOfWeek) {
      current.setDate(current.getDate() + 1);
    }

    let index = 0;

    if (rule.endCondition === 'count') {
      for (let i = 0; i < rule.occurrences; i++) {
        const session = new Date(current);
        session.setHours(hours, minutes, 0, 0);
        all.push({ key: `${rule.id}-${index}`, ruleId: rule.id, scheduledAt: session });
        current.setDate(current.getDate() + 7);
        index++;
      }
    } else {
      const endDate = new Date(`${rule.endDate!}T23:59:59`);
      while (current <= endDate) {
        const session = new Date(current);
        session.setHours(hours, minutes, 0, 0);
        all.push({ key: `${rule.id}-${index}`, ruleId: rule.id, scheduledAt: session });
        current.setDate(current.getDate() + 7);
        index++;
      }
    }
  }

  all.sort((a, b) => a.scheduledAt.getTime() - b.scheduledAt.getTime());
  return all;
}
