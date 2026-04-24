import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './BottomTabBar.module.css';

export interface BottomTabBarItem {
  id: string;
  label: React.ReactNode;
  icon?: React.ReactNode;
  active?: boolean;
  onClick?: (event: React.MouseEvent<HTMLButtonElement>) => void;
}

export interface BottomTabBarProps extends React.HTMLAttributes<HTMLDivElement> {
  items: BottomTabBarItem[];
}

export function BottomTabBar({ className, items, ...props }: BottomTabBarProps) {
  return (
    <nav className={cx(styles.root, className)} {...props}>
      {items.map((item) => (
        <button
          className={cx(styles.item, item.active && styles.active)}
          key={item.id}
          onClick={item.onClick}
          type="button"
        >
          {item.icon ? <span className={styles.icon}>{item.icon}</span> : null}
          <span>{item.label}</span>
        </button>
      ))}
    </nav>
  );
}