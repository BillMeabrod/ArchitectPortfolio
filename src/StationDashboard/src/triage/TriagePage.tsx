import { useState } from 'react'
import Layout from '../shared/Layout'
import { SecurityQueue, MedicalQueue, HazmatQueue } from './TriageQueues'

type Tab = 'security' | 'medical' | 'hazmat'

const tabs: { id: Tab; label: string; accent: string }[] = [
  { id: 'security', label: 'Security', accent: 'border-red-600 text-red-400' },
  { id: 'medical', label: 'Medical', accent: 'border-green-600 text-green-400' },
  { id: 'hazmat', label: 'Hazmat', accent: 'border-orange-600 text-orange-400' },
]

export default function TriagePage() {
  const [activeTab, setActiveTab] = useState<Tab>('security')

  return (
    <Layout zoneName="Station Triage" headerClass="bg-gray-950 text-gray-100 border-b border-gray-800">
      <div className="max-w-3xl">
        <div className="mb-6">
          <h1 className="text-xl font-mono font-bold text-red-400 tracking-wide uppercase">
            Threat Assessment Board
          </h1>
          <p className="text-gray-500 text-sm mt-1 font-mono">
            Live queues populated by ARIA risk assessments. Manage status by role.
          </p>
        </div>

        <div className="flex gap-1 mb-6 border-b border-gray-800 pb-0">
          {tabs.map(tab => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`px-5 py-2 text-sm font-mono font-semibold border-b-2 transition-colors ${
                activeTab === tab.id
                  ? `${tab.accent} bg-gray-900/50`
                  : 'border-transparent text-gray-600 hover:text-gray-400'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <div>
          {activeTab === 'security' && <SecurityQueue />}
          {activeTab === 'medical' && <MedicalQueue />}
          {activeTab === 'hazmat' && <HazmatQueue />}
        </div>
      </div>
    </Layout>
  )
}
