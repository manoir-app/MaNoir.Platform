import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './SectionTitle.module.css';

export interface SectionTitleProps extends React.HTMLAttributes<HTMLDivElement> {
  eyebrow?: React.ReactNode;
  heading: React.ReactNode;
  description?: React.ReactNode;
  actions?: React.ReactNode;
}

export function SectionTitle({
  actions,
  className,
  description,
  eyebrow,
  heading,
  ...props
}: SectionTitleProps) {
  return (
    <div className={cx(styles.root, className)} {...props}>
      <div className={styles.content}>
        {eyebrow ? <div className={styles.eyebrow}>{eyebrow}</div> : null}
        <div className={styles.heading}>{heading}</div>
        {description ? <div className={styles.description}>{description}</div> : null}
      </div>
      {actions ? <div className={styles.actions}>{actions}</div> : null}
    </div>
  );
}