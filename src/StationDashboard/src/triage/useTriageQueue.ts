import { useState, useCallback, useRef, useEffect } from 'react'

const BASE = import.meta.env.VITE_TRIAGE_API_URL as string

type Queue = 'security' | 'medical' | 'hazmat'
type Status = 'NEW' | 'IN_PROGRESS' | 'RESOLVED'

export interface TriageItem {
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
}

interface UseTriageQueue {
  items: TriageItem[]
  loading: boolean
  slow: boolean
  error: string | null
  reload: () => void
  updateStatus: (id: number, status: Status) => Promise<void>
}

export function useTriageQueue(queue: Queue): UseTriageQueue {
  const [items, setItems] = useState<TriageItem[]>([])
  const [loading, setLoading] = useState(true)
  const [slow, setSlow] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const slowTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    setSlow(false)

    slowTimer.current = setTimeout(() => setSlow(true), 5000)

    try {
      const res = await fetch(`${BASE}/${queue}/`)
      if (!res.ok) throw new Error(`Server returned ${res.status}`)
      const json: TriageItem[] = await res.json()
      setItems(json)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load queue')
    } finally {
      if (slowTimer.current) clearTimeout(slowTimer.current)
      setLoading(false)
      setSlow(false)
    }
  }, [queue])

  useEffect(() => { load() }, [load])

  const statusField: Record<Queue, string> = {
    security: 'security_status',
    medical: 'medical_status',
    hazmat: 'hazmat_status',
  }

  const updateStatus = useCallback(
    async (id: number, status: Status) => {
      try {
        const res = await fetch(`${BASE}/${queue}/${id}/`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ [statusField[queue]]: status }),
        })
        if (!res.ok) throw new Error(`Server returned ${res.status}`)
        setItems(prev =>
          prev.map(item =>
            item.id === id ? { ...item, [statusField[queue]]: status } : item,
          ),
        )
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Status update failed')
      }
    },
    [queue],
  )

  return { items, loading, slow, error, reload: load, updateStatus }
}
