import { useMemo, useState } from 'react';
import { Area, AreaChart, Bar, BarChart, CartesianGrid, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { api, type DeviceState, type SimulationStatus } from '../api';
import { usePolling } from '../usePolling';
import { Card, ChartTooltip, kw, Legend, timeAxis, valueAxis, type SeriesDef } from '../components/ui';
import { rangeStart, type RangeKey } from '../components/RangePicker';

// Colour follows the device, never its rank.
const APPLIANCES: SeriesDef[] = [
  { key: 'fridge', name: 'Fridge', color: 'var(--series-1)' },
  { key: 'oven', name: 'Oven', color: 'var(--series-2)' },
  { key: 'kettle', name: 'Kettle', color: 'var(--series-3)' },
  { key: 'dishwasher', name: 'Dishwasher', color: 'var(--series-4)' },
  { key: 'washing-machine', name: 'Washing machine', color: 'var(--series-5)' },
];

export default function Appliances({ status, range }: { status: SimulationStatus | null; range: RangeKey }) {
  const { data: devices, refresh: refreshDevices } = usePolling(() => api.current().then((s) => s.devices));
  const from = rangeStart(range, status?.currentTime);
  const { data: readings } = usePolling(() => api.readings(from), [from]);
  const { data: kpis } = usePolling(() => (from ? api.kpis(from) : api.kpis()), [from]);

  // One row per timestamp with one column per appliance, for the stacked chart.
  const rows = useMemo(() => {
    const byTime = new Map<string, Record<string, number | string>>();
    for (const r of readings ?? []) {
      if (!APPLIANCES.some((a) => a.key === r.deviceId)) continue;
      const row = byTime.get(r.timestamp) ?? { timestamp: r.timestamp };
      row[r.deviceId] = r.powerKw;
      byTime.set(r.timestamp, row);
    }
    return [...byTime.values()];
  }, [readings]);

  const energy = APPLIANCES.map((a) => ({ ...a, kwh: kpis?.applianceKwh[a.key] ?? 0 }));

  return (
    <div className="grid">
      <Card title="Appliances" sub="Switching takes effect in the next 15-minute step. Switching on a program appliance starts one program.">
        <div className="devices">
          {(devices ?? [])
            .filter((d) => d.category === 'Consumer')
            .map((d) => (
              <DeviceCard key={d.id} device={d} onChanged={refreshDevices} />
            ))}
        </div>
      </Card>

      <div className="grid two-col">
        <Card title="Power per appliance" sub="Average power per 15-minute step, kW, stacked">
          <Legend series={APPLIANCES} />
          <ResponsiveContainer width="100%" height={280}>
            <AreaChart data={rows} margin={{ top: 8, right: 8, bottom: 0, left: 0 }}>
              <CartesianGrid stroke="var(--grid)" vertical={false} />
              <XAxis {...timeAxis} />
              <YAxis {...valueAxis} />
              <Tooltip content={<ChartTooltip unit="kW" />} />
              {APPLIANCES.map((a) => (
                <Area
                  key={a.key}
                  type="stepAfter"
                  dataKey={a.key}
                  name={a.name}
                  stackId="load"
                  stroke="var(--surface-1)"
                  strokeWidth={1}
                  fill={a.color}
                  fillOpacity={0.9}
                  isAnimationActive={false}
                />
              ))}
            </AreaChart>
          </ResponsiveContainer>
        </Card>

        <Card title="Energy per appliance" sub="kWh in the selected time range">
          <ResponsiveContainer width="100%" height={280}>
            <BarChart data={energy} layout="vertical" margin={{ top: 8, right: 48, bottom: 0, left: 8 }}>
              <CartesianGrid stroke="var(--grid)" horizontal={false} />
              <XAxis type="number" {...valueAxis} />
              <YAxis type="category" dataKey="name" width={120} tick={{ fill: 'var(--text-secondary)', fontSize: 12 }} stroke="var(--border)" />
              <Tooltip
                cursor={{ fill: 'var(--grid)' }}
                formatter={(v: number) => [`${v.toFixed(2)} kWh`, 'Energy']}
                contentStyle={{ background: 'var(--surface-1)', border: '1px solid var(--border)', borderRadius: 6 }}
                labelStyle={{ color: 'var(--text-muted)' }}
                itemStyle={{ color: 'var(--text-primary)' }}
              />
              <Bar dataKey="kwh" radius={[0, 4, 4, 0]} barSize={18} isAnimationActive={false}
                label={{ position: 'right', fill: 'var(--text-secondary)', fontSize: 11, formatter: (v: number) => v.toFixed(2) }}>
                {energy.map((e) => (
                  <Cell key={e.key} fill={e.color} />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </Card>
      </div>
    </div>
  );
}

function DeviceCard({ device, onChanged }: { device: DeviceState; onChanged: () => void }) {
  const [sent, setSent] = useState<'On' | 'Off' | null>(null);
  const send = async (action: 'On' | 'Off') => {
    await api.command(device.id, action);
    setSent(action);
    onChanged();
    window.setTimeout(() => setSent(null), 3000);
  };

  const state = !device.isEnabled ? 'off' : device.isOn ? 'on' : '';
  return (
    <div className="card device">
      <div className="row">
        <strong>{device.name}</strong>
        <span className={`badge ${state}`}>
          <span className="dot" />
          {!device.isEnabled ? 'Switched off' : device.isOn ? 'Running' : 'Idle'}
        </span>
      </div>
      <div className="row" style={{ color: 'var(--text-secondary)' }}>
        <span>{kw(device.powerKw)} kW now</span>
        <span>rated {device.ratedPowerKw} kW</span>
      </div>
      <div className="row">
        <button className="btn" onClick={() => send('On')}>{device.type === 'Fridge' ? 'Switch on' : 'Start now'}</button>
        <button className="btn" onClick={() => send('Off')} disabled={!device.isEnabled && !device.isOn}>Switch off</button>
      </div>
      {sent && <span className="badge">Command sent, applies next step</span>}
    </div>
  );
}
