import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import {
  Container,
  ScrollContainer,
  ScrollPage,
  FullScreenContainer,
  HeadlessScroll,
} from '../Containers';

describe('Container', () => {
  it('Container_RendersChildren_WithDefaultStyles', () => {
    render(<Container>Test content</Container>);
    expect(screen.getByText('Test content')).toBeInTheDocument();
  });

  it('Container_AcceptsClassName_MergesWithDefaults', () => {
    const { container } = render(
      <Container className="custom-class">Content</Container>
    );
    expect(container.firstChild).toHaveClass('custom-class');
  });

  it('Container_HasBackgroundAndPadding_ByDefault', () => {
    const { container } = render(<Container>Content</Container>);
    expect(container.firstChild).toHaveClass('bg-card');
    expect(container.firstChild).toHaveClass('p-6');
  });
});

describe('ScrollContainer', () => {
  it('ScrollContainer_RendersChildren_WithScrollStyles', () => {
    render(<ScrollContainer>Scrollable content</ScrollContainer>);
    expect(screen.getByText('Scrollable content')).toBeInTheDocument();
  });

  it('ScrollContainer_HasOverflowAuto_ForScrolling', () => {
    const { container } = render(
      <ScrollContainer>Scrollable content</ScrollContainer>
    );
    expect(container.firstChild).toHaveClass('overflow-y-auto');
  });

  it('ScrollContainer_AcceptsClassName_MergesWithDefaults', () => {
    const { container } = render(
      <ScrollContainer className="extra-class">Content</ScrollContainer>
    );
    expect(container.firstChild).toHaveClass('extra-class');
    expect(container.firstChild).toHaveClass('overflow-y-auto');
  });
});

describe('ScrollPage', () => {
  it('ScrollPage_RendersChildren_WithPageStyles', () => {
    render(<ScrollPage>Page content</ScrollPage>);
    expect(screen.getByText('Page content')).toBeInTheDocument();
  });

  it('ScrollPage_HasOverflowAuto_ForScrolling', () => {
    const { container } = render(<ScrollPage>Page content</ScrollPage>);
    expect(container.firstChild).toHaveClass('overflow-y-auto');
  });

  it('ScrollPage_AcceptsClassName_MergesWithDefaults', () => {
    const { container } = render(
      <ScrollPage className="page-class">Content</ScrollPage>
    );
    expect(container.firstChild).toHaveClass('page-class');
  });
});

describe('FullScreenContainer', () => {
  it('FullScreenContainer_RendersChildren_WithFullHeightStyles', () => {
    render(<FullScreenContainer>Full screen content</FullScreenContainer>);
    expect(screen.getByText('Full screen content')).toBeInTheDocument();
  });

  it('FullScreenContainer_HasOverflowHidden_ToPreventScroll', () => {
    const { container } = render(
      <FullScreenContainer>Full screen content</FullScreenContainer>
    );
    expect(container.firstChild).toHaveClass('overflow-hidden');
  });

  it('FullScreenContainer_AcceptsClassName_MergesWithDefaults', () => {
    const { container } = render(
      <FullScreenContainer className="fullscreen-class">
        Content
      </FullScreenContainer>
    );
    expect(container.firstChild).toHaveClass('fullscreen-class');
  });
});

describe('HeadlessScroll', () => {
  it('HeadlessScroll_RendersChildren_WithMinimalStyles', () => {
    render(<HeadlessScroll>Headless content</HeadlessScroll>);
    expect(screen.getByText('Headless content')).toBeInTheDocument();
  });

  it('HeadlessScroll_HasOverflowAuto_ForScrolling', () => {
    const { container } = render(
      <HeadlessScroll>Headless content</HeadlessScroll>
    );
    expect(container.firstChild).toHaveClass('overflow-y-auto');
  });

  it('HeadlessScroll_AcceptsClassName_MergesWithDefaults', () => {
    const { container } = render(
      <HeadlessScroll className="headless-class">Content</HeadlessScroll>
    );
    expect(container.firstChild).toHaveClass('headless-class');
  });
});
