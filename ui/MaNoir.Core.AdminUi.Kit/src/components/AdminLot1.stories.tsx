import * as React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import storyStyles from './AdminLot1.stories.module.css';
import { AdminShell } from './AdminShell';
import { Avatar } from './Avatar';
import { Button } from './Button';
import { DataTable } from './DataTable';
import { Dialog } from './Dialog';
import { Field } from './Field';
import { InlineTabs } from './InlineTabs';
import { PageHeader } from './PageHeader';
import { SearchInput } from './SearchInput';
import { ShellHeader } from './ShellHeader';
import { SidebarNav } from './SidebarNav';
import { StatStrip } from './StatStrip';
import { StatusDot } from './StatusDot';
import { ToggleSwitch } from './ToggleSwitch';

function AdminLot1Canvas() {
  const [tab, setTab] = React.useState('plugins');
  const [dialogOpen, setDialogOpen] = React.useState(false);
  const [enabledRows, setEnabledRows] = React.useState<Record<string, boolean>>({
    hue: true,
    frigate: true,
    spotify: false,
  });

  const rows = [
    { id: 'hue', name: 'Philips Hue', kind: 'Éclairage', version: '2.4.0', devices: 12, status: 'En ligne' },
    { id: 'frigate', name: 'Frigate', kind: 'Sécurité', version: '0.13.2', devices: 6, status: 'Alerte' },
    { id: 'spotify', name: 'Spotify', kind: 'Média', version: '3.1.0', devices: 4, status: 'À reconnecter' },
  ];

  return (
    <div className={storyStyles.canvasRoot + ' mn-kit-theme'}>
      <AdminShell
        sidebar={
          <SidebarNav
            brand="Accueil"
            caption="maison · 84 app."
            footer={
              <div className={storyStyles.sidebarFooter}>
                <Avatar initials="PC" />
                <div>
                  <div className={storyStyles.sidebarName}>Paul Castel</div>
                  <div className={storyStyles.sidebarRole}>Propriétaire</div>
                </div>
              </div>
            }
            searchSlot={<SearchInput id="lot1-sidebar-search" placeholder="Rechercher" shortcut="⌘K" variant="bare" />}
            sections={[
              {
                id: 'main',
                items: [
                  { id: 'dashboard', label: 'Tableau', active: true },
                  { id: 'server', label: 'Serveur', meta: 'maison.local' },
                  { id: 'plugins', label: 'Plugins', meta: '8' },
                  { id: 'secrets', label: 'Secrets', meta: '12' },
                ],
              },
              {
                id: 'admin',
                title: 'Admin',
                items: [
                  { id: 'users', label: 'Utilisateurs', meta: '7' },
                  { id: 'logs', label: 'Logs' },
                ],
              },
            ]}
          />
        }
        topBar={<ShellHeader items={[{ id: 'desktop', label: 'Desktop', active: true }, { id: 'mobile-web', label: 'Mobile web' }, { id: 'app', label: 'App téléphone' }]} title="Accueil — maison.local" />}
      >
        <PageHeader
          actions={<><Button size="sm" variant="secondary">Tester</Button><Button size="sm">Enregistrer</Button></>}
          description="Première livraison des primitives nécessaires au shell admin éditorial : structure, tableaux, tabs, statuts, dialogue et contrôles compacts."
          eyebrow="Lot 1 · Admin UI Kit"
          title={<>Le socle <em className={storyStyles.accentWord}>éditorial</em> prend forme.</>}
          variant="hero"
        />

        <StatStrip
          items={[
            { id: 'uptime', label: 'Uptime serveur', value: '47j 13h', detail: 'Accueil 2.4.1' },
            { id: 'plugins', label: 'Plugins actifs', value: '8', detail: '10 installés' },
            { id: 'devices', label: 'Appareils', value: '84', detail: '16 pièces' },
            { id: 'alerts', label: 'Alertes', value: '3', detail: 'à revoir', tone: 'accent' },
          ]}
        />

        <div className={storyStyles.sectionStack}>
          <PageHeader
            actions={<Button onClick={() => setDialogOpen(true)} size="sm" variant="secondary">Nouveau plugin</Button>}
            description="Vue compacte de la surface lot 1 avec tabs ligne, tableau éditorial, switch et statuts légers."
            eyebrow="Plugins installés"
            title="Tableau éditorial"
            variant="page"
          />

          <InlineTabs
            items={[
              { id: 'plugins', label: 'Plugins', badge: '3' },
              { id: 'integrations', label: 'Intégrations', badge: '9' },
              { id: 'errors', label: 'Erreurs', badge: '2' },
            ]}
            onValueChange={setTab}
            value={tab}
            variant="line"
          />

          <DataTable
            columns={[
              {
                id: 'plugin',
                header: 'Plugin',
                cell: (row, index) => (
                  <div>
                    <div className={storyStyles.tablePrimary}>{String(index + 1).padStart(2, '0')} · {row.name}</div>
                    <div className={storyStyles.tableSecondary}>{row.kind}</div>
                  </div>
                ),
              },
              { id: 'status', header: 'Statut', cell: (row) => <StatusDot label={row.status} tone={row.status === 'En ligne' ? 'success' : row.status === 'Alerte' ? 'warning' : 'danger'} />, width: 140 },
              { id: 'version', header: 'Version', cell: (row) => row.version, mono: true, align: 'right', width: 100 },
              { id: 'devices', header: 'Appareils', cell: (row) => row.devices, mono: true, align: 'right', width: 100 },
              {
                id: 'enabled',
                header: 'Activé',
                cell: (row) => (
                  <div className={storyStyles.tableSwitchCell}>
                    <ToggleSwitch
                      checked={enabledRows[row.id] ?? false}
                      onCheckedChange={(checked) => setEnabledRows((current) => ({ ...current, [row.id]: checked }))}
                    />
                  </div>
                ),
                align: 'center',
                width: 84,
              },
            ]}
            rowKey={(row) => row.id}
            rows={rows}
          />

          <div className={storyStyles.dualPanel}>
            <Field hint="Utilisé pour la génération des certificats" label="Domaine public" labelAside="challenge ACME" variant="editorial">
              <SearchInput id="domain-search" placeholder="home.castel.fr" shortcut={null} variant="bare" />
            </Field>

            <div className={storyStyles.sideStack}>
              <InlineTabs
                items={[
                  { id: 'primary', label: 'Résidence principale' },
                  { id: 'secondary', label: 'Secondaire' },
                ]}
                onValueChange={() => undefined}
                value="primary"
                variant="pill"
              />
              <div className={storyStyles.personRow}>
                <Avatar name="Marie Castel" size="lg" tone="success" />
                <div>
                  <div className={storyStyles.personName}>Marie Castel</div>
                  <StatusDot label="Admin" tone="accent" />
                </div>
              </div>
            </div>
          </div>
        </div>

        <Dialog
          description="Ce dialogue reprend la structure éditoriale prévue pour les confirmations, créations et reconnects du back-office."
          eyebrow="Lot 1"
          footer={<><Button onClick={() => setDialogOpen(false)} variant="secondary">Annuler</Button><Button onClick={() => setDialogOpen(false)}>Créer</Button></>}
          onOpenChange={setDialogOpen}
          open={dialogOpen}
          size="md"
          title="Ajouter un plugin"
        >
          <div className={storyStyles.dialogFields}>
            <Field label="Nom du plugin" variant="editorial">
              <SearchInput id="plugin-name" placeholder="Matter / Thread" shortcut={null} variant="bare" />
            </Field>
            <Field label="Source" variant="editorial">
              <SearchInput id="plugin-source" placeholder="Officielle ou communautaire" shortcut={null} variant="bare" />
            </Field>
          </div>
        </Dialog>
      </AdminShell>
    </div>
  );
}

const meta = {
  title: 'Compositions/AdminLot1',
  parameters: {
    layout: 'fullscreen',
  },
} satisfies Meta;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => <AdminLot1Canvas />,
};

.sideStack {
  display: grid;
  gap: 0.75rem;
}

.personRow {
  align-items: center;
  display: flex;
  gap: 0.6rem;
}

.personName {
  font-weight: 500;
}

.dialogFields {
  display: grid;
  gap: 1rem;
}

@media (max-width: 959px) {
  .dualPanel {
    grid-template-columns: minmax(0, 1fr);
  }
}