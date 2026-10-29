import { CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { api, formatDateTime, type SimulationStatus } from '../api';
import { usePolling } from '../usePolling';
import { Card, ChartTooltip, kw, Legend, StatTile, timeAxis, valueAxis, type SeriesDef } from '../components/ui';
import { rangeStart, type RangeKey } from '../components/RangePicker';

const SERIES: SeriesDef[] = [
  { key: 'loadKw', name: 'Home load', color: 'var(--series-1)' },
  { key: 'pvKw', name: 'Solar output', color: 'var(--series-2)' },
];

export default function Overview({ status, range }: { status: SimulationStatus | null; range: RangeKey }) {
  const { data: snapshot } = usePolling(api.current);
  const from = rangeStart(range, status?.currentTime);
  const { data: history } = usePolling(() => api.history(from), [from]);
  const { data: alerts } = usePolling(api.alerts);

  const gridValue = snapshot ? (snapshot.gridImportKw > 0 ? snapshot.gridImportKw : snapshot.gridExportKw) : 0;
  const gridLabel = snapshot && snapshot.gridExportKw > 0 ? 'Grid export' : 'Grid import';

  return (
    <div className="grid">
      <div className="grid tiles">
        <StatTile label="Home load" value={snapshot ? kw(snapshot.loadKw) : '–'} unit="kW" note="average over the last 15 min" />
        <StatTile label="Solar output" value={snapshot ? kw(snapshot.pvKw) : '–'} unit="kW" />
        <StatTile
          label="Battery"
          value={snapshot ? snapshot.batterySocPercent.toFixed(0) : '–'}
          unit="%"
          note={
            snapshot
              ? snapshot.batteryChargeKw > 0
                ? `charging ${kw(snapshot.batteryChargeKw)} kW`
                : snapshot.batteryDischargeKw > 0
                  ? `discharging ${kw(snapshot.batteryDischargeKw)} kW`
                  : 'idle'
              : undefined
          }
        />
        <StatTile label={gridLabel} value={snapshot ? kw(gridValue) : '–'} unit="kW" />
      </div>

      <div className="grid two-col">
        <Card title="Home load and solar output" sub="Average power per 15-minute step, kW">
          <Legend series={SERIES} />
          <ResponsiveContainer width="100%" height={260}>
            <LineChart data={history ?? []} margin={{ top: 8, right: 8, bottom: 0, left: 0 }}>
              <CartesianGrid stroke="var(--grid)" vertical={false} />
              <XAxis {...timeAxis} />
              <YAxis {...valueAxis} />
              <Tooltip content={<ChartTooltip unit="kW" />} />
              {SERIES.map((s) => (
                <Line key={s.key} type="monotone" dataKey={s.key} name={s.name} stroke={s.color} strokeWidth={2} dot={false} isAnimationActive={false} />
              ))}
            </LineChart>
          </ResponsiveContainer>
        </Card>

        <Card title="Alerts" sub="Thresholds from appsettings.json">
          {snapshot?.activeAlerts.length ? (
            <p style={{ margin: '0 0 8px' }}>
              <strong>Active now:</strong> {snapshot.activeAlerts.map((a) => a.type).join(', ')}
            </p>
          ) : (
            <p style={{ margin: '0 0 8px', color: 'var(--text-secondary)' }}>No active alerts.</p>
          )}
          <ul className="alerts">
            {(alerts ?? []).map((a, i) => (
              <li key={`${a.timestamp}-${a.type}-${i}`}>
                <span className="icon" aria-label="alert">!</span>
                <span className="time">{formatDateTime(a.timestamp)}</span>
                <span>{a.message}</span>
              </li>
            ))}
          </ul>
        </Card>
      </div>
    </div>
  );
}
