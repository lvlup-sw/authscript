import { AnimatePresence, motion } from 'motion/react';
import type { ReactNode } from 'react';

interface SceneTransitionProps {
  sceneKey: string;
  children: ReactNode;
}

export function SceneTransition({ sceneKey, children }: SceneTransitionProps) {
  return (
    <AnimatePresence mode="wait">
      <motion.div
        key={sceneKey}
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        transition={{ duration: 0.5 }}
      >
        {children}
      </motion.div>
    </AnimatePresence>
  );
}
