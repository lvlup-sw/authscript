/**
 * Transition variants for scene changes in the demo.
 *
 * - zoom-out:   Encounter -> Fleet ("zooming out to fleet view")
 * - drill-down: Fleet -> Case ("drilling into a case")
 * - generic:    Pill nav (non-linear jump)
 */
export type TransitionDirection = 'zoom-out' | 'drill-down' | 'generic';

const EASING = [0.4, 0, 0.2, 1] as const;

export const transitionVariants = {
  'zoom-out': {
    initial: { opacity: 0, scale: 0.9 },
    animate: { opacity: 1, scale: 1, transition: { duration: 0.8, ease: EASING } },
    exit: { opacity: 0, scale: 0.85, transition: { duration: 0.8, ease: EASING } },
  },
  'drill-down': {
    initial: { opacity: 0, x: 80 },
    animate: { opacity: 1, x: 0, transition: { duration: 0.6, ease: EASING } },
    exit: { opacity: 0, x: -80, transition: { duration: 0.6, ease: EASING } },
  },
  generic: {
    initial: { opacity: 0, x: 40 },
    animate: { opacity: 1, x: 0, transition: { duration: 0.5, ease: EASING } },
    exit: { opacity: 0, x: -40, transition: { duration: 0.5, ease: EASING } },
  },
} as const;
