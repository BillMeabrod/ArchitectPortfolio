import { useState } from 'react'
import Layout from '../shared/Layout'
import { SecurityQueue, MedicalQueue, HazmatQueue } from './TriageQueues'

type Tab = 'security' | 'medical' | 'hazmat'

const tabs: { id: Tab; label: string; activeClass: string }[] = [
  { id: 'security', label: 'Security', activeClass: 'border-red-500 text-red-400' },
  { id: 'medical', label: 'Medical', activeClass: 'border-green-500 text-green-400' },
  { id: 'hazmat', label: 'Hazmat', activeClass: 'border-orange-500 text-orange-400' },
]

export default function TriagePage() {
  const [activeTab, setActiveTab] = useState<Tab>('security')

  return (
    <Layout
      zoneName="Station Triage"
      headerClass="bg-[#0a0e14] text-[#e6f1fb] border-b border-[#85b7eb]"
      pageClass="bg-[#0a0e14]"
    >
      <div className="max-w-3xl">
        <div className="mb-6">
          <h1 className="text-xl font-mono font-bold text-[#e6f1fb] tracking-wide uppercase">
            Threat Assessment Board
          </h1>
          <p className="text-[#5f5e5a] text-sm mt-1 font-mono">
            Live queues populated by ARIA risk assessments. Manage status by role.
          </p>
        </div>

        <div className="flex gap-1 mb-6 border-b border-[#85b7eb]/20 pb-0">
          {tabs.map(tab => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`px-5 py-2 text-sm font-mono font-semibold border-b-2 transition-colors ${
                activeTab === tab.id
                  ? `${tab.activeClass} bg-[#11161f]/80`
                  : 'border-transparent text-[#5f5e5a] hover:text-[#85b7eb]'
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
