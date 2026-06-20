import { Routes, Route } from 'react-router-dom'
import LandingPage from './landing/LandingPage'
import ManifestsPage from './manifests/ManifestsPage'
import AiConsolePage from './ai-console/AiConsolePage'
import TriagePage from './triage/TriagePage'
import TriageDetailPage from './triage/TriageDetailPage'

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/manifests" element={<ManifestsPage />} />
      <Route path="/ai-console" element={<AiConsolePage />} />
      <Route path="/triage" element={<TriagePage />} />
      <Route path="/triage/:queue/:id" element={<TriageDetailPage />} />
    </Routes>
  )
}
