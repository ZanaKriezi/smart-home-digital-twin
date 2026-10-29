// Types and calls for the twin's REST API (see SmartHomeTwin.Api/Controllers).

export type DeviceCategory = 'Consumer' | 'Producer' | 'Storage';

export interface DeviceState {
  id: string;
  name: string;
  type: string;
  category: DeviceCategory;
  ratedPowerKw: number;
  isEnabled: boolean;
  isOn: boolean;
  powerKw: number;
  lastStepEnergyKwh: number;
  isShiftable: boolean;
  stateOfChargePercent: number | null;
}

export interface Alert {
  type: string;
  timestamp: string;
  message: string;
  value: number;
  threshold: number;
}

export interface HomeSnapshot {
  runId: string;
  scenarioId: string;
  tickIndex: number;
  timestamp: string;
  loadKw: number;
  pvKw: number;
  batteryChargeKw: number;
  batteryDischargeKw: number;
  batterySocKwh: number;
  batterySocPercent: number;
  gridImportKw: number;
  gridExportKw: number;
  devices: DeviceState[];
  activeAlerts: Alert[];
}

export interface SimulationStatus {
  isRunning: boolean;
  secondsPerTick: number;
  stepMinutes: number;
  currentTime: string;
  tickIndex: number;
  scenarioId: string | null;
  runId: string | null;
  pendingCommands: number;
}

export interface EnergyPoint {
  timestamp: string;
  loadKw: number;
  pvKw: number;
  batteryChargeKw: number;
  batteryDischargeKw: number;
  batterySocPercent: number;
  gridImportKw: number;
  gridExportKw: number;
}

export interface Reading {
  timestamp: string;
  deviceId: string;
  powerKw: number;
  energyKwh: number;
  isOn: boolean;
}

export interface KpiSummary {
  steps: number;
  loadKwh: number;
  pvKwh: number;
  gridImportKwh: number;
  gridExportKwh: number;
  batteryChargeKwh: number;
  batteryDischargeKwh: number;
  peakLoadKw: number;
  peakGridImportKw: number;
  selfConsumptionRate: number | null;
  selfSufficiencyRate: number | null;
  applianceKwh: Record<string, number>;
}

export interface Scenario {
  id: string;
  name: string;
  description: string;
  seed: number;
  startDate: string;
  initialBatterySocPercent: number;
  isActive: boolean;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  });
  if (!response.ok) throw new Error(`${response.status} ${await response.text()}`);
  return (await response.json()) as T;
}

function range(from?: string, to?: string): string {
  const params = new URLSearchParams();
  if (from) params.set('from', from);
  if (to) params.set('to', to);
  const query = params.toString();
  return query ? `?${query}` : '';
}

export const api = {
  current: () => request<HomeSnapshot>('/energy/current'),
  history: (from?: string, to?: string) => request<EnergyPoint[]>(`/energy/history${range(from, to)}`),
  kpis: (from?: string, to?: string) => request<KpiSummary>(`/energy/kpis${range(from, to)}`),
  readings: (from?: string, to?: string) => request<Reading[]>(`/readings${range(from, to)}`),
  alerts: () => request<Alert[]>('/alerts?limit=20'),
  scenarios: () => request<Scenario[]>('/scenarios'),
  startScenario: (id: string) => request<unknown>(`/scenarios/${id}/start`, { method: 'POST' }),
  simulation: () => request<SimulationStatus>('/simulation'),
  start: () => request<SimulationStatus>('/simulation/start', { method: 'POST' }),
  pause: () => request<SimulationStatus>('/simulation/pause', { method: 'POST' }),
  step: () => request<HomeSnapshot>('/simulation/step', { method: 'POST' }),
  setSpeed: (secondsPerTick: number) =>
    request<SimulationStatus>('/simulation/speed', { method: 'PUT', body: JSON.stringify({ secondsPerTick }) }),
  command: (deviceId: string, action: 'On' | 'Off') =>
    request<unknown>(`/devices/${deviceId}/commands`, { method: 'POST', body: JSON.stringify({ action }) }),
};

/** Formats a simulated timestamp ("2026-04-15T18:30:00") without converting time zones. */
export function formatTime(iso: string): string {
  return iso.slice(11, 16);
}

export function formatDateTime(iso: string): string {
  return `${iso.slice(0, 10)} ${iso.slice(11, 16)}`;
}
