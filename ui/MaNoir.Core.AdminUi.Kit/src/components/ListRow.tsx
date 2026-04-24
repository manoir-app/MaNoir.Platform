import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './ListRow.module.css';

export interface ListRowProps extends Omit<React.HTMLAttributes<HTMLDivElement>, 'title'> {
  title: React.ReactNode;
  subtitle?: React.ReactNode;
  leading?: React.ReactNode;
  trailing?: React.ReactNode;
  clickable?: boolean;
}

export function ListRow({
  className,
  clickable = false,
  leading,
  subtitle,
  title,
  trailing,
  ...props
}: ListRowProps) {
  return (
    <div className={cx(styles.root, clickable && styles.clickable, className)} {...props}>
      {leading ? <div className={styles.leading}>{leading}</div> : null}
      <div className={styles.main}>
        <div className={styles.title}>{title}</div>
        {subtitle ? <div className={styles.subtitle}>{subtitle}</div> : null}
      </div>
      {trailing ? <div className={styles.trailing}>{trailing}</div> : null}
    </div>
  );
}