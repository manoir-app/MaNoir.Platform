import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './Spinner.module.css';

export type SpinnerSize = 'sm' | 'md' | 'lg';

export interface SpinnerProps extends React.HTMLAttributes<HTMLSpanElement> {
  size?: SpinnerSize;
  /** Says what is in progress. Always provide it when known; never fake a percentage. */
  label?: React.ReactNode;
}

export function Spinner({ className, label, size = 'md', ...props }: SpinnerProps) {
  const blob = <span aria-hidden="true" className={cx(styles.root, size !== 'md' && styles[size], !label && className)} />;

  if (!label) {
    return React.cloneElement(blob, props as React.HTMLAttributes<HTMLSpanElement>);
  }

  return (
    <span className={cx(styles.wait, className)} role="status" {...props}>
      {blob}
      <span>{label}</span>
    </span>
  );
}
