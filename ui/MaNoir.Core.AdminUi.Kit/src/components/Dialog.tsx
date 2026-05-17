import * as React from 'react';
import { createPortal } from 'react-dom';
import { cx } from '../lib/cx';
import styles from './Dialog.module.css';

export interface DialogProps extends Omit<React.HTMLAttributes<HTMLDivElement>, 'title'> {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  eyebrow?: React.ReactNode;
  title: React.ReactNode;
  description?: React.ReactNode;
  footer?: React.ReactNode;
  size?: 'sm' | 'md' | 'lg';
  dismissible?: boolean;
  closeLabel?: string;
}

export function Dialog({
  children,
  className,
  closeLabel = 'Fermer',
  description,
  dismissible = true,
  eyebrow,
  footer,
  onOpenChange,
  open,
  size = 'md',
  title,
  ...props
}: DialogProps) {
  React.useEffect(() => {
    if (!open || typeof document === 'undefined') {
      return undefined;
    }

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && dismissible) {
        onOpenChange(false);
      }
    };

    document.addEventListener('keydown', onKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [dismissible, onOpenChange, open]);

  if (!open) {
    return null;
  }

  const content = (
    <div className={styles.overlay} onClick={dismissible ? () => onOpenChange(false) : undefined}>
      <div
        {...props}
        aria-modal="true"
        className={cx(styles.dialog, styles[size], className)}
        onClick={(event) => {
          event.stopPropagation();
        }}
        role="dialog"
      >
        <div className={styles.header}>
          <div className={styles.headerContent}>
            {eyebrow ? <div className={styles.eyebrow}>{eyebrow}</div> : null}
            <div className={styles.title}>{title}</div>
            {description ? <p className={styles.description}>{description}</p> : null}
          </div>
          {dismissible ? (
            <button aria-label={closeLabel} className={styles.close} onClick={() => onOpenChange(false)} type="button">
              ×
            </button>
          ) : null}
        </div>
        <div className={styles.body}>{children}</div>
        {footer ? <div className={styles.footer}>{footer}</div> : null}
      </div>
    </div>
  );

  if (typeof document === 'undefined') {
    return content;
  }

  return createPortal(content, document.body);
}