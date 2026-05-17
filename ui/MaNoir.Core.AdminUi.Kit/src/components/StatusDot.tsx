import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './StatusDot.module.css';

export type StatusDotTone = 'neutral' | 'accent' | 'success' | 'warning' | 'danger' | 'muted';

export interface StatusDotProps extends React.HTMLAttributes<HTMLSpanElement> {
  label?: React.ReactNode;
  tone?: StatusDotTone;
}

export function StatusDot({ className, label, tone = 'neutral', ...props }: StatusDotProps) {
  return (
    <span className={cx(styles.root, styles[tone], className)} data-tone={tone} {...props}>
      <span aria-hidden="true" className={styles.dot} />
      {label ? <span className={styles.label}>{label}</span> : null}
    </span>
  );
}