import { useNavigate } from 'react-router-dom'
import ColdStartNotice from '../shared/ColdStartNotice'
import StatusBadge from './StatusBadge'
import { useTriageQueue, type TriageItem } from './useTriageQueue'

type Status = 'NEW' | 'IN_PROGRESS' | 'RESOLVED'

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
  queue,
}: {
  item: TriageItem
  hazardLabel: string
  hazardLevel: number
  statusKey: keyof TriageItem
  queue: string
}) {
  const navigate = useNavigate()
  const currentStatus = (item[statusKey] as Status) ?? 'NEW'

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={() => navigate(`/triage/${queue}/${item.id}`)}
      onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); navigate(`/triage/${queue}/${item.id}`) } }}
      className="bg-[#11161f] border-l-4 rounded-r-[4px] p-4 space-y-3 cursor-pointer hover:bg-[#161c27] transition-colors focus:outline-none focus:ring-1 focus:ring-[#85b7eb]"
      style={{ borderLeftColor: severityColor(hazardLevel) }}
    >
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="font-mono font-bold text-[#e6f1fb]">
            {item.inappropriateContent ? '[REDACTED]' : item.shipName}
          </p>
          <p className="text-xs text-[#5f5e5a] font-mono">
            {item.inappropriateContent
              ? '[REDACTED] \u2014 Capt. [REDACTED]'
              : `${item.callsign} \u2014 Capt. ${item.captainName}`}
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
    </div>
  )
}

export function SecurityQueue(_props: SecurityQueueProps) {
  const { items, loading, slow, error, reload } = useTriageQueue('security')

  return (
    <div>
      {loading && <p className="text-sm text-[#5f5e5a] font-mono animate-pulse">Loading security queue…</p>}
      <ColdStartNotice slow={slow} error={error} onRetry={reload} accentClass="border-red-700 text-red-400" textClass="text-[#5f5e5a]" />
      {!loading && !error && items.length === 0 && (
        <p className="text-sm text-[#5f5e5a] font-mono">No active security threats.</p>
      )}
      <div className="space-y-3 mt-2">
        {items.map(item => (
          <QueueCard
            key={item.id}
            item={item}
            hazardLabel="Security"
            hazardLevel={item.securityHazardLevel ?? 0}
            statusKey="securityStatus"
            queue="security"
          />
        ))}
      </div>
    </div>
  )
}

export function MedicalQueue(_props: SecurityQueueProps) {
  const { items, loading, slow, error, reload } = useTriageQueue('medical')

  return (
    <div>
      {loading && <p className="text-sm text-[#5f5e5a] font-mono animate-pulse">Loading medical queue…</p>}
      <ColdStartNotice slow={slow} error={error} onRetry={reload} accentClass="border-red-700 text-red-400" textClass="text-[#5f5e5a]" />
      {!loading && !error && items.length === 0 && (
        <p className="text-sm text-[#5f5e5a] font-mono">No active medical threats.</p>
      )}
      <div className="space-y-3 mt-2">
        {items.map(item => (
          <QueueCard
            key={item.id}
            item={item}
            hazardLabel="Biohazard"
            hazardLevel={item.biohazardLevel ?? 0}
            statusKey="medicalStatus"
            queue="medical"
          />
        ))}
      </div>
    </div>
  )
}

export function HazmatQueue(_props: SecurityQueueProps) {
  const { items, loading, slow, error, reload } = useTriageQueue('hazmat')

  return (
    <div>
      {loading && <p className="text-sm text-[#5f5e5a] font-mono animate-pulse">Loading hazmat queue…</p>}
      <ColdStartNotice slow={slow} error={error} onRetry={reload} accentClass="border-red-700 text-red-400" textClass="text-[#5f5e5a]" />
      {!loading && !error && items.length === 0 && (
        <p className="text-sm text-[#5f5e5a] font-mono">No active hazmat threats.</p>
      )}
      <div className="space-y-3 mt-2">
        {items.map(item => (
          <QueueCard
            key={item.id}
            item={item}
            hazardLabel="Chemical"
            hazardLevel={item.chemicalHazardLevel ?? 0}
            statusKey="hazmatStatus"
            queue="hazmat"
          />
        ))}
      </div>
    </div>
  )
}
