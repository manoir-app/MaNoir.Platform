import * as React from 'react';
import { AttentionPanel, Button, Card, Field, PageHeader, TextField } from '@manoir-app/core-admin-ui-kit';

interface LoginPageProps {
  busy: boolean;
  error?: string;
  onSubmit: (userId: string, password: string) => Promise<void>;
}

export function LoginPage({ busy, error, onSubmit }: LoginPageProps) {
  const [userId, setUserId] = React.useState('');
  const [password, setPassword] = React.useState('');

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onSubmit(userId, password);
  }

  return (
    <Card className="bootstrap-panel bootstrap-auth-card">
      <PageHeader
        eyebrow="Session"
        title="Sign in to the Core admin"
        description="Enter the admin identifier and password to open the local session."
      />

      {error ? (
        <AttentionPanel
          eyebrow="Authentication"
          title="The sign-in failed"
          description={error}
        />
      ) : null}

      <form className="bootstrap-form" onSubmit={(event) => void handleSubmit(event)}>
        <Field htmlFor="login-user-id" hint="Use the canonical admin identifier." label="User identifier" required>
          <TextField
            autoFocus
            autoComplete="username"
            id="login-user-id"
            onChange={(event) => setUserId(event.target.value)}
            placeholder="admin"
            value={userId}
          />
        </Field>

        <Field htmlFor="login-password" hint="The browser never receives the JWT when the interactive flow is used." label="Password" required>
          <TextField
            autoComplete="current-password"
            id="login-password"
            onChange={(event) => setPassword(event.target.value)}
            placeholder="Your admin password"
            type="password"
            value={password}
          />
        </Field>

        <div className="bootstrap-actions">
          <Button disabled={busy} size="lg" type="submit">
            {busy ? 'Opening session…' : 'Open admin session'}
          </Button>
        </div>
      </form>
    </Card>
  );
}