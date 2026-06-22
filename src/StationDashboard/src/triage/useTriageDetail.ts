import { useState, useCallback, useRef, useEffect } from 'react'

const BASE = import.meta.env.VITE_TRIAGE_API_URL as string

export type Queue = 'security' | 'medical' | 'hazmat'
type Status = 'NEW' | 'IN_PROGRESS' | 'RESOLVED'

export interface TriageDetail {
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
  recommendation: string
  cargoItems: string[]
  passengers: string[]
  inappropriateContent: boolean
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function mapApiDetail(raw: any): TriageDetail {
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
    recommendation: raw.recommendation,
    cargoItems: raw.cargo_items,
    passengers: raw.passengers,
    inappropriateContent: raw.inappropriate_content,
  }
}

interface UseTriageDetail {
  detail: TriageDetail | null
  loading: boolean
  slow: boolean
  error: string | null
  reload: () => void
  updateStatus: (status: Status) => Promise<void>
}

const statusFieldApi: Record<Queue, string> = {
  security: 'security_status',
  medical: 'medical_status',
  hazmat: 'hazmat_status',
}

const statusFieldState: Record<Queue, keyof TriageDetail> = {
  security: 'securityStatus',
  medical: 'medicalStatus',
  hazmat: 'hazmatStatus',
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
      const json = await res.json()
      setDetail(mapApiDetail(json))
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
      setError(null)
      try {
        const res = await fetch(`${BASE}/${queue}/${id}/`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ [statusFieldApi[queue]]: status }),
        })
        if (!res.ok) throw new Error(`Server returned ${res.status}`)
        setDetail(prev =>
          prev ? { ...prev, [statusFieldState[queue]]: status } : prev,
        )
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Status update failed')
      }
    },
    [queue, id],
  )

  return { detail, loading, slow, error, reload: load, updateStatus }
}
