import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './Card.module.css';

export interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  tone?: 'default' | 'attention';
}

export function Card({ className, tone = 'default', ...props }: CardProps) {
  return <div className={cx(styles.root, styles[tone], className)} data-tone={tone} {...props} />;
}