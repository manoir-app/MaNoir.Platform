import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './SidebarNav.module.css';

export interface SidebarNavItem {
  id: string;
  label: React.ReactNode;
  href?: string;
  meta?: React.ReactNode;
  active?: boolean;
  onClick?: (event: React.MouseEvent<HTMLElement>) => void;
}

export interface SidebarNavSection {
  id: string;
  title?: React.ReactNode;
  items: SidebarNavItem[];
}

export interface SidebarNavProps extends React.HTMLAttributes<HTMLElement> {
  brand?: React.ReactNode;
  caption?: React.ReactNode;
  searchSlot?: React.ReactNode;
  sections: SidebarNavSection[];
  footer?: React.ReactNode;
}

export function SidebarNav({
  brand,
  caption,
  className,
  footer,
  searchSlot,
  sections,
  ...props
}: SidebarNavProps) {
  return (
    <aside className={cx(styles.root, className)} {...props}>
      {(brand || caption) ? (
        <div className={styles.brandBlock}>
          {brand ? <div className={styles.brand}>{brand}</div> : null}
          {caption ? <div className={styles.caption}>{caption}</div> : null}
        </div>
      ) : null}

      {searchSlot ? <div className={styles.search}>{searchSlot}</div> : null}

      <div className={styles.sections}>
        {sections.map((section) => (
          <section className={styles.section} key={section.id}>
            {section.title ? <div className={styles.sectionTitle}>{section.title}</div> : null}
            <ul className={styles.list}>
              {section.items.map((item) => (
                <li className={styles.item} key={item.id}>
                  {item.href ? (
                    <a
                      aria-current={item.active ? 'page' : undefined}
                      className={cx(styles.link, item.active && styles.active)}
                      href={item.href}
                      onClick={item.onClick}
                    >
                      <span>{item.label}</span>
                      {item.meta ? <span className={styles.meta}>{item.meta}</span> : null}
                    </a>
                  ) : (
                    <button
                      aria-current={item.active ? 'page' : undefined}
                      className={cx(styles.link, item.active && styles.active)}
                      onClick={item.onClick}
                      type="button"
                    >
                      <span>{item.label}</span>
                      {item.meta ? <span className={styles.meta}>{item.meta}</span> : null}
                    </button>
                  )}
                </li>
              ))}
            </ul>
          </section>
        ))}
      </div>

      {footer ? <div className={styles.footer}>{footer}</div> : null}
    </aside>
  );
}