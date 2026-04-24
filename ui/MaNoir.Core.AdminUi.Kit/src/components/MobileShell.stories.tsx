import type { Meta, StoryObj } from '@storybook/react-vite';
import { BottomTabBar } from './BottomTabBar';
import { MobileShell } from './MobileShell';

const meta = {
  title: 'Mobile/MobileShell',
  component: MobileShell,
} satisfies Meta<typeof MobileShell>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => (
    <MobileShell
      bottomBar={<BottomTabBar items={[{ id: 'home', label: 'Accueil', icon: '⌂', active: true }, { id: 'more', label: 'Plus', icon: '…' }]} />}
    >
      <div style={{ padding: '20px 24px 32px' }}>
        <div style={{ color: 'var(--mn-color-ink-muted)', fontFamily: 'var(--mn-font-mono)', fontSize: '0.7rem', letterSpacing: '0.14em', textTransform: 'uppercase' }}>
          Aperçu
        </div>
        <div style={{ fontFamily: 'var(--mn-font-display)', fontSize: '2rem', fontWeight: 500, letterSpacing: '-0.04em', lineHeight: 1, marginTop: '0.35rem' }}>
          Shell téléphone.
        </div>
      </div>
    </MobileShell>
  ),
};