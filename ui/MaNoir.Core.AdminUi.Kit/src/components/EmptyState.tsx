import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './EmptyState.module.css';

export interface EmptyStateProps extends React.HTMLAttributes<HTMLDivElement> {
  eyebrow?: React.ReactNode;
  heading: React.ReactNode;
  description?: React.ReactNode;
  actions?: React.ReactNode;
}

export function EmptyState({
  actions,
  className,
  description,
  eyebrow,
  heading,
  ...props
}: EmptyStateProps) {
  return (
    <section className={cx(styles.root, className)} {...props}>
      {eyebrow ? <div className={styles.eyebrow}>{eyebrow}</div> : null}
      <div className={styles.heading}>{heading}</div>
      {description ? <p className={styles.description}>{description}</p> : null}
      {actions ? <div className={styles.actions}>{actions}</div> : null}
    </section>
  );
}