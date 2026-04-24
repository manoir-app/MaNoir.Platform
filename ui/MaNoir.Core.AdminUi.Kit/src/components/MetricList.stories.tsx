import type { Meta, StoryObj } from '@storybook/react-vite';
import { MetricList } from './MetricList';

const meta = {
  title: 'Patterns/MetricList',
  component: MetricList,
  args: {
    items: [
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
    ],
  },
} satisfies Meta<typeof MetricList>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {};