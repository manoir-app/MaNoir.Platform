import type { Meta, StoryObj } from '@storybook/react-vite';
import { Button } from './Button';
import { EmptyState } from './EmptyState';

const meta = {
  title: 'Patterns/EmptyState',
  component: EmptyState,
  args: {
    eyebrow: 'Aucun résultat',
    heading: 'Rien à afficher pour le moment.',
    description: 'Ajustez vos filtres ou créez une première entrée pour démarrer ce module.',
    actions: <Button>Créer une entrée</Button>,
  },
} satisfies Meta<typeof EmptyState>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {};