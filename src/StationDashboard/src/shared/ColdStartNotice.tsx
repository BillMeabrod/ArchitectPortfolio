interface ColdStartNoticeProps {
  slow: boolean
  error: string | null
  onRetry: () => void
  accentClass: string
  textClass: string
}

export default function ColdStartNotice({ slow, error, onRetry, accentClass, textClass }: ColdStartNoticeProps) {
  if (error) {
    return (
      <div className="rounded border border-red-800 bg-red-950/40 p-4 text-sm">
        <p className="text-red-400 mb-2">{error}</p>
        <button
          onClick={onRetry}
          className={`px-3 py-1 rounded text-xs font-mono border ${accentClass} hover:opacity-80 transition-opacity`}
        >
          Retry
        </button>
      </div>
    )
  }

  if (slow) {
    return (
      <p className={`text-sm opacity-80 italic ${textClass}`}>
        This is a portfolio project running on low-cost infrastructure — the backend may be waking
        up. This can take up to a minute on the first request.
      </p>
    )
  }

  return null
}