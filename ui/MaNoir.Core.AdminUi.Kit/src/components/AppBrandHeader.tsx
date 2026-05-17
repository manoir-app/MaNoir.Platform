import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './AppBrandHeader.module.css';

export interface AppBrandHeaderProps extends Omit<React.HTMLAttributes<HTMLElement>, 'title'> {
  actions?: React.ReactNode;
  brand: React.ReactNode;
  logo?: React.ReactNode;
  meta?: React.ReactNode;
}

export function AppBrandHeader({
  actions,
  brand,
  className,
  logo,
  meta,
  ...props
}: AppBrandHeaderProps) {
  return (
    <header className={cx(styles.root, className)} {...props}>
      <div className={styles.identity}>
        <div className={styles.branding}>
          {logo ? <span className={styles.logo}>{logo}</span> : null}
          <span className={styles.brand}>{brand}</span>
        </div>
        {meta ? <span className={styles.meta}>{meta}</span> : null}
      </div>
      {actions ? <div className={styles.tools}>{actions}</div> : null}
    </header>
  );
}