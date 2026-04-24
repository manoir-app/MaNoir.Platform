import * as React from 'react';
import { cx } from '../lib/cx';
import { Field } from './Field';
import { TextField, type TextFieldProps } from './TextField';
import styles from './SearchInput.module.css';

export interface SearchInputProps extends Omit<TextFieldProps, 'type'> {
  label?: React.ReactNode;
  hint?: React.ReactNode;
  error?: React.ReactNode;
  shortcut?: React.ReactNode;
}

export const SearchInput = React.forwardRef<HTMLInputElement, SearchInputProps>(function SearchInput(
  { className, error, hint, id, label, shortcut = '⌘K', ...props },
  ref,
) {
  return (
    <Field error={error} hint={hint} htmlFor={id} label={label}>
      <div className={cx(styles.root, className)}>
        <span aria-hidden="true" className={styles.icon}>
          ⌕
        </span>
        <TextField {...props} className={styles.input} id={id} ref={ref} type="search" />
        {shortcut ? <span className={styles.shortcut}>{shortcut}</span> : null}
      </div>
    </Field>
  );
});