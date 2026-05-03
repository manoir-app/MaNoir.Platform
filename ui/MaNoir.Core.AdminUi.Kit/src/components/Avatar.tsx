import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './Avatar.module.css';

export type AvatarSize = 'sm' | 'md' | 'lg';
export type AvatarTone = 'default' | 'muted' | 'accent' | 'success' | 'warning';

export interface AvatarProps extends React.HTMLAttributes<HTMLSpanElement> {
  initials?: React.ReactNode;
  name?: string;
  size?: AvatarSize;
  tone?: AvatarTone;
}

function computeInitials(name?: string) {
  if (!name) {
    return null;
  }

  const parts = name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '');

  return parts.join('');
}

export function Avatar({ className, initials, name, size = 'md', tone = 'default', ...props }: AvatarProps) {
  const content = initials ?? computeInitials(name);

  return (
    <span className={cx(styles.root, styles[size], styles[tone], className)} {...props}>
      {content}
    </span>
  );
}