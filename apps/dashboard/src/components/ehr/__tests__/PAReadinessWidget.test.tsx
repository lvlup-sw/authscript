import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { PAReadinessWidget } from '../PAReadinessWidget';
import type { PreCheckCriterion } from '@/lib/demoData';

function buildCriteria(): PreCheckCriterion[] {
  return [
    {
      label: 'Valid ICD-10 for lumbar pathology',
      status: 'met',
      evidence: 'M54.5 on active problem list since 2025-09-12',
      source: 'Problem List',
    },
    {
      label: 'Red flag symptoms or progressive neurological deficit',
      status: 'met',
      evidence: 'Progressive numbness in left foot over past 3 weeks.',
      source: 'CC / HPI',
    },
    {
      label: '4+ weeks conservative management',
      status: 'met',
      evidence: 'Failed 8 weeks of physical therapy and 6 weeks of NSAIDs.',
      source: 'HPI / Orders',
    },
    {
      label: 'Clinical rationale documented',
      status: 'met',
      evidence: 'Progressive neurological symptoms warrant advanced imaging.',
      source: 'Assessment',
    },
    {
      label: 'No recent duplicative imaging',
      status: 'met',
      evidence: 'No lumbar CT or MRI in past 12 months',
      source: 'Imaging Hx',
    },
  ];
}

const defaultOrder = { code: '72148', name: 'MRI Lumbar Spine w/o Contrast' };
const defaultPayer = 'Blue Cross Blue Shield';
const defaultPolicyId = 'LCD L34220';

describe('PAReadinessWidget', () => {
  it('PAReadinessWidget_Checking_ShowsAnalyzingState', () => {
    render(
      <PAReadinessWidget
        state="checking"
        criteria={[]}
        order={defaultOrder}
        payer={defaultPayer}
        policyId={defaultPolicyId}
      />,
    );

    expect(
      screen.getByText(/analyzing patient chart against payer policy/i),
    ).toBeInTheDocument();

    // Loading indicator (Loader2 renders as an svg)
    const spinner = document.querySelector('.animate-spin');
    expect(spinner).toBeInTheDocument();
  });

  it('PAReadinessWidget_Ready_ShowsSummaryBar', () => {
    const criteria = buildCriteria();

    render(
      <PAReadinessWidget
        state="ready"
        criteria={criteria}
        order={defaultOrder}
        payer={defaultPayer}
        policyId={defaultPolicyId}
      />,
    );

    // Order name
    expect(screen.getByText(/MRI Lumbar Spine/)).toBeInTheDocument();

    // Payer
    expect(screen.getByText(/Blue Cross Blue Shield/)).toBeInTheDocument();

    // Policy ID in monospace/uppercase
    const policyEl = screen.getByText('LCD L34220');
    expect(policyEl).toBeInTheDocument();
    expect(policyEl.className).toMatch(/font-mono/);
    expect(policyEl.className).toMatch(/uppercase/);

    // Criteria count: 5 met out of 5
    expect(screen.getByText(/5\/5 criteria documented/)).toBeInTheDocument();
  });

  it('PAReadinessWidget_Ready_ShowsAllCriteria', () => {
    const criteria = buildCriteria();

    render(
      <PAReadinessWidget
        state="ready"
        criteria={criteria}
        order={defaultOrder}
        payer={defaultPayer}
        policyId={defaultPolicyId}
      />,
    );

    expect(screen.getByText('Valid ICD-10 for lumbar pathology')).toBeInTheDocument();
    expect(
      screen.getByText('Red flag symptoms or progressive neurological deficit'),
    ).toBeInTheDocument();
    expect(screen.getByText('4+ weeks conservative management')).toBeInTheDocument();
    expect(screen.getByText('Clinical rationale documented')).toBeInTheDocument();
    expect(screen.getByText('No recent duplicative imaging')).toBeInTheDocument();
  });

  it('PAReadinessWidget_Met_ShowsEvidenceAndSource', () => {
    const criteria = buildCriteria();

    render(
      <PAReadinessWidget
        state="ready"
        criteria={criteria}
        order={defaultOrder}
        payer={defaultPayer}
        policyId={defaultPolicyId}
      />,
    );

    // Evidence text in italic
    const evidenceEl = screen.getByText(
      'M54.5 on active problem list since 2025-09-12',
    );
    expect(evidenceEl).toBeInTheDocument();
    expect(evidenceEl.className).toMatch(/italic/);

    // Source tag
    const sourceEl = screen.getByText('Problem List');
    expect(sourceEl).toBeInTheDocument();
    expect(sourceEl.className).toMatch(/font-mono/);
  });

  it('PAReadinessWidget_Indeterminate_ShowsGap', () => {
    const criteria: PreCheckCriterion[] = [
      { label: 'Test criterion', status: 'indeterminate', gap: 'Missing documentation' },
    ];

    render(
      <PAReadinessWidget
        state="ready"
        criteria={criteria}
        order={defaultOrder}
        payer={defaultPayer}
        policyId={defaultPolicyId}
      />,
    );

    const gapEl = screen.getByText(/Missing documentation/);
    expect(gapEl).toBeInTheDocument();
    expect(gapEl.className).toMatch(/text-amber-600/);
  });

  it('PAReadinessWidget_CriterionClick_CallsHandler', () => {
    const criteria = buildCriteria();
    const onCriterionClick = vi.fn();

    render(
      <PAReadinessWidget
        state="ready"
        criteria={criteria}
        order={defaultOrder}
        payer={defaultPayer}
        policyId={defaultPolicyId}
        onCriterionClick={onCriterionClick}
      />,
    );

    fireEvent.click(screen.getByText('Valid ICD-10 for lumbar pathology'));

    expect(onCriterionClick).toHaveBeenCalledWith(criteria[0]);
  });

  it('PAReadinessWidget_AllMet_HasEmeraldAccent', () => {
    const criteria = buildCriteria();

    const { container } = render(
      <PAReadinessWidget
        state="ready"
        criteria={criteria}
        order={defaultOrder}
        payer={defaultPayer}
        policyId={defaultPolicyId}
      />,
    );

    const widget = container.firstElementChild;
    // All 5 criteria met → emerald border
    expect(widget?.className).toMatch(/border-emerald-500/);
  });

  it('PAReadinessWidget_PartialMet_HasAmberAccent', () => {
    const criteria: PreCheckCriterion[] = [
      { label: 'Met criterion', status: 'met', evidence: 'Found', source: 'HPI' },
      { label: 'Missing criterion', status: 'indeterminate', gap: 'Not documented' },
    ];

    const { container } = render(
      <PAReadinessWidget
        state="ready"
        criteria={criteria}
        order={defaultOrder}
        payer={defaultPayer}
        policyId={defaultPolicyId}
      />,
    );

    const widget = container.firstElementChild;
    expect(widget?.className).toMatch(/border-amber-500/);
  });

  it('PAReadinessWidget_Indeterminate_ShowsDocumentLink', () => {
    const criteria: PreCheckCriterion[] = [
      { label: 'Test criterion', status: 'indeterminate', gap: 'Missing documentation' },
    ];
    const onGapAction = vi.fn();

    render(
      <PAReadinessWidget
        state="ready"
        criteria={criteria}
        order={defaultOrder}
        payer={defaultPayer}
        policyId={defaultPolicyId}
        onGapAction={onGapAction}
        docState="idle"
      />,
    );

    const docLink = screen.getByText('Document');
    expect(docLink).toBeInTheDocument();
    fireEvent.click(docLink);
    expect(onGapAction).toHaveBeenCalled();
  });
});
