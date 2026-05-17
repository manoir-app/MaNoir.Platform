import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './AdminShell.module.css';

export interface AdminShellProps extends React.HTMLAttributes<HTMLDivElement> {
  topBar?: React.ReactNode;
  sidebar?: React.ReactNode;
  sidebarClassName?: string;
  sidebarSize?: 'sm' | 'md' | 'lg';
  contentClassName?: string;
  contentPadding?: 'none' | 'compact' | 'comfortable';
}

export function AdminShell({
  children,
  className,
  contentClassName,
  contentPadding = 'comfortable',
  sidebar,
  sidebarClassName,
  sidebarSize = 'md',
  topBar,
  ...props
}: AdminShellProps) {
  return (
    <div className={cx(styles.root, styles[sidebarSize], className)} {...props}>
      {topBar ? <div className={styles.topBar}>{topBar}</div> : null}
      <div className={cx(styles.body, !sidebar && styles.bodyWithoutSidebar)}>
        {sidebar ? <aside className={cx(styles.sidebar, sidebarClassName)}>{sidebar}</aside> : null}
        {sidebar ? <div aria-hidden="true" className={styles.divider} /> : null}
        <main className={cx(styles.content, styles[contentPadding], contentClassName)}>{children}</main>
      </div>
    </div>
  );
}