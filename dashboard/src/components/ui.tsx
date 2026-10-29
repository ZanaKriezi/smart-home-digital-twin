import type { ReactNode } from 'react';
import { formatTime } from '../api';

export function Card({ title, sub, children, className = '' }: { title?: string; sub?: string; children: ReactNode; className?: string }) {
  return (
    <section className={`card ${className}`}>
      {title && <h2>{title}</h2>}
      {sub && <p className="sub">{sub}</p>}
      {children}
    </section>
  );
}

export function StatTile({ label, value, unit, note }: { label: string; value: string; unit?: string; note?: string }) {
  return (
    <div className="card tile">
      <div className="label">{label}</div>
      <div className="value">
        {value}
        {unit && <span className="unit">{unit}</span>}
      </div>
      {note && <div className="note">{note}</div>}
    </div>
  );
}

export interface SeriesDef {
  key: string;
  name: string;
  color: string;
}

export function Legend({ series }: { series: SeriesDef[] }) {
  return (
    <div className="legend">
      {series.map((s) => (
        <span key={s.key}>
          <i style={{ background: s.color }} />
          {s.name}
        </span>
      ))}
    </div>
  );
}

interface TooltipProps {
  active?: boolean;
  label?: string;
  payload?: { dataKey?: string | number; name?: string; value?: number; color?: string }[];
  unit: string;
  digits?: number;
}

/** Shared chart tooltip: time, then one row per series with its colour, name and value. */
export function ChartTooltip({ active, label, payload, unit, digits = 2 }: TooltipProps) {
  if (!active || !payload?.length || !label) return null;
  return (
    <div className="tooltip">
      <div className="t">{formatTime(label)}</div>
      {payload.map((p) => (
        <div className="r" key={String(p.dataKey)}>
          <span>
            <i style={{ background: p.color }} />
            {p.name}
          </span>
          <strong>
            {Number(p.value ?? 0).toFixed(digits)} {unit}
          </strong>
        </div>
      ))}
    </div>
  );
}

export const kw = (v: number) => v.toFixed(2);
export const pct = (v: number | null | undefined) => (v == null ? '–' : `${(v * 100).toFixed(0)}`);

/** Recharts axis props shared by all time-series charts. */
export const timeAxis = {
  dataKey: 'timestamp',
  tickFormatter: formatTime,
  minTickGap: 40,
  tick: { fill: 'var(--text-muted)', fontSize: 11 },
  stroke: 'var(--border)',
} as const;

export const valueAxis = {
  tick: { fill: 'var(--text-muted)', fontSize: 11 },
  stroke: 'var(--border)',
  width: 44,
} as const;
