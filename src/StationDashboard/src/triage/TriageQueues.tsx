import ColdStartNotice from '../shared/ColdStartNotice'
import StatusBadge from './StatusBadge'
import { useTriageQueue, type TriageItem } from './useTriageQueue'

type Status = 'NEW' | 'IN_PROGRESS' | 'RESOLVED'

const STATUSES: Status[] = ['NEW', 'IN_PROGRESS', 'RESOLVED']

interface SecurityQueueProps {
  onError?: (msg: string) => void
}

function severityColor(level: number): string {
  if (level >= 8) return '#ef4444'
  if (level >= 5) return '#f97316'
  return '#eab308'
}

function QueueCard({
  item,
  hazardLabel,
  hazardLevel,
  statusKey,
  onUpdate,
}: {
  item: TriageItem
  hazardLabel: string
  hazardLevel: number
  statusKey: keyof TriageItem
  onUpdate: (id: number, status: Status) => void
}) {
  const currentStatus = (item[statusKey] as Status) ?? 'NEW'

  return (
    <div
      className="bg-[#11161f] border-l-4 rounded-r-[4px] p-4 space-y-3"
      style={{ borderLeftColor: severityColor(hazardLevel) }}
    >
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="font-mono font-bold text-[#e6f1fb]">{item.ship_name}</p>
          <p className="text-xs text-[#5f5e5a] font-mono">
            {item.callsign} &mdash; Capt. {item.captain_name}
          </p>
        </div>
        <StatusBadge status={currentStatus} />
      </div>

      <div className="flex items-center gap-1 text-xs font-mono text-[#5f5e5a]">
        <span
          className="inline-block w-2 h-2 rounded-full mr-1"
          style={{ backgroundColor: severityColor(hazardLevel) }}
        />
        {hazardLabel}:{' '}
        <span className="font-bold ml-1" style={{ color: severityColor(hazardLevel) }}>
          {hazardLevel}/10
        </span>
      </div>

      <div className="flex gap-2 flex-wrap">
        {STATUSES.filter(s => s !== currentStatus).map(s => (
          <button
            key={s}
            onClick={() => onUpdate(item.id, s)}
            className="px-2 py-1 text-xs font-mono border border-[#85b7eb]/30 hover:border-[#85b7eb] rounded-[2px] text-[#5f5e5a] hover:text-[#e6f1fb] transition-colors"
          >
            → {s.replace('_', ' ')}
          </button>
        ))}
      </div>
    </div>
  )
}

export function SecurityQueue(_props: SecurityQueueProps) {
  const { items, loading, slow, error, reload, updateStatus } = useTriageQueue('security')

  return (
    <div>
      {loading && <p className="text-sm text-[#5f5e5a] font-mono animate-pulse">Loading security queue…</p>}
      <ColdStartNotice slow={slow} error={error} onRetry={reload} accentClass="border-red-700 text-red-400" />
      {!loading && !error && items.length === 0 && (
        <p className="text-sm text-[#5f5e5a] font-mono">No active security threats.</p>
      )}
      <div className="space-y-3 mt-2">
        {items.map(item => (
          <QueueCard
            key={item.id}
            item={item}
            hazardLabel="Security"
            hazardLevel={item.security_hazard_level ?? 0}
            statusKey="security_status"
            onUpdate={updateStatus}
          />
        ))}
      </div>
    </div>
  )
}

export function MedicalQueue(_props: SecurityQueueProps) {
  const { items, loading, slow, error, reload, updateStatus } = useTriageQueue('medical')

  return (
    <div>
      {loading && <p className="text-sm text-[#5f5e5a] font-mono animate-pulse">Loading medical queue…</p>}
      <ColdStartNotice slow={slow} error={error} onRetry={reload} accentClass="border-red-700 text-red-400" />
      {!loading && !error && items.length === 0 && (
        <p className="text-sm text-[#5f5e5a] font-mono">No active medical threats.</p>
      )}
      <div className="space-y-3 mt-2">
        {items.map(item => (
          <QueueCard
            key={item.id}
            item={item}
            hazardLabel="Biohazard"
            hazardLevel={item.biohazard_level ?? 0}
            statusKey="medical_status"
            onUpdate={updateStatus}
          />
        ))}
      </div>
    </div>
  )
}

export function HazmatQueue(_props: SecurityQueueProps) {
  const { items, loading, slow, error, reload, updateStatus } = useTriageQueue('hazmat')

  return (
    <div>
      {loading && <p className="text-sm text-[#5f5e5a] font-mono animate-pulse">Loading hazmat queue…</p>}
      <ColdStartNotice slow={slow} error={error} onRetry={reload} accentClass="border-red-700 text-red-400" />
      {!loading && !error && items.length === 0 && (
        <p className="text-sm text-[#5f5e5a] font-mono">No active hazmat threats.</p>
      )}
      <div className="space-y-3 mt-2">
        {items.map(item => (
          <QueueCard
            key={item.id}
            item={item}
            hazardLabel="Chemical"
            hazardLevel={item.chemical_hazard_level ?? 0}
            statusKey="hazmat_status"
            onUpdate={updateStatus}
          />
        ))}
      </div>
    </div>
  )
}
