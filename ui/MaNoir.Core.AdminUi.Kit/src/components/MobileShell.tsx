import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './MobileShell.module.css';

export interface MobileShellProps extends React.HTMLAttributes<HTMLDivElement> {
  statusLeft?: React.ReactNode;
  statusRight?: React.ReactNode;
  bottomBar?: React.ReactNode;
}

export function MobileShell({
  bottomBar,
  children,
  className,
  statusLeft = '9:41',
  statusRight = '◖◗ ▰▰▰',
  ...props
}: MobileShellProps) {
  return (
    <div className={cx(styles.root, className)} {...props}>
      <div className={styles.island} />
      <div className={styles.statusBar}>
        <span>{statusLeft}</span>
        <span className={styles.statusRight}>{statusRight}</span>
      </div>
      <div className={styles.content}>{children}</div>
      {bottomBar ? <div className={styles.bottomBar}>{bottomBar}</div> : null}
      <div className={styles.homeIndicator} />
    </div>
  );
}