import { useState, useEffect, type FormEvent } from 'react'
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

  async function handleSave(e: FormEvent) {
    e.preventDefault()
    await save(intel)
  }

  const sectionHeadClass = 'text-xs font-mono font-semibold tracking-widest text-[#5dcaa5] mb-2'
  const blockClass = 'rounded border border-[#1d9e75] bg-[#04342c] p-4'

  return (
    <Layout
      zoneName="ARIA AI Console"
      headerClass="bg-black text-[#5dcaa5] border-b border-[#1d9e75]"
      pageClass="bg-black"
    >
      <div className="max-w-3xl">
        <div className="mb-6">
          <h1 className="text-xl font-mono font-bold text-[#5dcaa5] tracking-wide">
            &gt; ARIA OPERATIONAL RULE CONFIGURATION
          </h1>
          <p className="text-[#9fe1cb]/50 text-sm mt-1 font-mono">
            Inspect ARIA&apos;s core directive and configure volatile universe intel.
          </p>
        </div>

        {loading && (
          <div className="space-y-4">
            <div className={blockClass}>
              <p className="text-[#5dcaa5] text-sm font-mono animate-pulse">
                &gt; Connecting to ARIA…
              </p>
            </div>
            <ColdStartNotice
              slow={slow}
              error={null}
              onRetry={reload}
              accentClass="border-[#1d9e75] text-[#5dcaa5]"
            />
          </div>
        )}

        {!loading && fetchError && (
          <ColdStartNotice
            slow={false}
            error={fetchError}
            onRetry={reload}
            accentClass="border-[#1d9e75] text-[#5dcaa5]"
          />
        )}

        {!loading && data && (
          <div className="space-y-6">
            <div>
              <p className={sectionHeadClass}>&gt; CORE DIRECTIVE [READ ONLY]</p>
              <div className={`${blockClass} relative`}>
                <span className="absolute top-2 right-3 text-xs font-mono text-[#1d9e75] uppercase tracking-widest">
                  Immutable
                </span>
                <pre className="text-[#9fe1cb] text-xs leading-relaxed whitespace-pre-wrap font-mono">
                  {data.coreDirective}
                </pre>
              </div>
            </div>

            <form onSubmit={handleSave}>
              <p className={sectionHeadClass}>
                &gt; UNIVERSE INTEL [EDITABLE]<span className="cursor-blink">█</span>
              </p>
              <div className={blockClass}>
                <p className="text-xs text-[#9fe1cb]/60 font-mono mb-3">
                  Active tactical sector updates fed into every risk assessment. Modifications take
                  effect on the next assessment cycle.
                </p>
                <textarea
                  className="w-full bg-black border border-[#1d9e75]/60 rounded-[2px] px-3 py-2 text-[#5dcaa5] placeholder-[#1d9e75]/40 focus:outline-none focus:border-[#1d9e75] font-mono text-sm resize-y min-h-36"
                  value={intel}
                  onChange={e => setIntel(e.target.value)}
                  placeholder="Enter sector intel…"
                />
              </div>

              <div className="mt-3 flex items-center gap-4">
                <button
                  type="submit"
                  disabled={saveLoading}
                  className="px-5 py-2 bg-transparent border border-[#1d9e75] hover:bg-[#1d9e75]/10 disabled:opacity-50 disabled:cursor-not-allowed rounded-[2px] font-mono font-bold text-[#5dcaa5] text-sm tracking-widest transition-colors"
                >
                  {saveLoading ? '[ TRANSMITTING… ]' : '[ SAVE_INTEL ]'}
                </button>
                {saveSuccess && (
                  <span className="text-sm font-mono text-[#5dcaa5]">✓ Intel updated</span>
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
