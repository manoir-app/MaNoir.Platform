import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './PageHeader.module.css';

export type PageHeaderVariant = 'hero' | 'page';

export interface PageHeaderProps extends Omit<React.HTMLAttributes<HTMLDivElement>, 'title'> {
  eyebrow?: React.ReactNode;
  title: React.ReactNode;
  description?: React.ReactNode;
  meta?: React.ReactNode;
  actions?: React.ReactNode;
  variant?: PageHeaderVariant;
}

export function PageHeader({
  actions,
  className,
  description,
  eyebrow,
  meta,
  title,
  variant = 'hero',
  ...props
}: PageHeaderProps) {
  return (
    <header className={cx(styles.root, styles[variant], className)} data-variant={variant} {...props}>
      <div className={styles.content}>
        {eyebrow ? <div className={styles.eyebrow}>{eyebrow}</div> : null}
        <div className={styles.title}>{title}</div>
        {description ? <p className={styles.description}>{description}</p> : null}
        {meta ? <div className={styles.meta}>{meta}</div> : null}
      </div>
      {actions ? <div className={styles.actions}>{actions}</div> : null}
    </header>
  );
}