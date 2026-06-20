import { Link } from 'react-router-dom'

const zones = [
  {
    path: '/manifests',
    label: 'Ship Manifest Logger',
    pattern: 'Vertical Slice Architecture',
    desc: 'Submit and audit incoming vessel manifests. Each feature is a self-contained slice — handler, command, and persistence co-located with no shared service layer.',
    accent: 'text-amber-400',
    border: 'border-slate-600',
    bg: 'bg-slate-800/50 hover:bg-slate-800/80',
  },
  {
    path: '/ai-console',
    label: 'ARIA AI Console',
    pattern: 'Hexagonal Architecture',
    desc: "Configure ARIA's operational rules and inspect her immutable core directive. Ports and adapters keep AI and storage concerns fully decoupled from the domain.",
    accent: 'text-cyan-400',
    border: 'border-indigo-700',
    bg: 'bg-indigo-950/50 hover:bg-indigo-950/80',
  },
  {
    path: '/triage',
    label: 'Station Triage',
    pattern: 'Django MTV',
    desc: 'Monitor and action security, medical, and hazmat queues populated by ARIA risk assessments. Model-Template-View — now serving JSON to this frontend.',
    accent: 'text-red-400',
    border: 'border-gray-700',
    bg: 'bg-gray-900/50 hover:bg-gray-900/80',
  },
]

export default function LandingPage() {
  return (
    <div className="min-h-screen bg-gray-950 text-gray-100 flex flex-col">
      <header className="px-8 py-6 border-b border-gray-800">
        <h1 className="text-2xl font-mono font-bold tracking-widest text-gray-100 uppercase">
          Meabrod Station — Dashboard
        </h1>
        <p className="text-sm text-gray-500 mt-1 font-mono">
          Three backend applications. Three architectural patterns.
        </p>
      </header>

      <main className="flex-1 px-8 py-10">
        <div className="grid gap-6 md:grid-cols-3 max-w-5xl">
          {zones.map(zone => (
            <Link
              key={zone.path}
              to={zone.path}
              className={`block rounded-lg border p-6 transition-colors ${zone.border} ${zone.bg}`}
            >
              <div className={`font-mono text-xs font-semibold tracking-widest uppercase mb-1 ${zone.accent}`}>
                {zone.pattern}
              </div>
              <h2 className="text-lg font-semibold text-gray-100 mb-2">{zone.label}</h2>
              <p className="text-sm text-gray-400 leading-relaxed">{zone.desc}</p>
              <div className={`mt-4 text-xs font-mono ${zone.accent} opacity-70`}>
                Enter zone →
              </div>
            </Link>
          ))}
        </div>
      </main>
    </div>
  )
}
