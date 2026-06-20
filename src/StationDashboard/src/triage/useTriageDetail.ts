import { useState, useCallback, useRef, useEffect } from 'react'

const BASE = import.meta.env.VITE_TRIAGE_API_URL as string

export type Queue = 'security' | 'medical' | 'hazmat'
type Status = 'NEW' | 'IN_PROGRESS' | 'RESOLVED'

export interface TriageDetail {
  id: number
  ship_name: string
  callsign: string
  captain_name: string
  security_hazard_level?: number
  biohazard_level?: number
  chemical_hazard_level?: number
  security_status?: Status
  medical_status?: Status
  hazmat_status?: Status
  recommendation: string
  cargo_items: unknown[]
  passengers: unknown[]
}

interface UseTriageDetail {
  detail: TriageDetail | null
  loading: boolean
  slow: boolean
  error: string | null
  reload: () => void
  updateStatus: (status: Status) => Promise<void>
}

const statusField: Record<Queue, string> = {
  security: 'security_status',
  medical: 'medical_status',
  hazmat: 'hazmat_status',
}

export function useTriageDetail(queue: Queue, id: string): UseTriageDetail {
  const [detail, setDetail] = useState<TriageDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [slow, setSlow] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const slowTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    setSlow(false)

    if (slowTimer.current) clearTimeout(slowTimer.current)
    slowTimer.current = setTimeout(() => setSlow(true), 5000)

    try {
      const res = await fetch(`${BASE}/${queue}/${id}/`)
      if (!res.ok) throw new Error(`Server returned ${res.status}`)
      const json: TriageDetail = await res.json()
      setDetail(json)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load detail')
    } finally {
      if (slowTimer.current) clearTimeout(slowTimer.current)
      setLoading(false)
      setSlow(false)
    }
  }, [queue, id])

  useEffect(() => { load() }, [load])

  const updateStatus = useCallback(
    async (status: Status) => {
      try {
        const res = await fetch(`${BASE}/${queue}/${id}/`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ [statusField[queue]]: status }),
        })
        if (!res.ok) throw new Error(`Server returned ${res.status}`)
        setDetail(prev =>
          prev ? { ...prev, [statusField[queue]]: status } : prev,
        )
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Status update failed')
      }
    },
    [queue, id],
  )

  return { detail, loading, slow, error, reload: load, updateStatus }
}
