import { useState, useCallback, useRef, useEffect } from 'react'

const BASE = import.meta.env.VITE_AI_API_URL as string

interface StationDirective {
  coreDirective: string
  stationDirective: string
}

interface UseStationDirective {
  data: StationDirective | null
  loading: boolean
  slow: boolean
  fetchError: string | null
  saveLoading: boolean
  saveError: string | null
  saveSuccess: boolean
  reload: () => void
  save: (directive: string) => Promise<void>
}

export function useStationDirective(): UseStationDirective {
  const [data, setData] = useState<StationDirective | null>(null)
  const [loading, setLoading] = useState(true)
  const [slow, setSlow] = useState(false)
  const [fetchError, setFetchError] = useState<string | null>(null)
  const [saveLoading, setSaveLoading] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [saveSuccess, setSaveSuccess] = useState(false)
  const slowTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setFetchError(null)
    setSlow(false)

    if (slowTimer.current) clearTimeout(slowTimer.current)
    slowTimer.current = setTimeout(() => setSlow(true), 5000)

    try {
      const res = await fetch(`${BASE}/api/stationdirective`)
      if (!res.ok) throw new Error(`Server returned ${res.status}`)
      const json: StationDirective = await res.json()
      setData(json)
    } catch (e) {
      setFetchError(e instanceof Error ? e.message : 'Failed to load station directive')
    } finally {
      if (slowTimer.current) clearTimeout(slowTimer.current)
      setLoading(false)
      setSlow(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  const save = useCallback(async (directive: string) => {
    setSaveLoading(true)
    setSaveError(null)
    setSaveSuccess(false)
    try {
      const res = await fetch(`${BASE}/api/stationdirective`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ directive }),
      })
      if (!res.ok) throw new Error(`Server returned ${res.status}`)
      setSaveSuccess(true)
      setTimeout(() => setSaveSuccess(false), 3000)
    } catch (e) {
      setSaveError(e instanceof Error ? e.message : 'Save failed')
    } finally {
      setSaveLoading(false)
    }
  }, [])

  return { data, loading, slow, fetchError, saveLoading, saveError, saveSuccess, reload: load, save }
}