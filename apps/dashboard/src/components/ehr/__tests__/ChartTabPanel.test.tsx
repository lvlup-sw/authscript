import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ChartTabPanel } from '../ChartTabPanel';
import { DEMO_CHART_DATA } from '@/lib/demoData';

describe('ChartTabPanel', () => {
  it('ChartTabPanel_Problems_RendersICDCodes', () => {
    render(<ChartTabPanel activeTab="problems" chartData={DEMO_CHART_DATA} />);

    expect(screen.getByText('M54.5')).toBeInTheDocument();
    expect(screen.getByText('M54.41')).toBeInTheDocument();
    expect(screen.getByText('Low back pain')).toBeInTheDocument();
    expect(screen.getByText(/Lumbago with sciatica/)).toBeInTheDocument();
  });

  it('ChartTabPanel_Meds_RendersMedicationList', () => {
    render(<ChartTabPanel activeTab="medications" chartData={DEMO_CHART_DATA} />);

    expect(screen.getByText('Ibuprofen')).toBeInTheDocument();
    expect(screen.getByText('Cyclobenzaprine')).toBeInTheDocument();
    expect(screen.getByText('Gabapentin')).toBeInTheDocument();
  });

  it('ChartTabPanel_Allergies_RendersNKDA', () => {
    render(<ChartTabPanel activeTab="allergies" chartData={DEMO_CHART_DATA} />);

    expect(screen.getByText('No Known Drug Allergies')).toBeInTheDocument();
  });

  it('ChartTabPanel_Imaging_RendersNoHistory', () => {
    render(<ChartTabPanel activeTab="imaging" chartData={DEMO_CHART_DATA} />);

    expect(screen.getByText('No prior lumbar imaging')).toBeInTheDocument();
  });
});
