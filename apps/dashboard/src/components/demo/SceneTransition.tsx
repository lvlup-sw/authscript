import type { ReactNode } from 'react';
import { AnimatePresence, motion } from 'motion/react';
import {
  transitionVariants,
  type TransitionDirection,
} from './transitionVariants';

interface SceneTransitionProps {
  sceneKey: string;
  children: ReactNode;
  direction?: TransitionDirection;
}

export function SceneTransition({
  sceneKey,
  children,
  direction = 'generic',
}: SceneTransitionProps) {
  const variants = transitionVariants[direction];

  return (
    <AnimatePresence mode="wait">
      <motion.div
        key={sceneKey}
        initial={variants.initial}
        animate={variants.animate}
        exit={variants.exit}
      >
        {children}
      </motion.div>
    </AnimatePresence>
  );
}
