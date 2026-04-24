import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './StatCard.module.css';

export interface StatCardProps extends React.HTMLAttributes<HTMLDivElement> {
  value: React.ReactNode;
  label: React.ReactNode;
  detail?: React.ReactNode;
  tone?: 'default' | 'attention';
}

export function StatCard({ className, detail, label, tone = 'default', value, ...props }: StatCardProps) {
  return (
    <div className={cx(styles.root, styles[tone], className)} data-tone={tone} {...props}>
      <div className={styles.value}>{value}</div>
      <div className={styles.label}>{label}</div>
      {detail ? <div className={styles.detail}>{detail}</div> : null}
    </div>
  );
}