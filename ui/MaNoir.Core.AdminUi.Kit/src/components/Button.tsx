import * as React from 'react';
import { Slot } from '@radix-ui/react-slot';
import { cx } from '../lib/cx';
import { Spinner } from './Spinner';
import styles from './Button.module.css';

export type ButtonVariant = 'primary' | 'secondary' | 'quiet' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  asChild?: boolean;
  /** Shows a small spinner and disables the button while an action is running. */
  loading?: boolean;
  variant?: ButtonVariant;
  size?: ButtonSize;
}

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  {
    asChild = false,
    children,
    className,
    disabled,
    loading = false,
    size = 'md',
    variant = 'primary',
    type = 'button',
    ...props
  },
  ref,
) {
  const Comp = asChild ? Slot : 'button';

  return (
    <Comp
      aria-busy={loading || undefined}
      className={cx(styles.root, styles[size], styles[variant], loading && styles.loading, className)}
      data-size={size}
      data-variant={variant}
      disabled={asChild ? undefined : disabled || loading}
      ref={ref}
      type={asChild ? undefined : type}
      {...props}
    >
      {asChild ? (
        children
      ) : (
        <>
          {loading ? <Spinner size="sm" /> : null}
          {children}
        </>
      )}
    </Comp>
  );
});
