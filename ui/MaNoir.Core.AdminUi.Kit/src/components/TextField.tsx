import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './TextField.module.css';

export interface TextFieldProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'size'> {
  invalid?: boolean;
}

export const TextField = React.forwardRef<HTMLInputElement, TextFieldProps>(function TextField(
  { className, invalid = false, ...props },
  ref,
) {
  return <input className={cx(styles.root, invalid && styles.invalid, className)} data-invalid={invalid} ref={ref} {...props} />;
});