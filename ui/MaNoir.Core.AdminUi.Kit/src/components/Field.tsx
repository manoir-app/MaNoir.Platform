import * as React from 'react';
import * as Label from '@radix-ui/react-label';
import { cx } from '../lib/cx';
import styles from './Field.module.css';

export type FieldVariant = 'default' | 'editorial';

export interface FieldProps extends React.HTMLAttributes<HTMLDivElement> {
  label?: React.ReactNode;
  hint?: React.ReactNode;
  error?: React.ReactNode;
  htmlFor?: string;
  labelAside?: React.ReactNode;
  required?: boolean;
  variant?: FieldVariant;
}

export function Field({
  children,
  className,
  error,
  hint,
  htmlFor,
  label,
  labelAside,
  required = false,
  variant = 'default',
  ...props
}: FieldProps) {
  return (
    <div className={cx(styles.root, styles[variant], className)} data-variant={variant} {...props}>
      {label ? (
        <div className={styles.header}>
          <Label.Root className={styles.label} htmlFor={htmlFor}>
            <span>{label}</span>
            {required ? <span className={styles.required}>*</span> : null}
          </Label.Root>
          {labelAside ? <div className={styles.labelAside}>{labelAside}</div> : null}
        </div>
      ) : null}
      {children}
      {error ? <p className={styles.error}>{error}</p> : hint ? <p className={styles.hint}>{hint}</p> : null}
    </div>
  );
}