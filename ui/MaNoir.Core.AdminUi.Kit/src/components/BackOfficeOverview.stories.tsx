import type { Meta, StoryObj } from '@storybook/react-vite';
import { AttentionPanel } from './AttentionPanel';
import { Button } from './Button';
import { EmptyState } from './EmptyState';
import { MetricList } from './MetricList';
import { PageHeader } from './PageHeader';
import { SearchInput } from './SearchInput';
import { SectionTitle } from './SectionTitle';
import { ShellHeader } from './ShellHeader';
import { SidebarNav } from './SidebarNav';
import { StatCard } from './StatCard';
import { StatusPill } from './StatusPill';

function OverviewCanvas() {
  return (
    <div
      className="mn-kit-theme"
      style={{
        background: 'var(--mn-color-paper)',
        minHeight: '100vh',
      }}
    >
      <ShellHeader
        items={[
          { id: 'desktop', label: 'Desktop', active: true },
          { id: 'mobile-web', label: 'Mobile web' },
          { id: 'app', label: 'App téléphone' },
        ]}
        title={
          <>
            <span style={{ color: 'var(--mn-color-ink)', fontWeight: 600 }}>Intendance</span>
            <span> — maison</span>
          </>
        }
      />

      <div style={{ display: 'grid', gridTemplateColumns: '248px minmax(0, 1fr)', minHeight: 'calc(100vh - 57px)' }}>
        <SidebarNav
          brand="Intendance"
          caption="maison · 48 réf."
          searchSlot={
            <>
              <div style={{ color: 'var(--mn-color-ink)', fontSize: '0.8125rem', fontWeight: 500 }}>Recherche</div>
              <SearchInput id="overview-search" placeholder="Rechercher" shortcut="⌘K" />
            </>
          }
          sections={[
            {
              id: 'primary',
              items: [
                { id: 'dashboard', label: 'Tableau', active: true },
                { id: 'possessions', label: 'Possessions', meta: '20' },
                { id: 'pantry', label: 'Garde-manger', meta: '28' },
                { id: 'recipes', label: 'Recettes', meta: '9' },
                { id: 'shopping', label: 'Courses', meta: '10' },
                { id: 'movements', label: 'Mouvements', meta: '10' },
                { id: 'loans', label: 'Prêts', meta: '4' },
              ],
            },
            {
              id: 'admin',
              title: 'Admin',
              items: [
                { id: 'rooms', label: 'Pièces & rangements' },
                { id: 'stores', label: 'Magasins' },
                { id: 'contacts', label: 'Contacts' },
              ],
            },
          ]}
        />

        <div style={{ display: 'grid', gap: '2rem', padding: '3rem 4rem 2.25rem' }}>
          <PageHeader
            description="7 produits à consommer sous trois jours, 2 prêts en retard, et 10 articles attendent sur votre liste de courses."
            eyebrow="N° XLVII • Lundi 20 avril 2026"
            title={
              <>
                Bonjour Paul.
                <br />
                Rien de grave aujourd&apos;hui <span style={{ color: 'var(--mn-color-ink-faint)' }}>—</span> juste{' '}
                <span style={{ color: 'var(--mn-color-accent)', fontStyle: 'italic' }}>quelques attentions.</span>
              </>
            }
          />

          <div style={{ borderTop: '1px solid var(--mn-color-line)', display: 'grid', gridTemplateColumns: 'repeat(4, minmax(0, 1fr))' }}>
            <StatCard detail="2 193 € estimés" label="Possessions" value="20" />
            <StatCard detail="7 pièces · 11 meubles" label="Containers" value="12" />
            <StatCard detail="10 à surveiller" label="Produits" value="28" />
            <StatCard detail="2 en retard" label="Prêts" tone="attention" value="4" />
          </div>

          <div style={{ display: 'grid', gap: '1.5rem', gridTemplateColumns: 'minmax(0, 1.5fr) minmax(320px, 1fr)' }}>
            <div style={{ display: 'grid', gap: '1rem' }}>
              <SectionTitle
                actions={<StatusPill tone="warning">3 urgences</StatusPill>}
                description="Classé par date de péremption. Un clic sort le produit du stock."
                eyebrow="À consommer bientôt"
                heading="Le garde-manger presse."
              />
              <MetricList
                items={[
                  {
                    id: 'salmon',
                    eyebrow: '—',
                    label: 'Saumon fumé',
                    detail: '100g · Frigo · Frais',
                    status: 'Expiré',
                    statusTone: 'danger',
                  },
                  {
                    id: 'basil',
                    eyebrow: '—',
                    label: 'Basilic frais',
                    detail: '1 bott. · Frigo · Herbes',
                    status: 'Expiré',
                    statusTone: 'danger',
                  },
                  {
                    id: 'ham',
                    eyebrow: '1j',
                    label: 'Jambon blanc',
                    detail: '150g · Frigo · Frais',
                    status: 'Urgent',
                    statusTone: 'warning',
                  },
                ]}
              />
            </div>

            <div style={{ display: 'grid', gap: '1rem' }}>
              <AttentionPanel
                actions={<Button size="sm">Relancer</Button>}
                description="Ski Rossignol Experience 76 — Clara B. Scie sauteuse Makita — Maman"
                title="2 prêts en retard de retour."
              />
              <EmptyState
                actions={<Button variant="secondary">Configurer le module</Button>}
                description="Aucune tournée planifiée pour aujourd’hui. Vous pouvez préparer une première collecte ou laisser cet espace vide tant que le flux n’existe pas."
                eyebrow="Prêts en cours"
                heading="Hors les murs."
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

const meta = {
  title: 'Compositions/BackOfficeOverview',
  parameters: {
    layout: 'fullscreen',
  },
} satisfies Meta;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => <OverviewCanvas />,
};