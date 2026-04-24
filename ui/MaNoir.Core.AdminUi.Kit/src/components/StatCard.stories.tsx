import type { Meta, StoryObj } from '@storybook/react-vite';
import { StatCard } from './StatCard';

const meta = {
  title: 'Patterns/StatCard',
  component: StatCard,
  args: {
    value: '20',
    label: 'Possessions',
    detail: '2 193 € estimés',
  },
} satisfies Meta<typeof StatCard>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const Attention: Story = {
  args: {
    tone: 'attention',
    value: '4',
    label: 'Prêts',
    detail: '2 en retard',
  },
};