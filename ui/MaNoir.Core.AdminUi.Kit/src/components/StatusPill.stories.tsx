import type { Meta, StoryObj } from '@storybook/react-vite';
import { StatusPill } from './StatusPill';

const meta = {
  title: 'Primitives/StatusPill',
  component: StatusPill,
  args: {
    children: 'Urgent',
    tone: 'danger',
  },
} satisfies Meta<typeof StatusPill>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Danger: Story = {};

export const Warning: Story = {
  args: {
    children: 'À surveiller',
    tone: 'warning',
  },
};

export const Accent: Story = {
  args: {
    children: 'En cours',
    tone: 'accent',
  },
};