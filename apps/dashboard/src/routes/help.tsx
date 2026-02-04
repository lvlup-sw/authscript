import { useState } from 'react';
import { createFileRoute, Link } from '@tanstack/react-router';
import { cn } from '@/lib/utils';
import {
  HelpCircle,
  FileText,
  ClipboardCheck,
  LayoutDashboard,
  ArrowLeft,
} from 'lucide-react';

export const Route = createFileRoute('/help')({
  component: HelpPage,
});

const TABS = [
  { id: 'request-pa', label: 'Request new PA', icon: FileText },
  { id: 'review-pa', label: 'Review & submit PA', icon: ClipboardCheck },
  { id: 'dashboard', label: 'Dashboard overview', icon: LayoutDashboard },
] as const;

type TabId = (typeof TABS)[number]['id'];

function HelpPage() {
  const [activeTab, setActiveTab] = useState<TabId>('request-pa');

  return (
    <div className="p-6 max-w-4xl mx-auto animate-fade-in">
      <div className="flex items-center gap-3 mb-8">
        <div className="w-12 h-12 rounded-xl bg-teal flex items-center justify-center">
          <HelpCircle className="w-6 h-6 text-white" />
        </div>
        <div>
          <h1 className="text-2xl font-bold text-foreground">Help</h1>
          <p className="text-sm text-muted-foreground">
            Step-by-step guides for every flow in the AuthScript dashboard
          </p>
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b border-border mb-8">
        <div className="flex gap-1">
          {TABS.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={cn(
                'flex items-center gap-2 px-4 py-3 text-sm font-medium border-b-2 transition-colors click-effect',
                activeTab === tab.id
                  ? 'border-teal text-teal'
                  : 'border-transparent text-muted-foreground hover:text-foreground hover:border-border'
              )}
            >
              <tab.icon className="w-4 h-4" />
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      {/* Tab content */}
      <div className="bg-card rounded-2xl border shadow-soft p-6 space-y-6">
        {activeTab === 'request-pa' && <RequestNewPAContent />}
        {activeTab === 'review-pa' && <ReviewSubmitPAContent />}
        {activeTab === 'dashboard' && <DashboardOverviewContent />}
      </div>

      <div className="mt-8 flex flex-wrap items-center justify-center gap-4">
        <Link
          to="/"
          className="inline-flex items-center gap-2 px-5 py-2.5 bg-teal text-white rounded-xl font-semibold hover:bg-teal/90 transition-colors shadow-teal click-effect-primary"
        >
          <ArrowLeft className="w-4 h-4" />
          Back to Dashboard
        </Link>
      </div>
    </div>
  );
}

function Step({ number, title, children }: { number: number; title: string; children: React.ReactNode }) {
  return (
    <div className="flex gap-4">
      <div className="w-8 h-8 rounded-full bg-teal/10 text-teal flex items-center justify-center text-sm font-bold flex-shrink-0">
        {number}
      </div>
      <div className="flex-1 min-w-0">
        <h3 className="font-semibold text-foreground mb-2">{title}</h3>
        <div className="text-sm text-muted-foreground space-y-2">{children}</div>
      </div>
    </div>
  );
}

function RequestNewPAContent() {
  return (
    <>
      <div>
        <h2 className="text-lg font-bold text-foreground mb-2">Request new PA</h2>
        <p className="text-sm text-muted-foreground">
          Create a new prior authorization request by choosing the patient, service (procedure or medication), and diagnosis. The AI then generates the PA form.
        </p>
      </div>

      <div className="space-y-6">
        <Step
          number={1}
          title="Open the New PA modal"
        >
          <p>From the dashboard, click <strong>New PA Request</strong> in the header banner or <strong>New Request</strong> in the requests section. The modal opens with a short loading effect between steps.</p>
        </Step>

        <Step
          number={2}
          title="Select a patient"
        >
          <p>Use the search box to find a patient by name or MRN. Click a patient row to select them. You’ll see a brief loading state, then the next step.</p>
          <ul className="list-disc list-inside space-y-1 mt-2">
            <li>Each row shows name, MRN, DOB, and payer.</li>
            <li>Use <strong>Back</strong> at the bottom to return to the dashboard or go to the previous step.</li>
          </ul>
        </Step>

        <Step
          number={3}
          title="Select procedure or medication"
        >
          <p>Switch between <strong>Procedure</strong> and <strong>Medication</strong> with the tabs. Search or scroll to find the service, then click it to continue.</p>
          <ul className="list-disc list-inside space-y-1 mt-2">
            <li>Procedures show code, name, and category (e.g. surgery).</li>
            <li>Medications show drug name and whether PA is required.</li>
          </ul>
        </Step>

        <Step
          number={4}
          title="Select diagnosis"
        >
          <p>Search or browse the diagnosis list. Click a diagnosis (code and name) to confirm. A short loading overlay appears, then processing starts.</p>
        </Step>

        <Step
          number={5}
          title="Processing and completion"
        >
          <p>The AI runs through: reading notes, analyzing medical necessity, mapping to payer requirements, and generating the PA form. When done, you can <strong>Review request</strong> to open the review page or <strong>Close</strong> to stay on the dashboard.</p>
        </Step>
      </div>
    </>
  );
}

function ReviewSubmitPAContent() {
  return (
    <>
      <div>
        <h2 className="text-lg font-bold text-foreground mb-2">Review & submit PA</h2>
        <p className="text-sm text-muted-foreground">
          On the analysis page you can review the AI-generated prior auth, edit fields and criteria, then submit or download a PDF.
        </p>
      </div>

      <div className="space-y-6">
        <Step
          number={1}
          title="Open a request"
        >
          <p>From the dashboard, click <strong>Review</strong> on a ready request or <strong>View</strong> on a processing one. You can also use the request row (patient name, procedure code, payer) as a link.</p>
        </Step>

        <Step
          number={2}
          title="Understand the layout"
        >
          <p>Top: request ID and optional <strong>Low confidence</strong> banner (manual review suggested). Main area: patient and service details, diagnosis, place of service, clinical summary, and payer criteria. Sidebar: AI confidence ring and quick actions.</p>
        </Step>

        <Step
          number={3}
          title="Edit details (optional)"
        >
          <p>Click <strong>Edit</strong>. You can change diagnosis, service date, place of service, and clinical summary. For criteria (met / not met / unclear), click a criterion to cycle its status. Click <strong>Save</strong> to apply or <strong>Cancel</strong> to discard.</p>
        </Step>

        <Step
          number={4}
          title="Print or download PDF"
        >
          <p>Use <strong>Print</strong> or <strong>Download PDF</strong> to open the PA form in a new window for printing or saving. You can do this before or after submitting.</p>
        </Step>

        <Step
          number={5}
          title="Submit to payer"
        >
          <p>When everything looks correct, click <strong>Confirm & Submit</strong>. After the loading state, the request moves to submitted. Use <strong>Back to Dashboard</strong> to return.</p>
        </Step>
      </div>
    </>
  );
}

function DashboardOverviewContent() {
  return (
    <>
      <div>
        <h2 className="text-lg font-bold text-foreground mb-2">Dashboard overview</h2>
        <p className="text-sm text-muted-foreground">
          The dashboard shows your PA workload, pipeline status, request list, and recent activity.
        </p>
      </div>

      <div className="space-y-6">
        <div>
          <h3 className="font-semibold text-foreground mb-2">Header and search</h3>
          <p className="text-sm text-muted-foreground">
            The top bar has the AuthScript logo, a search field for patients and PA requests (with shortcut hint), status (e.g. Live), and icons for notifications, help, and settings.
          </p>
        </div>

        <div>
          <h3 className="font-semibold text-foreground mb-2">Stats row</h3>
          <ul className="text-sm text-muted-foreground space-y-2">
            <li><strong>Ready for Review</strong> — PAs ready for you to review and submit.</li>
            <li><strong>Processing</strong> — PAs currently being analyzed by the AI.</li>
            <li><strong>Completed Today</strong> — Submitted (or approved) count for today.</li>
            <li><strong>Needs Attention</strong> — Ready PAs with low AI confidence; review these first.</li>
          </ul>
        </div>

        <div>
          <h3 className="font-semibold text-foreground mb-2">Current pipeline status</h3>
          <p className="text-sm text-muted-foreground mb-2">
            The horizontal pipeline shows stages: Signed → Detected → Processing → Ready → Submitted. Completed steps are green; the current stage is highlighted. Counts for “in progress” and “completed today” appear next to it.
          </p>
        </div>

        <div>
          <h3 className="font-semibold text-foreground mb-2">Tabs and request list</h3>
          <ul className="text-sm text-muted-foreground space-y-2">
            <li><strong>Pending Review</strong> — Ready PAs (with count).</li>
            <li><strong>Processing</strong> — PAs still being analyzed.</li>
            <li><strong>History</strong> — Submitted, approved, or denied.</li>
            <li><strong>Analytics</strong> — Overview of all requests.</li>
          </ul>
          <p className="text-sm text-muted-foreground mt-2">
            Each row shows patient, procedure code, payer, time, AI confidence %, and status. Rows with low confidence have an amber “Low confidence” badge. Click a row or <strong>Review</strong> / <strong>View</strong> to open the analysis page.
          </p>
        </div>

        <div>
          <h3 className="font-semibold text-foreground mb-2">Recent activity & weekly summary</h3>
          <p className="text-sm text-muted-foreground">
            <strong>Recent Activity</strong> lists recent events (e.g. submitted, ready, processing) with patient and procedure. <strong>This Week</strong> shows totals: processed count, average processing time, average AI confidence, and success rate.
          </p>
        </div>

        <div>
          <h3 className="font-semibold text-foreground mb-2">Footer</h3>
          <p className="text-sm text-muted-foreground">
            The footer indicates that the system is monitoring athenahealth for signed encounters and shows the last check time and interval.
          </p>
        </div>
      </div>
    </>
  );
}
