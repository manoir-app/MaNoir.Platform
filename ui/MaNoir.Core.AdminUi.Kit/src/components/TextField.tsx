import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './TextField.module.css';

export type TextFieldVariant = 'underline' | 'plain' | 'surface';
export type TextFieldSize = 'md' | 'lg';

export interface TextFieldProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'size'> {
  invalid?: boolean;
  mono?: boolean;
  size?: TextFieldSize;
  variant?: TextFieldVariant;
}

export const TextField = React.forwardRef<HTMLInputElement, TextFieldProps>(function TextField(
  { className, invalid = false, mono = false, size = 'md', variant = 'underline', ...props },
  ref,
) {
  return (
    <input
      className={cx(
        styles.root,
        styles[variant],
        styles[size],
        mono && styles.mono,
        invalid && styles.invalid,
        className,
      )}
      data-invalid={invalid}
      data-size={size}
      data-variant={variant}
      ref={ref}
      {...props}
    />
  );
});