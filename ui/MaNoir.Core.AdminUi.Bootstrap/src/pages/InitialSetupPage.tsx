import * as React from 'react';
import { AttentionPanel, Button, Card, Field, PageHeader, TextField } from '@manoir-app/core-admin-ui-kit';
import type { InitialSetupRequest, InitialSetupStatus } from '../lib/api';

interface InitialSetupPageProps {
  busy: boolean;
  error?: string;
  onSubmit: (request: InitialSetupRequest) => Promise<void>;
  status: InitialSetupStatus;
}

export function InitialSetupPage({ busy, error, onSubmit, status }: InitialSetupPageProps) {
  const [formState, setFormState] = React.useState<InitialSetupRequest>({
    adminUserId: 'admin',
    adminFirstName: '',
    adminName: '',
    adminCommonName: '',
    adminEmail: '',
    adminPassword: '',
    languageId: 'fr-FR',
    timeZoneId: 'Europe/Paris',
    countryId: 'FR',
  });

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onSubmit(formState);
  }

  function updateField<K extends keyof InitialSetupRequest>(key: K, value: InitialSetupRequest[K]) {
    setFormState((currentState) => ({ ...currentState, [key]: value }));
  }

  return (
    <Card className="bootstrap-panel bootstrap-panel-wide">
      <PageHeader
        eyebrow="First setup"
        title="Create the admin user"
        description="Start with the first admin account, then review the defaults below if needed."
        meta={<span className="bootstrap-meta">Mesh: {status.hasMesh ? 'present' : 'missing'} · Users: {status.hasUsers ? 'present' : 'missing'}</span>}
      />

      {error ? (
        <AttentionPanel
          eyebrow="Setup issue"
          title="The first setup could not be completed"
          description={error}
        />
      ) : null}

      <form className="bootstrap-form" onSubmit={(event) => void handleSubmit(event)}>
        <section className="bootstrap-form-section">
          <div className="bootstrap-form-section-header">
            <div className="bootstrap-kicker">Required first</div>
            <h3>Master admin account</h3>
            <p>These are the fields the operator must actually fill before the first run can complete.</p>
          </div>

          <div className="bootstrap-form-grid bootstrap-form-grid-two-columns">
            <Field htmlFor="setup-admin-user-id" label="Master admin identifier" required>
              <TextField autoFocus id="setup-admin-user-id" onChange={(event) => updateField('adminUserId', event.target.value)} placeholder="admin" value={formState.adminUserId} />
            </Field>

            <Field htmlFor="setup-admin-password" label="Master admin password" required>
              <TextField
                autoComplete="new-password"
                id="setup-admin-password"
                onChange={(event) => updateField('adminPassword', event.target.value)}
                placeholder="Choose a strong password"
                type="password"
                value={formState.adminPassword}
              />
            </Field>

            <Field htmlFor="setup-admin-first-name" label="First name">
              <TextField id="setup-admin-first-name" onChange={(event) => updateField('adminFirstName', event.target.value)} placeholder="Sarah" value={formState.adminFirstName} />
            </Field>

            <Field htmlFor="setup-admin-name" label="Last name">
              <TextField id="setup-admin-name" onChange={(event) => updateField('adminName', event.target.value)} placeholder="Martin" value={formState.adminName} />
            </Field>

            <Field htmlFor="setup-admin-common-name" label="Display name">
              <TextField id="setup-admin-common-name" onChange={(event) => updateField('adminCommonName', event.target.value)} placeholder="Sarah Martin" value={formState.adminCommonName} />
            </Field>

            <Field htmlFor="setup-admin-email" label="Email">
              <TextField id="setup-admin-email" onChange={(event) => updateField('adminEmail', event.target.value)} placeholder="sarah@example.com" type="email" value={formState.adminEmail} />
            </Field>
          </div>
        </section>

        <section className="bootstrap-form-section">
          <div className="bootstrap-form-section-header">
            <div className="bootstrap-kicker">Optional defaults</div>
            <h3>Default mesh settings</h3>
            <p>Leave these values as-is unless this instance needs another locale.</p>
          </div>

          <div className="bootstrap-form-grid bootstrap-form-grid-three-columns">
            <Field htmlFor="setup-language" hint="Culture name accepted by the Core API." label="Language">
              <TextField id="setup-language" onChange={(event) => updateField('languageId', event.target.value)} placeholder="fr-FR" value={formState.languageId} />
            </Field>

            <Field htmlFor="setup-timezone" hint="IANA identifier expected by the mesh settings logic." label="Time zone">
              <TextField id="setup-timezone" onChange={(event) => updateField('timeZoneId', event.target.value)} placeholder="Europe/Paris" value={formState.timeZoneId} />
            </Field>

            <Field htmlFor="setup-country" hint="ISO region code." label="Country">
              <TextField id="setup-country" onChange={(event) => updateField('countryId', event.target.value)} placeholder="FR" value={formState.countryId} />
            </Field>
          </div>
        </section>

        <div className="bootstrap-actions bootstrap-actions-full">
          <Button disabled={busy} size="lg" type="submit">
            {busy ? 'Initializing local Core…' : 'Initialize local Core'}
          </Button>
        </div>
      </form>
    </Card>
  );
}