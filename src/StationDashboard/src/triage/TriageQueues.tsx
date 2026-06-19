import ColdStartNotice from '../shared/ColdStartNotice'
import StatusBadge from './StatusBadge'
import { useTriageQueue, type TriageItem } from './useTriageQueue'

type Status = 'NEW' | 'IN_PROGRESS' | 'RESOLVED'

const STATUSES: Status[] = ['NEW', 'IN_PROGRESS', 'RESOLVED']

interface SecurityQueueProps {
  onError?: (msg: string) => void
}

function HazardPip({ level }: { level: number }) {
  return (
    <span
      className={`inline-block w-2 h-2 rounded-full mr-1 ${
        level >= 8 ? 'bg-red-500' : level >= 5 ? 'bg-orange-400' : 'bg-yellow-500'
      }`}
    />
  )
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
    <div className="rounded border border-gray-700 bg-gray-900 p-4 space-y-3">
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="font-mono font-bold text-gray-100">{item.ship_name}</p>
          <p className="text-xs text-gray-500 font-mono">
            {item.callsign} &mdash; Capt. {item.captain_name}
          </p>
        </div>
        <StatusBadge status={currentStatus} />
      </div>

      <div className="flex items-center gap-1 text-xs font-mono text-gray-400">
        <HazardPip level={hazardLevel} />
        {hazardLabel}:{' '}
        <span className={`font-bold ml-1 ${hazardLevel >= 8 ? 'text-red-400' : hazardLevel >= 5 ? 'text-orange-400' : 'text-yellow-400'}`}>
          {hazardLevel}/10
        </span>
      </div>

      <div className="flex gap-2 flex-wrap">
        {STATUSES.filter(s => s !== currentStatus).map(s => (
          <button
            key={s}
            onClick={() => onUpdate(item.id, s)}
            className="px-2 py-1 text-xs font-mono border border-gray-600 hover:border-gray-400 rounded text-gray-400 hover:text-gray-200 transition-colors"
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
      {loading && <p className="text-sm text-gray-500 font-mono animate-pulse">Loading security queue…</p>}
      <ColdStartNotice slow={slow} error={error} onRetry={reload} accentClass="border-red-700 text-red-400" />
      {!loading && !error && items.length === 0 && (
        <p className="text-sm text-gray-500 font-mono">No active security threats.</p>
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
      {loading && <p className="text-sm text-gray-500 font-mono animate-pulse">Loading medical queue…</p>}
      <ColdStartNotice slow={slow} error={error} onRetry={reload} accentClass="border-red-700 text-red-400" />
      {!loading && !error && items.length === 0 && (
        <p className="text-sm text-gray-500 font-mono">No active medical threats.</p>
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
      {loading && <p className="text-sm text-gray-500 font-mono animate-pulse">Loading hazmat queue…</p>}
      <ColdStartNotice slow={slow} error={error} onRetry={reload} accentClass="border-red-700 text-red-400" />
      {!loading && !error && items.length === 0 && (
        <p className="text-sm text-gray-500 font-mono">No active hazmat threats.</p>
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
