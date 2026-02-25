import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { EhrHeader } from '../EhrHeader';

describe('EhrHeader', () => {
  it('EhrHeader_Render_ShowsAthenaNavAndPatientBanner', () => {
    render(
      <EhrHeader
        patient={{ name: 'Maria Garcia', dob: '03/15/1981', mrn: '60182' }}
      />,
    );

    expect(screen.getByText('athenaOne')).toBeInTheDocument();
    expect(screen.getByText('Maria Garcia')).toBeInTheDocument();
    expect(screen.getByText('03/15/1981')).toBeInTheDocument();
    expect(screen.getByText('60182')).toBeInTheDocument();
    expect(screen.getByText('Charts')).toBeInTheDocument();
    expect(screen.getByText('Schedule')).toBeInTheDocument();
    expect(screen.getByText('Messages')).toBeInTheDocument();
  });
});
