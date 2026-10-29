import { useState } from 'react';
import { api, formatDateTime } from './api';
import { usePolling } from './usePolling';
import { RangePicker, type RangeKey } from './components/RangePicker';
import Overview from './pages/Overview';
import Appliances from './pages/Appliances';
import EnergyFlow from './pages/EnergyFlow';
import Scenarios from './pages/Scenarios';

type Page = 'overview' | 'appliances' | 'energy' | 'scenarios';

const PAGES: { key: Page; label: string }[] = [
  { key: 'overview', label: 'Overview' },
  { key: 'appliances', label: 'Kitchen appliances' },
  { key: 'energy', label: 'Energy flow' },
  { key: 'scenarios', label: 'Scenarios' },
];

// Real seconds per simulated 15-minute step.
const SPEEDS = [
  { value: 15, label: '1 step / 15 s' },
  { value: 5, label: '1 step / 5 s' },
  { value: 2, label: '1 step / 2 s (1 day ≈ 3 min)' },
  { value: 0.5, label: '1 step / 0.5 s' },
  { value: 0.05, label: 'Fastest' },
];

export default function App() {
  const [page, setPage] = useState<Page>('overview');
  const [range, setRange] = useState<RangeKey>('24h');
  const { data: status, error, refresh } = usePolling(api.simulation);

  const act = async (action: () => Promise<unknown>) => {
    await action();
    await refresh();
  };

  return (
    <div className="app">
      <header className="header">
        <h1>Smart Home Digital Twin</h1>
        {status && (
          <span className="clock">
            Simulated time {formatDateTime(status.currentTime)} · scenario {status.scenarioId ?? '–'} ·{' '}
            {status.isRunning ? 'running' : 'paused'}
          </span>
        )}
        <div className="controls" style={{ marginLeft: 'auto' }}>
          {status?.isRunning ? (
            <button className="btn" onClick={() => act(api.pause)}>Pause</button>
          ) : (
            <button className="btn primary" onClick={() => act(api.start)} disabled={!status?.runId}>Start</button>
          )}
          <button className="btn" onClick={() => act(api.step)} disabled={!status || status.isRunning}>Step 15 min</button>
          <select
            aria-label="Simulation speed"
            value={SPEEDS.some((s) => s.value === status?.secondsPerTick) ? status?.secondsPerTick : ''}
            onChange={(e) => act(() => api.setSpeed(Number(e.target.value)))}
          >
            {!SPEEDS.some((s) => s.value === status?.secondsPerTick) && <option value="">Custom speed</option>}
            {SPEEDS.map((s) => (
              <option key={s.value} value={s.value}>{s.label}</option>
            ))}
          </select>
        </div>
      </header>

      {error && <p className="error">Cannot reach the API: {error}</p>}

      <nav className="tabs">
        {PAGES.map((p) => (
          <button key={p.key} className={page === p.key ? 'active' : ''} onClick={() => setPage(p.key)}>
            {p.label}
          </button>
        ))}
        {page !== 'scenarios' && (
          <div style={{ marginLeft: 'auto', paddingBottom: 6 }}>
            <RangePicker value={range} onChange={setRange} />
          </div>
        )}
      </nav>

      {page === 'overview' && <Overview status={status} range={range} />}
      {page === 'appliances' && <Appliances status={status} range={range} />}
      {page === 'energy' && <EnergyFlow status={status} range={range} />}
      {page === 'scenarios' && <Scenarios status={status} onStarted={refresh} />}
    </div>
  );
}
