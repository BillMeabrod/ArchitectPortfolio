import { useState } from 'react'
import Layout from '../shared/Layout'
import ColdStartNotice from '../shared/ColdStartNotice'
import { useSubmitManifest } from './useSubmitManifest'

export default function ManifestsPage() {
  const { submit, result, loading, slow, error, reset } = useSubmitManifest()

  const [shipName, setShipName] = useState('')
  const [callsign, setCallsign] = useState('')
  const [captainName, setCaptainName] = useState('')
  const [cargoItems, setCargoItems] = useState<string[]>([''])
  const [passengers, setPassengers] = useState<string[]>([''])

  function handleListChange(
    list: string[],
    setter: (v: string[]) => void,
    index: number,
    value: string,
  ) {
    const next = [...list]
    next[index] = value
    setter(next)
  }

  function addItem(list: string[], setter: (v: string[]) => void) {
    setter([...list, ''])
  }

  function removeItem(list: string[], setter: (v: string[]) => void, index: number) {
    setter(list.filter((_, i) => i !== index))
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    await submit({
      shipName,
      callsign,
      captainName,
      cargoItems: cargoItems.filter(Boolean),
      passengers: passengers.filter(Boolean),
    })
  }

  const fieldClass =
    'w-full bg-slate-900 border border-slate-600 rounded px-3 py-2 text-slate-100 placeholder-slate-500 focus:outline-none focus:border-amber-500 font-mono text-sm'
  const labelClass = 'block text-xs font-mono text-amber-400/80 uppercase tracking-widest mb-1'

  return (
    <Layout zoneName="Ship Manifest Logger" headerClass="bg-slate-900 text-slate-100 border-b border-slate-700">
      <div className="max-w-2xl">
        <div className="mb-6">
          <h1 className="text-xl font-mono font-bold text-amber-400 tracking-wide uppercase">
            Docking Manifest Entry
          </h1>
          <p className="text-slate-400 text-sm mt-1 font-mono">
            Submit vessel details for audit and queue processing.
          </p>
        </div>

        {result ? (
          <div className="rounded border border-amber-700 bg-slate-900 p-6">
            <div className="flex items-start gap-3 mb-4">
              <span className="text-amber-400 text-2xl">✓</span>
              <div>
                <p className="text-amber-400 font-mono font-bold text-lg">Manifest Logged</p>
                <p className="text-slate-300 text-sm mt-1">
                  Audit ID:{' '}
                  <span className="font-mono font-bold text-amber-300">#{result.auditId}</span>
                </p>
                <p className="text-slate-500 text-xs mt-1">
                  Vessel queued for ARIA risk assessment.
                </p>
              </div>
            </div>
            <button
              onClick={reset}
              className="px-4 py-2 bg-slate-800 border border-slate-600 hover:border-amber-500 rounded text-sm font-mono text-slate-300 transition-colors"
            >
              Submit Another Manifest
            </button>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-5">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className={labelClass}>Ship Name</label>
                <input
                  className={fieldClass}
                  value={shipName}
                  onChange={e => setShipName(e.target.value)}
                  placeholder="e.g. Ardent Nomad"
                  required
                />
              </div>
              <div>
                <label className={labelClass}>Callsign</label>
                <input
                  className={fieldClass}
                  value={callsign}
                  onChange={e => setCallsign(e.target.value)}
                  placeholder="e.g. AN-7714"
                  required
                />
              </div>
            </div>

            <div>
              <label className={labelClass}>Captain Name</label>
              <input
                className={fieldClass}
                value={captainName}
                onChange={e => setCaptainName(e.target.value)}
                placeholder="e.g. Mira Veskov"
                required
              />
            </div>

            <div>
              <label className={labelClass}>Cargo Items</label>
              <div className="space-y-2">
                {cargoItems.map((item, i) => (
                  <div key={i} className="flex gap-2">
                    <input
                      className={fieldClass}
                      value={item}
                      onChange={e => handleListChange(cargoItems, setCargoItems, i, e.target.value)}
                      placeholder={`Cargo item ${i + 1}`}
                    />
                    {cargoItems.length > 1 && (
                      <button
                        type="button"
                        onClick={() => removeItem(cargoItems, setCargoItems, i)}
                        className="px-2 text-slate-500 hover:text-red-400 transition-colors"
                        aria-label="Remove cargo item"
                      >
                        ✕
                      </button>
                    )}
                  </div>
                ))}
                <button
                  type="button"
                  onClick={() => addItem(cargoItems, setCargoItems)}
                  className="text-xs font-mono text-amber-500/70 hover:text-amber-400 transition-colors"
                >
                  + Add cargo item
                </button>
              </div>
            </div>

            <div>
              <label className={labelClass}>Passengers</label>
              <div className="space-y-2">
                {passengers.map((p, i) => (
                  <div key={i} className="flex gap-2">
                    <input
                      className={fieldClass}
                      value={p}
                      onChange={e => handleListChange(passengers, setPassengers, i, e.target.value)}
                      placeholder={`Passenger ${i + 1}`}
                    />
                    {passengers.length > 1 && (
                      <button
                        type="button"
                        onClick={() => removeItem(passengers, setPassengers, i)}
                        className="px-2 text-slate-500 hover:text-red-400 transition-colors"
                        aria-label="Remove passenger"
                      >
                        ✕
                      </button>
                    )}
                  </div>
                ))}
                <button
                  type="button"
                  onClick={() => addItem(passengers, setPassengers)}
                  className="text-xs font-mono text-amber-500/70 hover:text-amber-400 transition-colors"
                >
                  + Add passenger
                </button>
              </div>
            </div>

            <div className="pt-2">
              <ColdStartNotice
                slow={slow}
                error={error}
                onRetry={() => {}}
                accentClass="border-amber-600 text-amber-400"
              />
              <button
                type="submit"
                disabled={loading}
                className="mt-3 px-6 py-2 bg-amber-600 hover:bg-amber-500 disabled:opacity-50 disabled:cursor-not-allowed rounded font-mono font-bold text-slate-950 text-sm tracking-wide transition-colors"
              >
                {loading ? 'Transmitting…' : 'Submit Manifest'}
              </button>
            </div>
          </form>
        )}
      </div>
    </Layout>
  )
}
