import * as React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { Button } from './Button';
import { Field } from './Field';
import { LoginPage } from './LoginPage';
import { StatusDot } from './StatusDot';
import { TextField } from './TextField';
import styles from './LoginPage.stories.module.css';

type Step = 'credentials' | 'mfa';

interface LoginCanvasProps {
  initialStep?: Step;
}

function LoginCanvas({ initialStep = 'credentials' }: LoginCanvasProps) {
  const [step, setStep] = React.useState<Step>(initialStep);
  const [email, setEmail] = React.useState('paul@castel.fr');
  const [password, setPassword] = React.useState('Bienvenue123');
  const [showPassword, setShowPassword] = React.useState(false);
  const [remember, setRemember] = React.useState(true);
  const [code, setCode] = React.useState(['', '', '', '', '', '']);

  const updateDigit = (index: number, value: string) => {
    if (value && !/^\d$/.test(value)) {
      return;
    }

    setCode((current) => {
      const next = [...current];
      next[index] = value;
      return next;
    });
  };

  const serverSummary = (
    <div className={styles.serverCard}>
      <div className={styles.serverEyebrow}>Serveur détecté · réseau local</div>
      <div className={styles.serverRow}>
        <div className={styles.serverGlyph}>⌂</div>
        <div className={styles.serverCopy}>
          <div className={styles.serverName}>maison.local</div>
          <div className={styles.serverMeta}>Accueil 2.4.1 · uptime 47j 13h · 192.168.1.42</div>
        </div>
        <StatusDot label="en ligne" tone="success" />
      </div>
    </div>
  );

  const heroFooter = (
    <div className={styles.heroFooter}>
      <span>Europe/Paris (UTC+1)</span>
      <span>·</span>
      <span>v 2.4.1</span>
    </div>
  );

  const credentialsBody = (
    <form
      className={styles.form}
      onSubmit={(event) => {
        event.preventDefault();
        setStep('mfa');
      }}
    >
      <div className={styles.formFields}>
        <Field hint="ou nom d'utilisateur" label="Adresse e-mail" variant="editorial">
          <TextField
            autoComplete="email"
            onChange={(event) => setEmail(event.target.value)}
            size="lg"
            type="email"
            value={email}
            variant="underline"
          />
        </Field>

        <Field label="Mot de passe" variant="editorial">
          <div className={styles.passwordRow}>
            <TextField
              autoComplete="current-password"
              className={styles.passwordInput}
              onChange={(event) => setPassword(event.target.value)}
              size="lg"
              type={showPassword ? 'text' : 'password'}
              value={password}
              variant="underline"
            />
            <button className={styles.inlineButton} onClick={() => setShowPassword((current) => !current)} type="button">
              {showPassword ? 'Masquer' : 'Afficher'}
            </button>
          </div>
        </Field>

        <div className={styles.formMetaRow}>
          <label className={styles.checkboxRow}>
            <input
              checked={remember}
              className={styles.checkbox}
              onChange={(event) => setRemember(event.target.checked)}
              type="checkbox"
            />
            <span>Se souvenir 30 jours</span>
          </label>
          <a className={styles.inlineLink} href="#">
            Mot de passe oublié ?
          </a>
        </div>
      </div>

      <div className={styles.formActions}>
        <Button className={styles.primaryButton} size="lg" type="submit">
          Continuer
        </Button>

        <div className={styles.separatorRow}>
          <span className={styles.separatorLine} />
          <span className={styles.separatorLabel}>ou bien</span>
          <span className={styles.separatorLine} />
        </div>

        <Button className={styles.secondaryButton} size="lg" variant="secondary">
          Utiliser une passkey
        </Button>
      </div>
    </form>
  );

  const mfaBody = (
    <div className={styles.mfaStack}>
      <div className={styles.mfaCodeRow}>
        {code.map((digit, index) => (
          <input
            className={styles.mfaDigit}
            inputMode="numeric"
            key={index}
            maxLength={1}
            onChange={(event) => updateDigit(index, event.target.value)}
            value={digit}
          />
        ))}
      </div>

      <div className={styles.mfaMeta}>Code valide encore 0:42</div>

      <Button className={styles.primaryButton} disabled={code.some((digit) => digit.length === 0)} size="lg">
        Se connecter
      </Button>

      <div className={styles.linkRowCentered}>
        <a className={styles.inlineLink} href="#">
          Recevoir un code SMS
        </a>
        <span className={styles.dotDivider}>·</span>
        <a className={styles.inlineLink} href="#">
          Code de récupération
        </a>
      </div>
    </div>
  );

  return (
    <div className={styles.storyRoot}>
      <LoginPage
        heroDescription="Pilotez l'éclairage, le chauffage, les caméras, les agents conversationnels et l'inventaire familial depuis une interface unique. Aucune donnée ne sort de votre serveur sans votre permission."
        heroEyebrow="Connexion · Serveur d'intendance"
        heroFooter={heroFooter}
        heroSupplementary={serverSummary}
        heroTitle={<>Votre maison, <em>posée</em> chez vous — sans cloud obligatoire.</>}
        panelDescription={step === 'credentials' ? <>Connexion locale sur <span className={styles.panelMono}>maison.local</span>.</> : <>Code généré par votre application d'authentification pour <span className={styles.panelMono}>{email}</span>.</>}
        panelEyebrow={step === 'credentials' ? 'Étape un · Identifiez-vous' : 'Étape deux · Vérification'}
        panelFooter={
          step === 'credentials' ? (
            <div className={styles.linkRowCentered}>
              Première fois ici ?
              <a className={styles.inlineLinkStrong} href="#">
                Demander un accès à un administrateur
              </a>
            </div>
          ) : null
        }
        panelLeadAction={
          step === 'mfa' ? (
            <Button onClick={() => setStep('credentials')} size="sm" variant="secondary">
              Retour
            </Button>
          ) : null
        }
        panelTitle={step === 'credentials' ? 'Bon retour.' : 'Six chiffres.'}
        topBarBrand="Accueil"
        topBarMeta="N° XLVII · lundi 29 avril 2026 · home.castel.fr"
        topBarStatus="TLS local · 1.3"
      >
        {step === 'credentials' ? credentialsBody : mfaBody}
      </LoginPage>
    </div>
  );
}

const meta = {
  title: 'Compositions/LoginPage',
  parameters: {
    layout: 'fullscreen',
  },
} satisfies Meta;

export default meta;

type Story = StoryObj<typeof meta>;

export const Credentials: Story = {
  render: () => <LoginCanvas initialStep="credentials" />,
};

export const MfaStep: Story = {
  render: () => <LoginCanvas initialStep="mfa" />,
};