import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './AdminShell.module.css';

export interface AdminShellProps extends React.HTMLAttributes<HTMLDivElement> {
  topBar?: React.ReactNode;
  sidebar?: React.ReactNode;
  sidebarClassName?: string;
  sidebarSize?: 'sm' | 'md' | 'lg';
  showDivider?: boolean;
  contentClassName?: string;
  contentPadding?: 'none' | 'compact' | 'comfortable';
}

export function AdminShell({
  children,
  className,
  contentClassName,
  contentPadding = 'comfortable',
  showDivider = true,
  sidebar,
  sidebarClassName,
  sidebarSize = 'md',
  topBar,
  ...props
}: AdminShellProps) {
  const hasSidebar = Boolean(sidebar);

  return (
    <div className={cx(styles.root, styles[sidebarSize], className)} {...props}>
      {topBar ? <div className={styles.topBar}>{topBar}</div> : null}
      <div className={cx(styles.body, !hasSidebar && styles.bodyWithoutSidebar, hasSidebar && !showDivider && styles.bodyWithoutDivider)}>
        {hasSidebar ? <aside className={cx(styles.sidebar, sidebarClassName)}>{sidebar}</aside> : null}
        {hasSidebar && showDivider ? <div aria-hidden="true" className={styles.divider} /> : null}
        <main className={cx(styles.content, styles[contentPadding], contentClassName)}>{children}</main>
      </div>
    </div>
  );
}