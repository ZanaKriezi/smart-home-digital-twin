import { useState } from 'react';
import { api, type SimulationStatus } from '../api';
import { usePolling } from '../usePolling';
import { Card } from '../components/ui';

export default function Scenarios({ status, onStarted }: { status: SimulationStatus | null; onStarted: () => void }) {
  const { data: scenarios, refresh } = usePolling(api.scenarios, [], 5000);
  const [busy, setBusy] = useState<string | null>(null);

  const start = async (id: string) => {
    setBusy(id);
    try {
      await api.startScenario(id);
      await refresh();
      onStarted();
    } finally {
      setBusy(null);
    }
  };

  return (
    <div className="grid two-col">
      {(scenarios ?? []).map((s) => (
        <Card key={s.id} title={s.name} className={`scenario ${s.isActive ? 'active' : ''}`}>
          <p style={{ margin: 0 }}>{s.description}</p>
          <p className="sub" style={{ margin: 0 }}>
            Simulated date {s.startDate} · battery starts at {s.initialBatterySocPercent} % · seed {s.seed}
          </p>
          <div className="controls">
            <button className="btn primary" onClick={() => start(s.id)} disabled={busy !== null}>
              {s.isActive ? 'Restart' : 'Start'} scenario
            </button>
            {s.isActive && status && <span className="badge on"><span className="dot" />Active</span>}
          </div>
        </Card>
      ))}
    </div>
  );
}
