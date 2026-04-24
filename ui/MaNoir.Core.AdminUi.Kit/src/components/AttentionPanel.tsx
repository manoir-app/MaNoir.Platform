import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './AttentionPanel.module.css';

export interface AttentionPanelProps extends Omit<React.HTMLAttributes<HTMLDivElement>, 'title'> {
  eyebrow?: React.ReactNode;
  title: React.ReactNode;
  description?: React.ReactNode;
  actions?: React.ReactNode;
}

export function AttentionPanel({
  actions,
  children,
  className,
  description,
  eyebrow = 'Attention',
  title,
  ...props
}: AttentionPanelProps) {
  return (
    <section className={cx(styles.root, className)} {...props}>
      <div className={styles.eyebrow}>{eyebrow}</div>
      <div className={styles.title}>{title}</div>
      {description ? <p className={styles.description}>{description}</p> : null}
      {children ? <div className={styles.body}>{children}</div> : null}
      {actions ? <div className={styles.actions}>{actions}</div> : null}
    </section>
  );
}