import { type ChangeEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import Layout from '../shared/Layout'
import ColdStartNotice from '../shared/ColdStartNotice'
import StatusBadge from './StatusBadge'
import { useTriageDetail, type Queue, type TriageDetail } from './useTriageDetail'

type Status = 'NEW' | 'IN_PROGRESS' | 'RESOLVED'
const STATUSES: Status[] = ['NEW', 'IN_PROGRESS', 'RESOLVED']

const QUEUE_CONFIG: Record<Queue, {
  label: string
  hazardField: keyof TriageDetail
  hazardLabel: string
  statusField: keyof TriageDetail
  accentClass: string
  severityBorder: (level: number) => string
}> = {
  security: {
    label: 'Security',
    hazardField: 'security_hazard_level',
    hazardLabel: 'Security Hazard',
    statusField: 'security_status',
    accentClass: 'border-red-700 text-red-400',
    severityBorder: (l) => l >= 8 ? '#ef4444' : l >= 5 ? '#f97316' : '#eab308',
  },
  medical: {
    label: 'Medical',
    hazardField: 'biohazard_level',
    hazardLabel: 'Biohazard Level',
    statusField: 'medical_status',
    accentClass: 'border-green-700 text-green-400',
    severityBorder: (l) => l >= 8 ? '#ef4444' : l >= 5 ? '#f97316' : '#eab308',
  },
  hazmat: {
    label: 'Hazmat',
    hazardField: 'chemical_hazard_level',
    hazardLabel: 'Chemical Hazard',
    statusField: 'hazmat_status',
    accentClass: 'border-orange-700 text-orange-400',
    severityBorder: (l) => l >= 8 ? '#ef4444' : l >= 5 ? '#f97316' : '#eab308',
  },
}

const VALID_QUEUES: Queue[] = ['security', 'medical', 'hazmat']

function isValidQueue(q: string | undefined): q is Queue {
  return VALID_QUEUES.includes(q as Queue)
}

export default function TriageDetailPage() {
  const { queue, id } = useParams<{ queue: string; id: string }>()

  if (!isValidQueue(queue) || !id) {
    return (
      <Layout
        zoneName="Station Triage"
        headerClass="bg-[#0a0e14] text-[#e6f1fb] border-b border-[#85b7eb]"
        pageClass="bg-[#0a0e14]"
      >
        <p className="text-red-400 font-mono text-sm">Invalid triage route.</p>
      </Layout>
    )
  }

  return <DetailView queue={queue} id={id} />
}

function DetailView({ queue, id }: { queue: Queue; id: string }) {
  const { detail, loading, slow, error, reload, updateStatus } = useTriageDetail(queue, id)
  const config = QUEUE_CONFIG[queue]

  const currentStatus = (detail?.[config.statusField] as Status | undefined) ?? 'NEW'
  const hazardLevel = (detail?.[config.hazardField] as number | undefined) ?? 0
  const accentColor = config.severityBorder(hazardLevel)

  async function handleStatusChange(e: ChangeEvent<HTMLSelectElement>) {
    await updateStatus(e.target.value as Status)
  }

  return (
    <Layout
      zoneName="Station Triage"
      headerClass="bg-[#0a0e14] text-[#e6f1fb] border-b border-[#85b7eb]"
      pageClass="bg-[#0a0e14]"
    >
      <div className="max-w-2xl">
        <div className="mb-6">
          <Link
            to="/triage"
            className="text-xs font-mono text-[#85b7eb] hover:text-[#e6f1fb] transition-colors"
          >
            ← Back to {config.label} Queue
          </Link>
        </div>

        {loading && (
          <p className="text-sm text-[#5f5e5a] font-mono animate-pulse">Loading…</p>
        )}

        <ColdStartNotice slow={slow} error={error} onRetry={reload} accentClass="border-red-700 text-red-400" textClass="text-[#5f5e5a]" />

        {!loading && detail && (
          <div
            className="bg-[#11161f] border-l-4 rounded-r-[4px] p-6 space-y-6"
            style={{ borderLeftColor: accentColor }}
          >
            <div className="flex items-start justify-between gap-4">
              <div>
                <h1 className="font-mono font-bold text-xl text-[#e6f1fb]">
                  {detail.inappropriate_content ? '[REDACTED]' : detail.ship_name}
                </h1>
                <p className="text-sm text-[#5f5e5a] font-mono mt-1">
                  {detail.inappropriate_content
                    ? '[REDACTED] \u2014 Capt. [REDACTED]'
                    : `${detail.callsign} \u2014 Capt. ${detail.captain_name}`}
                </p>
              </div>
              <StatusBadge status={currentStatus} />
            </div>

            <div className="flex items-center gap-2 text-sm font-mono text-[#5f5e5a]">
              <span
                className="inline-block w-2 h-2 rounded-full"
                style={{ backgroundColor: accentColor }}
              />
              {config.hazardLabel}:{' '}
              <span className="font-bold ml-1" style={{ color: accentColor }}>
                {hazardLevel}/10
              </span>
            </div>

            <div>
              <p className="text-xs font-mono text-[#5f5e5a] uppercase tracking-widest mb-2">
                Recommendation
              </p>
              <p className="text-sm font-mono text-[#c9d8ea] leading-relaxed whitespace-pre-wrap">
                {detail.recommendation}
              </p>
            </div>

            {Array.isArray(detail.cargo_items) && detail.cargo_items.length > 0 && (
              <div>
                <p className="text-xs font-mono text-[#5f5e5a] uppercase tracking-widest mb-2">
                  Cargo Items
                </p>
                <ul className="space-y-1">
                  {detail.inappropriate_content
                    ? (
                      <li className="text-sm font-mono text-[#c9d8ea] flex gap-2">
                        <span className="text-[#5f5e5a]">—</span>
                        [REDACTED]
                      </li>
                    )
                    : detail.cargo_items.map((item, i) => (
                      <li key={`cargo-${i}-${item}`} className="text-sm font-mono text-[#c9d8ea] flex gap-2">
                        <span className="text-[#5f5e5a]">—</span>
                        {item}
                      </li>
                    ))}
                </ul>
              </div>
            )}

            {Array.isArray(detail.passengers) && detail.passengers.length > 0 && (
              <div>
                <p className="text-xs font-mono text-[#5f5e5a] uppercase tracking-widest mb-2">
                  Passengers
                </p>
                <ul className="space-y-1">
                  {detail.inappropriate_content
                    ? (
                      <li className="text-sm font-mono text-[#c9d8ea] flex gap-2">
                        <span className="text-[#5f5e5a]">—</span>
                        [REDACTED]
                      </li>
                    )
                    : detail.passengers.map((p, i) => (
                      <li key={`passenger-${i}-${p}`} className="text-sm font-mono text-[#c9d8ea] flex gap-2">
                        <span className="text-[#5f5e5a]">—</span>
                        {p}
                      </li>
                    ))}
                </ul>
              </div>
            )}

            <div>
              <p className="text-xs font-mono text-[#5f5e5a] uppercase tracking-widest mb-2">
                Update Status
              </p>
              <select
                value={currentStatus}
                onChange={handleStatusChange}
                className="bg-[#0a0e14] border border-[#85b7eb]/30 hover:border-[#85b7eb] text-[#e6f1fb] font-mono text-sm rounded-[2px] px-3 py-1.5 focus:outline-none focus:border-[#85b7eb] transition-colors"
              >
                {STATUSES.map(s => (
                  <option key={s} value={s}>
                    {s.replace('_', ' ')}
                  </option>
                ))}
              </select>
            </div>
          </div>
        )}
      </div>
    </Layout>
  )
}
