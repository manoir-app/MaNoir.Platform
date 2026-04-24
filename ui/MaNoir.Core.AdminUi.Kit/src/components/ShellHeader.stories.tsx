import type { Meta, StoryObj } from '@storybook/react-vite';
import { ShellHeader } from './ShellHeader';

const desktopItems = [
  { id: 'desktop', label: 'Desktop', active: true },
  { id: 'mobile-web', label: 'Mobile web' },
  { id: 'app', label: 'App téléphone' },
];

const compactItems = [
  { id: 'home', label: 'Accueil', active: true },
  { id: 'scan', label: 'Scanner' },
  { id: 'food', label: 'Garde-manger' },
  { id: 'shopping', label: 'Courses' },
  { id: 'moves', label: 'Mouvements' },
  { id: 'loans', label: 'Prêts' },
];

const meta = {
  title: 'Patterns/ShellHeader',
  component: ShellHeader,
  args: {
    items: desktopItems,
  },
} satisfies Meta<typeof ShellHeader>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Desktop: Story = {
  render: (args) => (
    <div className="mn-kit-theme" style={{ background: 'var(--mn-color-paper)', minHeight: '10rem' }}>
      <ShellHeader
        {...args}
        title={
          <>
            <span style={{ color: 'var(--mn-color-ink)', fontWeight: 600 }}>Intendance</span>
            <span> — maison</span>
          </>
        }
      />
    </div>
  ),
};

export const Compact: Story = {
  args: {
    items: compactItems,
  },
  render: (args) => (
    <div className="mn-kit-theme" style={{ background: 'var(--mn-color-paper)', width: '390px' }}>
      <ShellHeader {...args} />
    </div>
  ),
};