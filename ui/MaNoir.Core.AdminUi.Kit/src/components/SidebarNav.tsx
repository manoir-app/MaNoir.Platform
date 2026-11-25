import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './SidebarNav.module.css';

export interface SidebarNavItem {
  id: string;
  label: React.ReactNode;
  href?: string;
  meta?: React.ReactNode;
  /** Glyph shown before the label; the only thing shown with a short caption when the nav is collapsed. */
  icon?: React.ReactNode;
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
  /** Icon rail: icon above a short caption, section titles hidden. Items without an icon show their initial. */
  collapsed?: boolean;
}

function renderContent(item: SidebarNavItem, collapsed: boolean) {
  const fallback = typeof item.label === 'string' ? item.label.trim().charAt(0).toUpperCase() : '';

  if (collapsed) {
    return (
      <>
        <span aria-hidden="true" className={styles.icon}>{item.icon ?? fallback}</span>
        <span className={styles.srOnly}>{item.label}</span>
      </>
    );
  }

  return (
    <>
      {item.icon ? <span aria-hidden="true" className={styles.icon}>{item.icon}</span> : null}
      <span className={styles.label}>{item.label}</span>
      {item.meta ? <span className={styles.meta}>{item.meta}</span> : null}
    </>
  );
}

export function SidebarNav({
  brand,
  caption,
  className,
  collapsed = false,
  footer,
  searchSlot,
  sections,
  ...props
}: SidebarNavProps) {
  return (
    <aside className={cx(styles.root, collapsed && styles.collapsed, className)} data-collapsed={collapsed || undefined} {...props}>
      {!collapsed && (brand || caption) ? (
        <div className={styles.brandBlock}>
          {brand ? <div className={styles.brand}>{brand}</div> : null}
          {caption ? <div className={styles.caption}>{caption}</div> : null}
        </div>
      ) : null}

      {!collapsed && searchSlot ? <div className={styles.search}>{searchSlot}</div> : null}

      <div className={styles.sections}>
        {sections.map((section) => (
          <section className={styles.section} key={section.id}>
            {!collapsed && section.title ? <div className={styles.sectionTitle}>{section.title}</div> : null}
            <ul className={styles.list}>
              {section.items.map((item) => (
                <li className={styles.item} key={item.id}>
                  {item.href ? (
                    <a
                      aria-current={item.active ? 'page' : undefined}
                      className={cx(styles.link, item.active && styles.active)}
                      href={item.href}
                      title={collapsed && typeof item.label === 'string' ? item.label : undefined}
                      onClick={item.onClick}
                    >
                      {renderContent(item, collapsed)}
                    </a>
                  ) : (
                    <button
                      aria-current={item.active ? 'page' : undefined}
                      className={cx(styles.link, item.active && styles.active)}
                      onClick={item.onClick}
                      title={collapsed && typeof item.label === 'string' ? item.label : undefined}
                      type="button"
                    >
                      {renderContent(item, collapsed)}
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