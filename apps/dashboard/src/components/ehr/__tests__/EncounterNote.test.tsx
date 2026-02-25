import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { EncounterNote } from '../EncounterNote';

describe('EncounterNote', () => {
  it('EncounterNote_Render_ShowsClinicalSections', () => {
    render(
      <EncounterNote
        encounter={{
          cc: 'Chronic lower back pain with radiculopathy',
          hpi: '45yo F presents with worsening lower back pain radiating to left leg',
          assessment: 'Lumbar radiculopathy',
          plan: 'Order MRI lumbar spine w/o contrast',
        }}
      />,
    );

    expect(screen.getByText('Chief Complaint')).toBeInTheDocument();
    expect(
      screen.getByText('Chronic lower back pain with radiculopathy'),
    ).toBeInTheDocument();
    expect(
      screen.getByText('History of Present Illness'),
    ).toBeInTheDocument();
    expect(screen.getByText('Assessment')).toBeInTheDocument();
    expect(screen.getByText('Plan')).toBeInTheDocument();
    expect(
      screen.getByText('Order MRI lumbar spine w/o contrast'),
    ).toBeInTheDocument();
  });
});
