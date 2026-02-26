import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { EhrHeader } from '../EhrHeader';

describe('EhrHeader', () => {
  it('EhrHeader_Render_ShowsAthenaNavAndPatientBanner', () => {
    render(
      <EhrHeader patient={{ name: 'Maria Garcia', dob: '03/15/1981', mrn: '60182' }} />,
    );
    expect(screen.getByText('athenaOne')).toBeInTheDocument();
    expect(screen.getByText('Maria Garcia')).toBeInTheDocument();
    expect(screen.getByText('03/15/1981')).toBeInTheDocument();
    expect(screen.getByText('60182')).toBeInTheDocument();
    expect(screen.getByText('Charts')).toBeInTheDocument();
    expect(screen.getByText('Schedule')).toBeInTheDocument();
    expect(screen.getByText('Messages')).toBeInTheDocument();
  });

  it('EhrHeader_Metadata_RendersProviderAndDate', () => {
    render(
      <EhrHeader
        patient={{ name: 'Test Patient', dob: '01/01/2000', mrn: 'MRN-001' }}
        encounterMeta={{
          provider: 'Dr. Kelli Smith',
          specialty: 'Family Medicine',
          date: '02/25/2026',
          type: 'Office Visit',
        }}
      />,
    );
    expect(screen.getByText('Dr. Kelli Smith')).toBeInTheDocument();
    expect(screen.getByText('Family Medicine')).toBeInTheDocument();
    expect(screen.getByText('02/25/2026')).toBeInTheDocument();
    expect(screen.getByText('Office Visit')).toBeInTheDocument();
  });

  it('EhrHeader_NoMetadata_NoExtraRow', () => {
    render(
      <EhrHeader patient={{ name: 'Test', dob: '01/01/2000', mrn: 'MRN-001' }} />,
    );
    expect(screen.queryByText('Family Medicine')).not.toBeInTheDocument();
  });
});
