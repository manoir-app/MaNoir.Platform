import * as React from 'react';
import { Slot } from '@radix-ui/react-slot';
import { cx } from '../lib/cx';
import styles from './Button.module.css';

export type ButtonVariant = 'primary' | 'secondary' | 'quiet' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  asChild?: boolean;
  variant?: ButtonVariant;
  size?: ButtonSize;
}

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  {
    asChild = false,
    className,
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
      className={cx(styles.root, styles[size], styles[variant], className)}
      data-size={size}
      data-variant={variant}
      ref={ref}
      type={asChild ? undefined : type}
      {...props}
    />
  );
});