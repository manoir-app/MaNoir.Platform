import * as React from 'react';
import { cx } from '../lib/cx';
import { StatusPill, type StatusPillTone } from './StatusPill';
import styles from './MetricList.module.css';

export interface MetricListItem {
  id: string;
  eyebrow?: React.ReactNode;
  label: React.ReactNode;
  value?: React.ReactNode;
  detail?: React.ReactNode;
  status?: React.ReactNode;
  statusTone?: StatusPillTone;
}

export interface MetricListProps extends React.HTMLAttributes<HTMLUListElement> {
  items: MetricListItem[];
}

export function MetricList({ className, items, ...props }: MetricListProps) {
  return (
    <ul className={cx(styles.root, className)} {...props}>
      {items.map((item) => (
        <li className={styles.item} key={item.id}>
          <div className={styles.leading}>
            {item.eyebrow ? <div className={styles.eyebrow}>{item.eyebrow}</div> : null}
            <div className={styles.label}>{item.label}</div>
            {item.detail ? <div className={styles.detail}>{item.detail}</div> : null}
          </div>
          <div className={styles.trailing}>
            {item.value ? <div className={styles.value}>{item.value}</div> : null}
            {item.status ? <StatusPill tone={item.statusTone}>{item.status}</StatusPill> : null}
          </div>
        </li>
      ))}
    </ul>
  );
}