type Status = 'NEW' | 'IN_PROGRESS' | 'RESOLVED'

const config: Record<Status, { label: string; classes: string }> = {
  NEW: { label: 'NEW', classes: 'bg-gray-700 text-gray-300 border-gray-600' },
  IN_PROGRESS: { label: 'IN PROGRESS', classes: 'bg-yellow-900/60 text-yellow-300 border-yellow-700' },
  RESOLVED: { label: 'RESOLVED', classes: 'bg-green-900/60 text-green-300 border-green-700' },
}

interface StatusBadgeProps {
  status: Status
}

export default function StatusBadge({ status }: StatusBadgeProps) {
  const { label, classes } = config[status] ?? config.NEW
  return (
    <span className={`inline-block border rounded px-2 py-0.5 text-xs font-mono font-semibold tracking-widest uppercase ${classes}`}>
      {label}
    </span>
  )
}
