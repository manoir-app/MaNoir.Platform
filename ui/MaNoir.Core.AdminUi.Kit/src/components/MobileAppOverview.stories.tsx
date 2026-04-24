import type { Meta, StoryObj } from '@storybook/react-vite';
import { EditorialBand } from './EditorialBand';
import { ListRow } from './ListRow';
import { MetricList } from './MetricList';
import { ScanPanel } from './ScanPanel';
import { SearchInput } from './SearchInput';
import { SectionTitle } from './SectionTitle';
import { ShellHeader } from './ShellHeader';
import { StatusPill } from './StatusPill';

function MobileOverviewCanvas() {
  return (
    <div
      className="mn-kit-theme"
      style={{
        alignItems: 'flex-start',
        background: 'var(--mn-color-paper)',
        display: 'flex',
        gap: '2.5rem',
        justifyContent: 'center',
        minHeight: '100vh',
        padding: '0 0 2rem',
      }}
    >
      <div
        style={{
          background: 'var(--mn-color-paper)',
          borderLeft: '1px solid var(--mn-color-line)',
          borderRight: '1px solid var(--mn-color-line)',
          minHeight: '100vh',
          width: '390px',
        }}
      >
        <ShellHeader
          items={[
            { id: 'home', label: 'Accueil', active: true },
            { id: 'scan', label: 'Scanner' },
            { id: 'food', label: 'Garde-manger' },
            { id: 'shopping', label: 'Courses' },
            { id: 'moves', label: 'Mouvements' },
            { id: 'loans', label: 'Prêts' },
          ]}
        />

        <div style={{ display: 'grid', gap: '1.75rem', padding: '0.65rem 1.5rem 2rem' }}>
          <div>
            <div style={{ color: 'var(--mn-color-ink-muted)', fontFamily: 'var(--mn-font-mono)', fontSize: '0.7rem', letterSpacing: '0.14em', textTransform: 'uppercase' }}>
              Lundi 20 avril
            </div>
            <div style={{ fontFamily: 'var(--mn-font-display)', fontSize: '2rem', fontWeight: 500, letterSpacing: '-0.04em', lineHeight: 1, marginTop: '0.35rem' }}>
              Bonjour Paul.
            </div>
          </div>

          <EditorialBand>
            <div style={{ fontFamily: 'var(--mn-font-display)', fontSize: '1.15rem', lineHeight: 1.05 }}>Scanner un QR container.</div>
            <div style={{ color: 'var(--mn-color-ink-soft)', fontSize: '0.86rem' }}>Un scan suffit pour ouvrir le bon bac, sans recherche intermédiaire.</div>
          </EditorialBand>

          <div style={{ display: 'grid', gap: '0.75rem' }}>
            <SectionTitle eyebrow="À consommer" heading="Le garde-manger presse." />
            <MetricList
              items={[
                {
                  id: 'salmon',
                  label: 'Saumon fumé',
                  detail: '100g · Frigo · Frais',
                  status: 'Expiré',
                  statusTone: 'danger',
                },
                {
                  id: 'ham',
                  label: 'Jambon blanc',
                  detail: '150g · Frigo · Frais',
                  status: 'Urgent',
                  statusTone: 'warning',
                },
              ]}
            />
          </div>

          <div style={{ display: 'grid', gap: '0.65rem' }}>
            <div style={{ color: 'var(--mn-color-ink-muted)', fontSize: '0.7rem', fontFamily: 'var(--mn-font-mono)', letterSpacing: '0.14em', textTransform: 'uppercase' }}>
              Prêts en cours
            </div>
            <ListRow
              clickable
              subtitle="Julien M. · retour le 21 avril"
              title="Réchaud MSR PocketRocket"
              trailing={<StatusPill tone="accent">En cours</StatusPill>}
            />
            <ListRow
              clickable
              subtitle="Clara B. · en retard"
              title="Ski Rossignol Experience 76"
              trailing={<StatusPill tone="danger">Retard</StatusPill>}
            />
          </div>
        </div>
      </div>

      <div
        style={{
          background: 'var(--mn-color-paper)',
          borderLeft: '1px solid var(--mn-color-line)',
          borderRight: '1px solid var(--mn-color-line)',
          minHeight: '100vh',
          width: '390px',
        }}
      >
        <ShellHeader
          items={[
            { id: 'home', label: 'Accueil' },
            { id: 'scan', label: 'Scanner', active: true },
            { id: 'food', label: 'Garde-manger' },
            { id: 'shopping', label: 'Courses' },
            { id: 'moves', label: 'Mouvements' },
            { id: 'loans', label: 'Prêts' },
          ]}
        />

        <div style={{ display: 'grid', gap: '1.25rem', padding: '0.9rem 1.5rem 2rem' }}>
          <div style={{ color: 'var(--mn-color-ink-muted)', fontFamily: 'var(--mn-font-mono)', fontSize: '0.7rem', letterSpacing: '0.14em', textTransform: 'uppercase' }}>
            Scanner
          </div>
          <div style={{ fontFamily: 'var(--mn-font-display)', fontSize: '2rem', fontWeight: 500, letterSpacing: '-0.04em', lineHeight: 1 }}>Cadrer le QR.</div>
          <ScanPanel />
          <SearchInput id="mobile-search" label="Recherche rapide" placeholder="Container, objet, contact..." shortcut={null} />
          <div style={{ display: 'grid', gap: '0.25rem' }}>
            <div style={{ color: 'var(--mn-color-ink-muted)', fontFamily: 'var(--mn-font-mono)', fontSize: '0.7rem', letterSpacing: '0.14em', textTransform: 'uppercase' }}>
              Récemment scannés
            </div>
            <ListRow clickable subtitle="Salon / Bibliothèque Kallax" title="INV-C001 — Archives Photos" />
            <ListRow clickable subtitle="Garage / Armoire outils" title="INV-C007 — Outillage électrique" />
            <ListRow clickable subtitle="Cuisine / Placard haut gauche" title="INV-C012 — Épices & condiments" />
          </div>
        </div>
      </div>
    </div>
  );
}

const meta = {
  title: 'Compositions/MobileAppOverview',
  parameters: {
    layout: 'fullscreen',
  },
} satisfies Meta;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => <MobileOverviewCanvas />,
};