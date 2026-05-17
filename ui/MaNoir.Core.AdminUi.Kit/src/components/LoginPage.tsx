import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './LoginPage.module.css';

export type LoginPagePanelWidth = 'md' | 'lg';

export interface LoginPageProps extends React.HTMLAttributes<HTMLDivElement> {
  topBar?: React.ReactNode;
  topBarBrand?: React.ReactNode;
  topBarMeta?: React.ReactNode;
  topBarStatus?: React.ReactNode;
  heroEyebrow?: React.ReactNode;
  heroTitle: React.ReactNode;
  heroDescription?: React.ReactNode;
  heroSupplementary?: React.ReactNode;
  heroFooter?: React.ReactNode;
  panelLeadAction?: React.ReactNode;
  panelEyebrow?: React.ReactNode;
  panelTitle: React.ReactNode;
  panelDescription?: React.ReactNode;
  panelFooter?: React.ReactNode;
  panelWidth?: LoginPagePanelWidth;
}

export function LoginPage({
  children,
  className,
  heroDescription,
  heroEyebrow,
  heroFooter,
  heroSupplementary,
  heroTitle,
  panelDescription,
  panelEyebrow,
  panelFooter,
  panelLeadAction,
  panelTitle,
  panelWidth = 'md',
  topBar,
  topBarBrand,
  topBarMeta,
  topBarStatus,
  ...props
}: LoginPageProps) {
  return (
    <div className={cx(styles.root, styles[panelWidth], className)} {...props}>
      {topBar ? topBar : (topBarBrand || topBarMeta || topBarStatus) ? (
        <header className={styles.topBar}>
          <div className={styles.topBarIdentity}>
            {topBarBrand ? <span className={styles.topBarBrand}>{topBarBrand}</span> : null}
            {topBarMeta ? <span className={styles.topBarMeta}>{topBarMeta}</span> : null}
          </div>
          {topBarStatus ? <div className={styles.topBarStatus}>{topBarStatus}</div> : null}
        </header>
      ) : null}

      <div className={styles.body}>
        <section className={styles.hero}>
          {heroEyebrow ? <div className={styles.heroEyebrow}>{heroEyebrow}</div> : null}
          <div className={styles.heroTitle}>{heroTitle}</div>
          {heroDescription ? <p className={styles.heroDescription}>{heroDescription}</p> : null}
          {heroSupplementary ? <div className={styles.heroSupplementary}>{heroSupplementary}</div> : null}
          {heroFooter ? <div className={styles.heroFooter}>{heroFooter}</div> : null}
        </section>

        <div aria-hidden="true" className={styles.divider} />

        <section className={styles.panel}>
          {panelLeadAction ? <div className={styles.panelLeadAction}>{panelLeadAction}</div> : null}
          {panelEyebrow ? <div className={styles.panelEyebrow}>{panelEyebrow}</div> : null}
          <div className={styles.panelTitle}>{panelTitle}</div>
          {panelDescription ? <p className={styles.panelDescription}>{panelDescription}</p> : null}
          <div className={styles.panelBody}>{children}</div>
          {panelFooter ? <div className={styles.panelFooter}>{panelFooter}</div> : null}
        </section>
      </div>
    </div>
  );
}