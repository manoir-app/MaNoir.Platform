import type { Meta, StoryObj } from '@storybook/react-vite';
import { AttentionPanel } from './AttentionPanel';
import { Button } from './Button';

const meta = {
  title: 'Patterns/AttentionPanel',
  component: AttentionPanel,
  args: {
    title: '2 prêts en retard de retour.',
    description: 'Ski Rossignol Experience 76, Scie sauteuse Makita',
    actions: <Button size="sm">Relancer</Button>,
  },
} satisfies Meta<typeof AttentionPanel>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {};