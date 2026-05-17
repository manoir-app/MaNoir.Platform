import * as React from 'react';
import { cx } from '../lib/cx';
import { Field } from './Field';
import { TextField, type TextFieldProps } from './TextField';
import styles from './SearchInput.module.css';

export type SearchInputVariant = 'framed' | 'bare';

export interface SearchInputProps extends Omit<TextFieldProps, 'type'> {
  label?: React.ReactNode;
  hint?: React.ReactNode;
  error?: React.ReactNode;
  shortcut?: React.ReactNode;
  variant?: SearchInputVariant;
}

export const SearchInput = React.forwardRef<HTMLInputElement, SearchInputProps>(function SearchInput(
  { className, error, hint, id, label, shortcut = '⌘K', variant = 'framed', ...props },
  ref,
) {
  const inputVariant = variant === 'bare' ? 'plain' : 'plain';

  return (
    <Field error={error} hint={hint} htmlFor={id} label={label}>
      <div className={cx(styles.root, styles[variant], className)} data-variant={variant}>
        <span aria-hidden="true" className={styles.icon}>
          ⌕
        </span>
        <TextField {...props} className={styles.input} id={id} ref={ref} type="search" variant={inputVariant} />
        {shortcut ? <span className={styles.shortcut}>{shortcut}</span> : null}
      </div>
    </Field>
  );
});