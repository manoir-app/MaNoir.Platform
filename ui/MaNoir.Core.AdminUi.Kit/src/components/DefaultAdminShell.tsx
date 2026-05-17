import * as React from 'react';
import { AdminShell } from './AdminShell';
import { AppBrandHeader } from './AppBrandHeader';
import { Avatar } from './Avatar';
import type { StatusPillTone } from './StatusPill';
import { cx } from '../lib/cx';
import styles from './DefaultAdminShell.module.css';

export interface DefaultAdminShellNavItem {
  id: string;
  label: React.ReactNode;
  description?: React.ReactNode;
  active?: boolean;
  href?: string;
  onSelect?: () => void;
}

export interface DefaultAdminShellProps extends React.HTMLAttributes<HTMLDivElement> {
  children: React.ReactNode;
  sidebarEyebrow?: React.ReactNode;
  sidebarBrand: React.ReactNode;
  sidebarDescription?: React.ReactNode;
  serverLabel?: React.ReactNode;
  serverName: React.ReactNode;
  serverMeta?: React.ReactNode;
  serverStatus?: React.ReactNode;
  serverStatusTone?: StatusPillTone;
  navigationAriaLabel?: string;
  navigationItems: DefaultAdminShellNavItem[];
  topBarBrand: React.ReactNode;
  topBarLogo?: React.ReactNode;
  topBarMeta?: React.ReactNode;
  topBarStatus?: React.ReactNode;
  topBarActions?: React.ReactNode;
  userLabel?: React.ReactNode;
  userValue: React.ReactNode;
  logoutLabel: React.ReactNode;
  onLogout: () => void;
  logoutDisabled?: boolean;
}

function renderNavItem(item: DefaultAdminShellNavItem) {
  const className = cx(styles.navItem, item.active && styles.navItemActive);

  if (item.href) {
    return (
      <a aria-current={item.active ? 'page' : undefined} className={className} href={item.href} onClick={item.onSelect}>
        <span className={styles.navTitle}>{item.label}</span>
        {item.description ? <span className={styles.navDescription}>{item.description}</span> : null}
      </a>
    );
  }

  return (
    <button aria-current={item.active ? 'page' : undefined} className={className} onClick={item.onSelect} type="button">
      <span className={styles.navTitle}>{item.label}</span>
      {item.description ? <span className={styles.navDescription}>{item.description}</span> : null}
    </button>
  );
}

export function DefaultAdminShell({
  children,
  className,
  logoutDisabled = false,
  logoutLabel,
  navigationAriaLabel,
  navigationItems,
  onLogout,
  serverLabel,
  serverMeta,
  serverName,
  serverStatus,
  serverStatusTone = 'neutral',
  sidebarBrand,
  sidebarDescription,
  sidebarEyebrow,
  topBarActions,
  topBarBrand,
  topBarLogo,
  topBarMeta,
  userLabel,
  userValue,
  ...props
}: DefaultAdminShellProps) {
  const accountButtonRef = React.useRef<HTMLButtonElement | null>(null);
  const accountMenuRef = React.useRef<HTMLDivElement | null>(null);
  const [accountMenuOpen, setAccountMenuOpen] = React.useState(false);

  React.useEffect(() => {
    if (!accountMenuOpen) {
      return undefined;
    }

    const onPointerDown = (event: PointerEvent) => {
      const target = event.target as Node;

      if (accountButtonRef.current?.contains(target) || accountMenuRef.current?.contains(target)) {
        return;
      }

      setAccountMenuOpen(false);
    };

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setAccountMenuOpen(false);
      }
    };

    document.addEventListener('pointerdown', onPointerDown);
    document.addEventListener('keydown', onKeyDown);

    return () => {
      document.removeEventListener('pointerdown', onPointerDown);
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [accountMenuOpen]);

  const sidebar = (
    <div className={styles.sidebar}>
      <div className={styles.brandBlock}>
        {sidebarEyebrow ? <div className={styles.eyebrow}>{sidebarEyebrow}</div> : null}
        <h1 className={styles.brand}>{sidebarBrand}</h1>
        {sidebarDescription ? <p className={styles.copy}>{sidebarDescription}</p> : null}
      </div>

      <div className={styles.serverCard}>
        {serverLabel ? <div className={styles.serverLabel}>{serverLabel}</div> : null}
        <div className={styles.serverHeading}>
          <span
            aria-label={typeof serverStatus === 'string' ? serverStatus : undefined}
            className={cx(styles.serverStatusDot, styles[`serverStatusDot${serverStatusTone.charAt(0).toUpperCase()}${serverStatusTone.slice(1)}`])}
            role={serverStatus ? 'status' : undefined}
            title={typeof serverStatus === 'string' ? serverStatus : undefined}
          />
          <div className={styles.serverName}>{serverName}</div>
        </div>
        {serverMeta ? (
          <div className={styles.serverMeta}>
            {serverMeta ? <span>{serverMeta}</span> : null}
          </div>
        ) : null}
      </div>

      <nav aria-label={navigationAriaLabel} className={styles.nav}>
        {navigationItems.map((item) => (
          <React.Fragment key={item.id}>{renderNavItem(item)}</React.Fragment>
        ))}
      </nav>

      <div className={styles.accountFooter}>
        <button
          aria-expanded={accountMenuOpen}
          className={styles.accountButton}
          onClick={() => {
            setAccountMenuOpen((current) => !current);
          }}
          ref={accountButtonRef}
          type="button"
        >
          <Avatar
            className={styles.accountAvatar}
            name={typeof userValue === 'string' ? userValue : undefined}
            size="md"
          />
          <span className={styles.accountIdentity}>
            {userLabel ? <span className={styles.userLabel}>{userLabel}</span> : null}
            <span className={styles.userValue}>{userValue}</span>
          </span>
          <span aria-hidden="true" className={styles.accountGlyph}>⚙</span>
        </button>

        {accountMenuOpen ? (
          <div className={styles.accountMenu} ref={accountMenuRef}>
            <button
              className={styles.accountMenuAction}
              disabled={logoutDisabled}
              onClick={() => {
                setAccountMenuOpen(false);
                onLogout();
              }}
              type="button"
            >
              {logoutLabel}
            </button>
          </div>
        ) : null}
      </div>
    </div>
  );

  const topBar = (
    <AppBrandHeader
      actions={topBarActions ? <div className={styles.headerActions}>{topBarActions}</div> : undefined}
      brand={topBarBrand}
      className={styles.header}
      logo={topBarLogo}
      meta={topBarMeta}
    />
  );

  return (
    <AdminShell className={className} contentClassName={styles.content} sidebar={sidebar} sidebarSize="lg" topBar={topBar} {...props}>
      {children}
    </AdminShell>
  );
}