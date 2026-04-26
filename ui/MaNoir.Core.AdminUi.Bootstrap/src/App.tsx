import * as React from 'react';
import { AttentionPanel, Card, EmptyState, PageHeader } from '@manoir-app/core-admin-ui-kit';
import {
  ApiProblemError,
  type InitialSetupRequest,
  type InitialSetupStatus,
  type UserModel,
  getCurrentUser,
  getSetupStatus,
  initializeSetup,
  loginUser,
  logoutUser,
} from './lib/api';
import { InitialSetupPage } from './pages/InitialSetupPage';
import { LoginPage } from './pages/LoginPage';
import { SessionLandingPage } from './pages/SessionLandingPage';

type ScreenState =
  | { kind: 'loading'; label: string }
  | { kind: 'login'; error?: string }
  | { kind: 'setup'; error?: string; status: InitialSetupStatus }
  | { kind: 'session'; user: UserModel }
  | { kind: 'fatal'; error: string };

type Presentation = {
  badge: string;
  heading: string;
  description: string;
};

export function App() {
  const [screenState, setScreenState] = React.useState<ScreenState>({ kind: 'loading', label: 'Inspecting local Core state…' });
  const [isPending, startTransition] = React.useTransition();
  const presentation = getPresentation(screenState);

  React.useEffect(() => {
    void loadBootstrapState();
  }, []);

  async function loadBootstrapState() {
    startTransition(() => setScreenState({ kind: 'loading', label: 'Inspecting local Core state…' }));

    try {
      const setupStatus = await getSetupStatus();
      if (setupStatus.canInitialize) {
        startTransition(() => setScreenState({ kind: 'setup', status: setupStatus }));
        return;
      }

      try {
        const user = await getCurrentUser();
        startTransition(() => setScreenState({ kind: 'session', user }));
      } catch (error) {
        if (isUnauthorized(error)) {
          startTransition(() => setScreenState({ kind: 'login' }));
          return;
        }

        throw error;
      }
    } catch (error) {
      startTransition(() => setScreenState({ kind: 'fatal', error: toDisplayError(error) }));
    }
  }

  async function handleLogin(userId: string, password: string) {
    try {
      const response = await loginUser({ userId, password }, true);
      startTransition(() => setScreenState({ kind: 'session', user: response.user }));
    } catch (error) {
      if (isUnauthorized(error)) {
        startTransition(() => setScreenState({ kind: 'login', error: toDisplayError(error) }));
        return;
      }

      startTransition(() => setScreenState({ kind: 'fatal', error: toDisplayError(error) }));
    }
  }

  async function handleSetup(request: InitialSetupRequest) {
    try {
      await initializeSetup(request);
      const response = await loginUser(
        {
          userId: request.adminUserId,
          password: request.adminPassword,
        },
        true,
      );
      startTransition(() => setScreenState({ kind: 'session', user: response.user }));
    } catch (error) {
      if (screenState.kind === 'setup') {
        startTransition(() => setScreenState({ kind: 'setup', status: screenState.status, error: toDisplayError(error) }));
        return;
      }

      startTransition(() => setScreenState({ kind: 'fatal', error: toDisplayError(error) }));
    }
  }

  async function handleLogout() {
    try {
      await logoutUser();
      await loadBootstrapState();
    } catch (error) {
      startTransition(() => setScreenState({ kind: 'fatal', error: toDisplayError(error) }));
    }
  }

  return (
    <div className="mn-kit-theme bootstrap-root">
      <div className="bootstrap-backdrop" aria-hidden="true" />
      <div className="bootstrap-grid-overlay" aria-hidden="true" />

      <main className="bootstrap-shell">
        <section className="bootstrap-hero">
          <PageHeader
            eyebrow={presentation.badge}
            title={presentation.heading}
            description={presentation.description}
          />
        </section>

        <section className="bootstrap-stage-simple">
          <section className="bootstrap-stage">
            {screenState.kind === 'loading' ? <LoadingState label={screenState.label} /> : null}
            {screenState.kind === 'login' ? (
              <LoginPage busy={isPending} error={screenState.error} onSubmit={handleLogin} />
            ) : null}
            {screenState.kind === 'setup' ? (
              <InitialSetupPage busy={isPending} error={screenState.error} onSubmit={handleSetup} status={screenState.status} />
            ) : null}
            {screenState.kind === 'session' ? (
              <SessionLandingPage busy={isPending} onLogout={handleLogout} user={screenState.user} />
            ) : null}
            {screenState.kind === 'fatal' ? <FatalState error={screenState.error} onRetry={loadBootstrapState} /> : null}
          </section>
        </section>
      </main>
    </div>
  );
}

function LoadingState({ label }: { label: string }) {
  return (
    <Card className="bootstrap-panel bootstrap-panel-centered">
      <EmptyState eyebrow="Handshake" heading="Checking the Core instance" description={label} />
    </Card>
  );
}

function FatalState({ error, onRetry }: { error: string; onRetry: () => Promise<void> }) {
  return (
    <AttentionPanel
      eyebrow="Blocking issue"
      title="The bootstrap flow could not continue"
      description={error}
      actions={
        <button className="bootstrap-inline-button" onClick={() => void onRetry()} type="button">
          Retry bootstrap check
        </button>
      }
    />
  );
}

function getPresentation(screenState: ScreenState): Presentation {
  switch (screenState.kind) {
    case 'loading':
      return {
        badge: 'Checking state',
        heading: 'The shell is deciding which entry point to expose.',
        description: 'The page first checks whether this Core instance needs setup, login, or a direct handoff into the current session.',
      };
    case 'setup':
      return {
        badge: 'MANOIR',
        heading: 'Hi There !',
        description: "Seems like you're new here. This instance is empty, let's get started by creating a admin user.",
      };
    case 'session':
      return {
        badge: 'Authenticated',
        heading: 'Bootstrap is done and the first admin session is open.',
        description: 'This state stays intentionally small until the first real secured screens are plugged in.',
      };
    case 'fatal':
      return {
        badge: 'Blocked',
        heading: 'A blocking issue stopped the bootstrap flow.',
        description: 'The error is shown directly below so the operator can retry or inspect the backend state.',
      };
    case 'login':
    default:
      return {
        badge: 'Login',
        heading: 'The instance is ready. Enter the admin credentials.',
        description: 'Only the login action remains here. The page keeps the rest of the context deliberately out of the way.',
      };
  }
}

function isUnauthorized(error: unknown) {
  return error instanceof ApiProblemError && error.status === 401;
}

function toDisplayError(error: unknown) {
  if (error instanceof ApiProblemError) {
    if (error.problem.detail) {
      return `${error.problem.title ?? 'Request failed'} - ${error.problem.detail}`;
    }

    return error.problem.title ?? `Request failed with status ${error.status}.`;
  }

  if (error instanceof Error && error.message) {
    return error.message;
  }

  return 'An unexpected frontend error occurred.';
}
