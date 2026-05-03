import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './StatStrip.module.css';

export interface StatStripItem {
  id: string;
  label: React.ReactNode;
  value: React.ReactNode;
  detail?: React.ReactNode;
  tone?: 'default' | 'accent';
}

export interface StatStripProps extends React.HTMLAttributes<HTMLDivElement> {
  items: StatStripItem[];
}

export function StatStrip({ className, items, ...props }: StatStripProps) {
  return (
    <div className={cx(styles.root, className)} {...props}>
      {items.map((item, index) => (
        <div className={cx(styles.item, index > 0 && styles.withDivider, item.tone === 'accent' && styles.accent)} key={item.id}>
          <div className={styles.value}>{item.value}</div>
          <div className={styles.label}>{item.label}</div>
          {item.detail ? <div className={styles.detail}>{item.detail}</div> : null}
        </div>
      ))}
    </div>
  );
}