import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './InlineTabs.module.css';

export type InlineTabsVariant = 'line' | 'pill';

export interface InlineTabsItem {
  id: string;
  label: React.ReactNode;
  badge?: React.ReactNode;
  disabled?: boolean;
  href?: string;
  onSelect?: (event: React.MouseEvent<HTMLElement>) => void;
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

        if (item.href) {
          return (
            <a
              aria-current={active ? 'page' : undefined}
              className={cx(styles.tab, active && styles.active, item.disabled && styles.disabled)}
              data-active={active ? 'true' : 'false'}
              href={item.disabled ? undefined : item.href}
              key={item.id}
              onClick={(event) => {
                if (item.disabled) {
                  event.preventDefault();
                  return;
                }

                item.onSelect?.(event);
                onValueChange(item.id);
              }}
            >
              <span>{item.label}</span>
              {item.badge ? <span className={styles.badge}>{item.badge}</span> : null}
            </a>
          );
        }

        return (
          <button
            className={cx(styles.tab, active && styles.active)}
            data-active={active ? 'true' : 'false'}
            disabled={item.disabled}
            key={item.id}
            onClick={(event) => {
              item.onSelect?.(event);
              onValueChange(item.id);
            }}
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