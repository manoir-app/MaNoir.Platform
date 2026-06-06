import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './ShellHeader.module.css';

export interface ShellHeaderItem {
  id: string;
  label: React.ReactNode;
  active?: boolean;
  disabled?: boolean;
  href?: string;
  onSelect?: () => void;
}

export interface ShellHeaderProps extends Omit<React.HTMLAttributes<HTMLElement>, 'title'> {
  title?: React.ReactNode;
  items: ShellHeaderItem[];
  overflowLabel?: React.ReactNode;
  compactBreakpoint?: number;
}

function renderAction(
  item: ShellHeaderItem,
  className: string,
  onSelect?: () => void,
) {
  const sharedProps = {
    'aria-current': item.active ? ('page' as const) : undefined,
    className,
  };

  if (item.href) {
    return (
      <a {...sharedProps} href={item.href} onClick={onSelect}>
        {item.label}
      </a>
    );
  }

  return (
    <button {...sharedProps} disabled={item.disabled} onClick={onSelect ?? item.onSelect} type="button">
      {item.label}
    </button>
  );
}

export function ShellHeader({
  className,
  compactBreakpoint = 640,
  items,
  overflowLabel = 'Plus',
  title,
  ...props
}: ShellHeaderProps) {
  const rootRef = React.useRef<HTMLElement | null>(null);
  const titleRef = React.useRef<HTMLDivElement | null>(null);
  const overflowMeasureRef = React.useRef<HTMLSpanElement | null>(null);
  const menuRef = React.useRef<HTMLDivElement | null>(null);
  const itemMeasureRefs = React.useRef(new Map<string, HTMLSpanElement | null>());

  const [menuOpen, setMenuOpen] = React.useState(false);
  const [rootWidth, setRootWidth] = React.useState(0);
  const [titleWidth, setTitleWidth] = React.useState(0);
  const [visibleCount, setVisibleCount] = React.useState(items.length);

  const isCompact = rootWidth > 0 ? rootWidth <= compactBreakpoint : false;

  React.useLayoutEffect(() => {
    const root = rootRef.current;
    if (!root || typeof ResizeObserver === 'undefined') {
      return undefined;
    }

    const updateRootWidth = () => {
      setRootWidth(root.getBoundingClientRect().width);
    };

    updateRootWidth();

    const rootObserver = new ResizeObserver(updateRootWidth);
    rootObserver.observe(root);

    let titleObserver: ResizeObserver | undefined;

    if (titleRef.current) {
      const updateTitleWidth = () => {
        setTitleWidth(titleRef.current?.getBoundingClientRect().width ?? 0);
      };

      updateTitleWidth();
      titleObserver = new ResizeObserver(updateTitleWidth);
      titleObserver.observe(titleRef.current);
    } else {
      setTitleWidth(0);
    }

    return () => {
      rootObserver.disconnect();
      titleObserver?.disconnect();
    };
  }, [title]);

  React.useLayoutEffect(() => {
    if (!rootWidth || items.length === 0) {
      setVisibleCount(items.length);
      return;
    }

    const gap = isCompact ? 6 : 8;
    const reserve = title ? (isCompact ? 20 : 32) : 0;
    const availableWidth = Math.max(rootWidth - titleWidth - reserve, 0);
    const plusWidth = overflowMeasureRef.current?.getBoundingClientRect().width ?? 0;
    const itemWidths = items.map((item) => itemMeasureRefs.current.get(item.id)?.getBoundingClientRect().width ?? 0);

    const computeVisibleCount = (reserveOverflow: boolean) => {
      let nextVisibleCount = items.length;
      let consumedWidth = 0;

      for (let index = 0; index < items.length; index += 1) {
        const width = itemWidths[index] ?? 0;
        const nextWidth = index === 0 ? width : consumedWidth + gap + width;
        const remaining = items.length - index - 1;
        const overflowReserve = reserveOverflow && remaining > 0 ? gap + plusWidth : 0;

        if (nextWidth + overflowReserve > availableWidth) {
          nextVisibleCount = index;
          break;
        }

        consumedWidth = nextWidth;
      }

      return nextVisibleCount;
    };

    let nextVisibleCount = computeVisibleCount(false);

    if (nextVisibleCount < items.length) {
      nextVisibleCount = computeVisibleCount(true);
    }

    if (nextVisibleCount === 0 && items.length > 0) {
      setVisibleCount(1);
      return;
    }

    setVisibleCount(nextVisibleCount);
  }, [isCompact, items, rootWidth, title, titleWidth]);

  const visibleItems = items.slice(0, visibleCount);
  const overflowItems = items.slice(visibleCount);
  const overflowActive = overflowItems.some((item) => item.active);

  React.useEffect(() => {
    if (overflowItems.length === 0) {
      setMenuOpen(false);
    }
  }, [overflowItems.length]);

  React.useEffect(() => {
    if (!menuOpen) {
      return undefined;
    }

    const onPointerDown = (event: PointerEvent) => {
      const target = event.target as Node;

      if (rootRef.current?.contains(target) || menuRef.current?.contains(target)) {
        return;
      }

      setMenuOpen(false);
    };

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setMenuOpen(false);
      }
    };

    document.addEventListener('pointerdown', onPointerDown);
    document.addEventListener('keydown', onKeyDown);

    return () => {
      document.removeEventListener('pointerdown', onPointerDown);
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [menuOpen]);

  return (
    <header
      {...props}
      className={cx(styles.root, title ? styles.withTitle : styles.withoutTitle, className)}
      data-compact={isCompact ? 'true' : 'false'}
      ref={rootRef}
    >
      {title ? (
        <div className={styles.title} ref={titleRef}>
          {title}
        </div>
      ) : null}

      <div className={styles.nav}>
        {visibleItems.map((item) => (
          <React.Fragment key={item.id}>
            {renderAction(item, cx(styles.action, item.active && styles.active), item.onSelect)}
          </React.Fragment>
        ))}

        {overflowItems.length > 0 ? (
          <div className={styles.overflow}>
            <button
              aria-expanded={menuOpen}
              className={cx(styles.action, overflowActive && styles.active)}
              onClick={() => {
                setMenuOpen((current) => !current);
              }}
              type="button"
            >
              {overflowLabel}
            </button>

            {menuOpen ? (
              <div className={styles.menu} ref={menuRef}>
                {overflowItems.map((item) => (
                  <div key={item.id}>
                    {renderAction(item, cx(styles.menuAction, item.active && styles.menuActionActive), () => {
                      item.onSelect?.();
                      setMenuOpen(false);
                    })}
                  </div>
                ))}
              </div>
            ) : null}
          </div>
        ) : null}
      </div>

      <div aria-hidden="true" className={styles.measure} data-compact={isCompact ? 'true' : 'false'}>
        {items.map((item) => (
          <span
            className={cx(styles.action, styles.measureItem, item.active && styles.active)}
            key={item.id}
            ref={(node) => {
              itemMeasureRefs.current.set(item.id, node);
            }}
          >
            {item.label}
          </span>
        ))}
        <span className={cx(styles.action, styles.measureItem)} ref={overflowMeasureRef}>
          {overflowLabel}
        </span>
      </div>
    </header>
  );
}