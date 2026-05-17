import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './ToggleSwitch.module.css';

export type ToggleSwitchSize = 'sm' | 'md';

export interface ToggleSwitchProps extends Omit<React.ButtonHTMLAttributes<HTMLButtonElement>, 'onChange'> {
  checked: boolean;
  onCheckedChange?: (checked: boolean) => void;
  size?: ToggleSwitchSize;
}

export const ToggleSwitch = React.forwardRef<HTMLButtonElement, ToggleSwitchProps>(function ToggleSwitch(
  { checked, className, disabled, onCheckedChange, onClick, size = 'md', type = 'button', ...props },
  ref,
) {
  return (
    <button
      {...props}
      className={cx(styles.root, styles[size], checked && styles.checked, className)}
      data-checked={checked ? 'true' : 'false'}
      disabled={disabled}
      onClick={(event) => {
        onClick?.(event);

        if (!event.defaultPrevented && !disabled) {
          onCheckedChange?.(!checked);
        }
      }}
      ref={ref}
      type={type}
    >
      <span className={styles.thumb} />
    </button>
  );
});