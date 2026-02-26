import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { EncounterNote } from '../EncounterNote';

const encounter = {
  cc: 'Chronic lower back pain with radiculopathy',
  hpi: '45yo F presents with worsening lower back pain radiating to left leg',
  assessment: 'Lumbar radiculopathy',
  plan: 'Order MRI lumbar spine w/o contrast',
};

describe('EncounterNote', () => {
  it('EncounterNote_Render_ShowsClinicalSections', () => {
    render(<EncounterNote encounter={encounter} />);

    expect(screen.getByText('Chief Complaint')).toBeInTheDocument();
    expect(
      screen.getByText('Chronic lower back pain with radiculopathy'),
    ).toBeInTheDocument();
    expect(screen.getByText('History of Present Illness')).toBeInTheDocument();
    expect(screen.getByText('Assessment')).toBeInTheDocument();
    expect(screen.getByText('Plan')).toBeInTheDocument();
    expect(
      screen.getByText('Order MRI lumbar spine w/o contrast'),
    ).toBeInTheDocument();
  });

  it('EncounterNote_CcHpi_CollapsedByDefault', () => {
    render(
      <EncounterNote
        encounter={encounter}
        vitals={{ bp: '128/82', hr: 72, temp: 98.6, spo2: 99 }}
      />,
    );
    const ccDetails = screen
      .getByText('Chief Complaint / HPI')
      .closest('details');
    expect(ccDetails).not.toHaveAttribute('open');
  });

  it('EncounterNote_AssessmentPlan_ExpandedByDefault', () => {
    render(
      <EncounterNote
        encounter={encounter}
        vitals={{ bp: '128/82', hr: 72, temp: 98.6, spo2: 99 }}
      />,
    );
    const apDetails = screen
      .getByText('Assessment & Plan')
      .closest('details');
    expect(apDetails).toHaveAttribute('open');
  });

  it('EncounterNote_Vitals_RendersValues', () => {
    render(
      <EncounterNote
        encounter={encounter}
        vitals={{ bp: '128/82', hr: 72, temp: 98.6, spo2: 99 }}
      />,
    );
    expect(screen.getByText('128/82')).toBeInTheDocument();
    expect(screen.getByText('72')).toBeInTheDocument();
    expect(screen.getByText('98.6\u00B0F')).toBeInTheDocument();
    expect(screen.getByText('99%')).toBeInTheDocument();
  });

  it('EncounterNote_Orders_ShowsPaBadge', () => {
    render(
      <EncounterNote
        encounter={encounter}
        orders={[
          {
            code: '72148',
            name: 'MRI Lumbar Spine w/o Contrast',
            status: 'requires-pa',
          },
        ]}
      />,
    );
    expect(screen.getByText('72148')).toBeInTheDocument();
    expect(
      screen.getByText('MRI Lumbar Spine w/o Contrast'),
    ).toBeInTheDocument();
    expect(screen.getByText('Requires PA')).toBeInTheDocument();
  });
});
