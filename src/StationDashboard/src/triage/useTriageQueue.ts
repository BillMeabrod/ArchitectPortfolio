import { useState, useCallback, useRef, useEffect } from 'react'

const BASE = import.meta.env.VITE_TRIAGE_API_URL as string

type Queue = 'security' | 'medical' | 'hazmat'
type Status = 'NEW' | 'IN_PROGRESS' | 'RESOLVED'

export interface TriageItem {
  id: number
  shipName: string
  callsign: string
  captainName: string
  securityHazardLevel?: number
  biohazardLevel?: number
  chemicalHazardLevel?: number
  securityStatus?: Status
  medicalStatus?: Status
  hazmatStatus?: Status
  inappropriateContent: boolean
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function mapApiItem(raw: any): TriageItem {
  return {
    id: raw.id,
    shipName: raw.ship_name,
    callsign: raw.callsign,
    captainName: raw.captain_name,
    securityHazardLevel: raw.security_hazard_level,
    biohazardLevel: raw.biohazard_level,
    chemicalHazardLevel: raw.chemical_hazard_level,
    securityStatus: raw.security_status,
    medicalStatus: raw.medical_status,
    hazmatStatus: raw.hazmat_status,
    inappropriateContent: raw.inappropriate_content,
  }
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

    if (slowTimer.current) clearTimeout(slowTimer.current)
    slowTimer.current = setTimeout(() => setSlow(true), 5000)

    try {
      const res = await fetch(`${BASE}/${queue}/`)
      if (!res.ok) throw new Error(`Server returned ${res.status}`)
      const json = await res.json()
      setItems(json.map(mapApiItem))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load queue')
    } finally {
      if (slowTimer.current) clearTimeout(slowTimer.current)
      setLoading(false)
      setSlow(false)
    }
  }, [queue])

  useEffect(() => { load() }, [load])

  const statusFieldApi: Record<Queue, string> = {
    security: 'security_status',
    medical: 'medical_status',
    hazmat: 'hazmat_status',
  }

  const statusFieldState: Record<Queue, keyof TriageItem> = {
    security: 'securityStatus',
    medical: 'medicalStatus',
    hazmat: 'hazmatStatus',
  }

  const updateStatus = useCallback(
    async (id: number, status: Status) => {
      try {
        const res = await fetch(`${BASE}/${queue}/${id}/`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ [statusFieldApi[queue]]: status }),
        })
        if (!res.ok) throw new Error(`Server returned ${res.status}`)
        setItems(prev =>
          prev.map(item =>
            item.id === id ? { ...item, [statusFieldState[queue]]: status } : item,
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
