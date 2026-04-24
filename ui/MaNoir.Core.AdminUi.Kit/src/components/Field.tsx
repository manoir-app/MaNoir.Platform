import * as React from 'react';
import * as Label from '@radix-ui/react-label';
import { cx } from '../lib/cx';
import styles from './Field.module.css';

export interface FieldProps extends React.HTMLAttributes<HTMLDivElement> {
  label?: React.ReactNode;
  hint?: React.ReactNode;
  error?: React.ReactNode;
  htmlFor?: string;
  required?: boolean;
}

export function Field({
  children,
  className,
  error,
  hint,
  htmlFor,
  label,
  required = false,
  ...props
}: FieldProps) {
  return (
    <div className={cx(styles.root, className)} {...props}>
      {label ? (
        <Label.Root className={styles.label} htmlFor={htmlFor}>
          <span>{label}</span>
          {required ? <span className={styles.required}>*</span> : null}
        </Label.Root>
      ) : null}
      {children}
      {error ? <p className={styles.error}>{error}</p> : hint ? <p className={styles.hint}>{hint}</p> : null}
    </div>
  );
}