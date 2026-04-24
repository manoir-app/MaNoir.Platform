import type { Meta, StoryObj } from '@storybook/react-vite';
import { StatusPill } from './StatusPill';
import { ListRow } from './ListRow';

const meta = {
  title: 'Patterns/ListRow',
  component: ListRow,
  args: {
    leading: '1j',
    title: 'Jambon blanc',
    subtitle: '150g · Frigo · Frais',
    trailing: <StatusPill tone="warning">Urgent</StatusPill>,
    clickable: true,
  },
} satisfies Meta<typeof ListRow>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {};