import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './ScanPanel.module.css';

export interface ScanPanelProps extends React.HTMLAttributes<HTMLDivElement> {
  height?: number | string;
}

export function ScanPanel({ className, height = 300, ...props }: ScanPanelProps) {
  return (
    <div className={cx(styles.root, className)} style={{ height }} {...props}>
      <div className={cx(styles.corner, styles.topLeft)} />
      <div className={cx(styles.corner, styles.topRight)} />
      <div className={cx(styles.corner, styles.bottomLeft)} />
      <div className={cx(styles.corner, styles.bottomRight)} />
      <div className={styles.frame} />
      <div className={styles.scanLine} />
    </div>
  );
}