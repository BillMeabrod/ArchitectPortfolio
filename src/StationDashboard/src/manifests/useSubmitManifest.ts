import { useState, useCallback, useRef } from 'react'

const BASE = import.meta.env.VITE_MANIFEST_API_URL as string

interface SubmitResult {
  auditId: string
}

interface UseSubmitManifest {
  submit: (body: object) => Promise<void>
  result: SubmitResult | null
  loading: boolean
  slow: boolean
  error: string | null
  reset: () => void
}

export function useSubmitManifest(): UseSubmitManifest {
  const [result, setResult] = useState<SubmitResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [slow, setSlow] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const slowTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  const submit = useCallback(async (body: object) => {
    setLoading(true)
    setError(null)
    setSlow(false)
    setResult(null)

    if (slowTimer.current) clearTimeout(slowTimer.current)
    slowTimer.current = setTimeout(() => setSlow(true), 5000)

    try {
      const res = await fetch(`${BASE}/api/shipmanifests/submit-manifest`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      })
      if (!res.ok) throw new Error(`Server returned ${res.status}`)
      const text = await res.text()
      const match = text.match(/Audit ID (\d+)/)
      setResult({ auditId: match ? match[1] : text })
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Submission failed')
    } finally {
      if (slowTimer.current) clearTimeout(slowTimer.current)
      setLoading(false)
      setSlow(false)
    }
  }, [])

  const reset = useCallback(() => {
    setResult(null)
    setError(null)
  }, [])

  return { submit, result, loading, slow, error, reset }
}
