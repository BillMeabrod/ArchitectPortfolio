import { Link, useLocation } from 'react-router-dom'

const zones = [
  { path: '/manifests', label: 'Manifests' },
  { path: '/ai-console', label: 'AI Console' },
  { path: '/triage', label: 'Triage' },
]

interface LayoutProps {
  children: React.ReactNode
  zoneName: string
  headerClass: string
}

export default function Layout({ children, zoneName, headerClass }: LayoutProps) {
  const location = useLocation()

  return (
    <div className="min-h-screen flex flex-col">
      <header className={`px-6 py-3 flex items-center gap-6 ${headerClass}`}>
        <Link to="/" className="text-sm opacity-60 hover:opacity-100 transition-opacity mr-2">
          ← Home
        </Link>
        <span className="font-mono text-sm font-semibold tracking-widest uppercase">
          {zoneName}
        </span>
        <nav className="ml-auto flex gap-4">
          {zones.map(z => (
            <Link
              key={z.path}
              to={z.path}
              className={`text-sm font-mono transition-opacity ${
                location.pathname === z.path ? 'opacity-100 font-bold' : 'opacity-50 hover:opacity-80'
              }`}
            >
              {z.label}
            </Link>
          ))}
        </nav>
      </header>
      <main className="flex-1 p-6 md:p-8">
        {children}
      </main>
    </div>
  )
}
