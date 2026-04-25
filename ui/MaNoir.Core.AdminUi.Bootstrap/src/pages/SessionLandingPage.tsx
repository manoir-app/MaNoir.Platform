import { AttentionPanel, Button, Card, EmptyState, PageHeader } from '@manoir-app/core-admin-ui-kit';
import type { UserModel } from '../lib/api';

interface SessionLandingPageProps {
  busy: boolean;
  onLogout: () => Promise<void>;
  user: UserModel;
}

export function SessionLandingPage({ busy, onLogout, user }: SessionLandingPageProps) {
  const displayName = user.commonName || [user.firstName, user.name].filter(Boolean).join(' ') || user.id;

  return (
    <Card className="bootstrap-panel bootstrap-panel-centered bootstrap-session-card">
      <PageHeader
        eyebrow="Authenticated"
        title={`Session ready for ${displayName}`}
        description="The bootstrap module stops here on purpose. The next step is to plug the first authenticated back-office pages into the same shell."
      />

      <div className="bootstrap-callout-strip">
        <span>Session opened</span>
        <span>{user.isMain ? 'Master admin privileges' : 'Standard user privileges'}</span>
        <span>{user.mainEmail || 'No email yet'}</span>
      </div>

      <EmptyState
        eyebrow="Next step"
        heading="The Core admin shell is ready to grow"
        description="Login and setup now converge on the same authenticated landing state. This is the right place to connect the first secured admin routes next."
        actions={
          <Button disabled={busy} onClick={() => void onLogout()} variant="secondary">
            {busy ? 'Closing session…' : 'Close session'}
          </Button>
        }
      />

      <AttentionPanel
        eyebrow="Current identity"
        title={displayName}
        description={user.mainEmail || 'No contact email registered yet.'}
      >
        <div className="bootstrap-identity-grid">
          <div>
            <span className="bootstrap-identity-label">User id</span>
            <strong>{user.id}</strong>
          </div>
          <div>
            <span className="bootstrap-identity-label">Role</span>
            <strong>{user.isMain ? 'Master admin' : 'User'}</strong>
          </div>
        </div>
      </AttentionPanel>
    </Card>
  );
}