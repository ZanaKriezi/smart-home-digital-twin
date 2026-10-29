export type RangeKey = '6h' | '24h' | '72h' | 'all';

export const RANGES: { key: RangeKey; label: string; hours: number | null }[] = [
  { key: '6h', label: 'Last 6 hours', hours: 6 },
  { key: '24h', label: 'Last 24 hours', hours: 24 },
  { key: '72h', label: 'Last 3 days', hours: 72 },
  { key: 'all', label: 'Whole run', hours: null },
];

/** Start of the selected range in simulated time, as an ISO string without time zone. */
export function rangeStart(range: RangeKey, currentTime: string | undefined): string | undefined {
  const hours = RANGES.find((r) => r.key === range)?.hours;
  if (!hours || !currentTime) return undefined;
  const t = new Date(`${currentTime}Z`);
  t.setUTCHours(t.getUTCHours() - hours);
  return t.toISOString().slice(0, 19);
}

export function RangePicker({ value, onChange }: { value: RangeKey; onChange: (r: RangeKey) => void }) {
  return (
    <label className="controls">
      <span style={{ color: 'var(--text-secondary)' }}>Time range</span>
      <select value={value} onChange={(e) => onChange(e.target.value as RangeKey)}>
        {RANGES.map((r) => (
          <option key={r.key} value={r.key}>
            {r.label}
          </option>
        ))}
      </select>
    </label>
  );
}
