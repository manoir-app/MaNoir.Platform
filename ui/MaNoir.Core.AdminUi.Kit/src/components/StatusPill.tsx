import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './StatusPill.module.css';

export type StatusPillTone = 'neutral' | 'accent' | 'success' | 'warning' | 'danger';

export interface StatusPillProps extends React.HTMLAttributes<HTMLSpanElement> {
  tone?: StatusPillTone;
  dot?: boolean;
}

export function StatusPill({ children, className, dot = true, tone = 'neutral', ...props }: StatusPillProps) {
  return (
    <span className={cx(styles.root, styles[tone], className)} data-tone={tone} {...props}>
      {dot ? <span className={styles.dot} aria-hidden="true" /> : null}
      <span>{children}</span>
    </span>
  );
}