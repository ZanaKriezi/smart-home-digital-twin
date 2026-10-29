import { Bar, BarChart, CartesianGrid, Line, LineChart, ReferenceLine, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { api, type SimulationStatus } from '../api';
import { usePolling } from '../usePolling';
import { Card, ChartTooltip, Legend, pct, StatTile, timeAxis, valueAxis, type SeriesDef } from '../components/ui';
import { rangeStart, type RangeKey } from '../components/RangePicker';

const GRID: SeriesDef[] = [
  { key: 'gridImportKw', name: 'Grid import', color: 'var(--series-2)' },
  { key: 'gridExport', name: 'Grid export (shown below zero)', color: 'var(--series-1)' },
];

const BATTERY: SeriesDef[] = [
  { key: 'batteryChargeKw', name: 'Charging', color: 'var(--series-3)' },
  { key: 'batteryDischarge', name: 'Discharging (shown below zero)', color: 'var(--series-5)' },
];

export default function EnergyFlow({ status, range }: { status: SimulationStatus | null; range: RangeKey }) {
  const from = rangeStart(range, status?.currentTime);
  const { data: history } = usePolling(() => api.history(from), [from]);
  const { data: kpis } = usePolling(() => (from ? api.kpis(from) : api.kpis()), [from]);

  const rows = (history ?? []).map((p) => ({ ...p, gridExport: -p.gridExportKw, batteryDischarge: -p.batteryDischargeKw }));

  return (
    <div className="grid">
      <div className="grid tiles">
        <StatTile label="Self-consumption" value={pct(kpis?.selfConsumptionRate)} unit="%" note="share of solar energy used or stored at home" />
        <StatTile label="Self-sufficiency" value={pct(kpis?.selfSufficiencyRate)} unit="%" note="share of consumption covered by own solar" />
        <StatTile label="Grid import" value={kpis ? kpis.gridImportKwh.toFixed(1) : '–'} unit="kWh" />
        <StatTile label="Grid export" value={kpis ? kpis.gridExportKwh.toFixed(1) : '–'} unit="kWh" />
      </div>

      <div className="grid two-col">
        <Card title="Battery state of charge" sub="%, minimum reserve 10 %">
          <ResponsiveContainer width="100%" height={240}>
            <LineChart data={rows} margin={{ top: 8, right: 8, bottom: 0, left: 0 }}>
              <CartesianGrid stroke="var(--grid)" vertical={false} />
              <XAxis {...timeAxis} />
              <YAxis {...valueAxis} domain={[0, 100]} />
              <ReferenceLine y={10} stroke="var(--text-muted)" strokeDasharray="4 4" />
              <Tooltip content={<ChartTooltip unit="%" digits={0} />} />
              <Line type="monotone" dataKey="batterySocPercent" name="State of charge" stroke="var(--series-1)" strokeWidth={2} dot={false} isAnimationActive={false} />
            </LineChart>
          </ResponsiveContainer>
        </Card>

        <Card title="Battery charging and discharging" sub="kW per 15-minute step">
          <Legend series={BATTERY} />
          <SignedBars rows={rows} series={BATTERY} />
        </Card>

        <Card title="Grid exchange" sub="kW per 15-minute step">
          <Legend series={GRID} />
          <SignedBars rows={rows} series={GRID} />
        </Card>

        <Card title="Indicators" sub="Selected time range">
          <table className="kpi-table">
            <tbody>
              <tr><td>Consumption</td><td>{kpis?.loadKwh.toFixed(2) ?? '–'} kWh</td></tr>
              <tr><td>Solar production</td><td>{kpis?.pvKwh.toFixed(2) ?? '–'} kWh</td></tr>
              <tr><td>Battery charged / discharged</td><td>{kpis ? `${kpis.batteryChargeKwh.toFixed(2)} / ${kpis.batteryDischargeKwh.toFixed(2)}` : '–'} kWh</td></tr>
              <tr><td>Peak load</td><td>{kpis?.peakLoadKw.toFixed(2) ?? '–'} kW</td></tr>
              <tr><td>Peak grid import</td><td>{kpis?.peakGridImportKw.toFixed(2) ?? '–'} kW</td></tr>
              <tr><td>Steps</td><td>{kpis?.steps ?? '–'}</td></tr>
            </tbody>
          </table>
        </Card>
      </div>
    </div>
  );
}

function SignedBars({ rows, series }: { rows: Record<string, unknown>[]; series: SeriesDef[] }) {
  return (
    <ResponsiveContainer width="100%" height={220}>
      <BarChart data={rows} stackOffset="sign" barCategoryGap={1} margin={{ top: 8, right: 8, bottom: 0, left: 0 }}>
        <CartesianGrid stroke="var(--grid)" vertical={false} />
        <XAxis {...timeAxis} />
        <YAxis {...valueAxis} />
        <ReferenceLine y={0} stroke="var(--text-muted)" />
        <Tooltip cursor={{ fill: 'var(--grid)' }} content={<AbsTooltip />} />
        {series.map((s) => (
          <Bar key={s.key} dataKey={s.key} name={s.name} stackId="flow" fill={s.color} isAnimationActive={false} />
        ))}
      </BarChart>
    </ResponsiveContainer>
  );
}

// Values below zero are drawn negative but reported as positive amounts.
function AbsTooltip(props: { active?: boolean; label?: string; payload?: { dataKey?: string | number; name?: string; value?: number; color?: string }[] }) {
  const payload = props.payload?.map((p) => ({ ...p, value: Math.abs(p.value ?? 0), name: p.name?.replace(' (shown below zero)', '') }));
  return <ChartTooltip {...props} payload={payload} unit="kW" />;
}
