import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './InlineTabs.module.css';

export type InlineTabsVariant = 'line' | 'pill';

export interface InlineTabsItem {
  id: string;
  label: React.ReactNode;
  badge?: React.ReactNode;
  disabled?: boolean;
}

export interface InlineTabsProps extends React.HTMLAttributes<HTMLDivElement> {
  items: InlineTabsItem[];
  value: string;
  onValueChange: (value: string) => void;
  variant?: InlineTabsVariant;
}

export function InlineTabs({ className, items, onValueChange, value, variant = 'line', ...props }: InlineTabsProps) {
  return (
    <div className={cx(styles.root, styles[variant], className)} {...props}>
      {items.map((item) => {
        const active = item.id === value;

        return (
          <button
            className={cx(styles.tab, active && styles.active)}
            data-active={active ? 'true' : 'false'}
            disabled={item.disabled}
            key={item.id}
            onClick={() => onValueChange(item.id)}
            type="button"
          >
            <span>{item.label}</span>
            {item.badge ? <span className={styles.badge}>{item.badge}</span> : null}
          </button>
        );
      })}
    </div>
  );
}