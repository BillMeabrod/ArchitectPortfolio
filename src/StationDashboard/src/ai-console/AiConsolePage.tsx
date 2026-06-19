import { useState, useEffect } from 'react'
import Layout from '../shared/Layout'
import ColdStartNotice from '../shared/ColdStartNotice'
import { useUniverseRules } from './useUniverseRules'

export default function AiConsolePage() {
  const { data, loading, slow, fetchError, saveLoading, saveError, saveSuccess, reload, save } =
    useUniverseRules()

  const [intel, setIntel] = useState('')

  useEffect(() => {
    if (data) setIntel(data.universeIntel)
  }, [data])

  async function handleSave(e: React.FormEvent) {
    e.preventDefault()
    await save(intel)
  }

  const sectionHeadClass = 'text-xs font-mono font-semibold tracking-widest uppercase text-cyan-400/70 mb-2'
  const blockClass = 'rounded border border-indigo-800 bg-indigo-950/60 p-4'

  return (
    <Layout zoneName="ARIA AI Console" headerClass="bg-indigo-950 text-indigo-100 border-b border-indigo-800">
      <div className="max-w-3xl">
        <div className="mb-6">
          <h1 className="text-xl font-mono font-bold text-cyan-400 tracking-wide uppercase">
            Operational Rule Configuration
          </h1>
          <p className="text-indigo-300 text-sm mt-1 font-mono">
            Inspect ARIA&apos;s core directive and configure volatile universe intel.
          </p>
        </div>

        {loading && (
          <div className="space-y-4">
            <div className={blockClass}>
              <p className="text-indigo-400 text-sm font-mono animate-pulse">
                Connecting to ARIA…
              </p>
            </div>
            <ColdStartNotice
              slow={slow}
              error={null}
              onRetry={reload}
              accentClass="border-cyan-700 text-cyan-400"
            />
          </div>
        )}

        {!loading && fetchError && (
          <ColdStartNotice
            slow={false}
            error={fetchError}
            onRetry={reload}
            accentClass="border-cyan-700 text-cyan-400"
          />
        )}

        {!loading && data && (
          <div className="space-y-6">
            <div>
              <p className={sectionHeadClass}>Core Directive — Read Only</p>
              <div className={`${blockClass} relative`}>
                <span className="absolute top-2 right-3 text-xs font-mono text-indigo-500 uppercase tracking-widest">
                  Immutable
                </span>
                <pre className="text-indigo-200 text-xs leading-relaxed whitespace-pre-wrap font-mono">
                  {data.coreDirective}
                </pre>
              </div>
            </div>

            <form onSubmit={handleSave}>
              <p className={sectionHeadClass}>Universe Intel — Editable</p>
              <div className={blockClass}>
                <p className="text-xs text-indigo-400 font-mono mb-3">
                  Active tactical sector updates fed into every risk assessment. Modifications take
                  effect on the next assessment cycle.
                </p>
                <textarea
                  className="w-full bg-indigo-900/50 border border-indigo-700 rounded px-3 py-2 text-indigo-100 placeholder-indigo-500 focus:outline-none focus:border-cyan-500 font-mono text-sm resize-y min-h-36"
                  value={intel}
                  onChange={e => setIntel(e.target.value)}
                  placeholder="Enter sector intel…"
                />
              </div>

              <div className="mt-3 flex items-center gap-4">
                <button
                  type="submit"
                  disabled={saveLoading}
                  className="px-5 py-2 bg-cyan-700 hover:bg-cyan-600 disabled:opacity-50 disabled:cursor-not-allowed rounded font-mono font-bold text-white text-sm tracking-wide transition-colors"
                >
                  {saveLoading ? 'Transmitting…' : 'Save Intel'}
                </button>
                {saveSuccess && (
                  <span className="text-sm font-mono text-cyan-400">✓ Intel updated</span>
                )}
                {saveError && (
                  <span className="text-sm font-mono text-red-400">{saveError}</span>
                )}
              </div>
            </form>
          </div>
        )}
      </div>
    </Layout>
  )
}
