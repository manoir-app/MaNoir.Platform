import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './EditorialBand.module.css';

export interface EditorialBandProps extends React.HTMLAttributes<HTMLDivElement> {
  tone?: 'accent' | 'warning' | 'danger';
}

export function EditorialBand({ className, tone = 'accent', ...props }: EditorialBandProps) {
  return <div className={cx(styles.root, styles[tone], className)} data-tone={tone} {...props} />;
}