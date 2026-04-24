import type { Meta, StoryObj } from '@storybook/react-vite';
import { Button } from './Button';

const meta = {
  title: 'Primitives/Button',
  component: Button,
  args: {
    children: 'Relancer',
    variant: 'primary',
    size: 'md',
  },
  argTypes: {
    onClick: { action: 'clicked' },
  },
} satisfies Meta<typeof Button>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Primary: Story = {};

export const Secondary: Story = {
  args: {
    variant: 'secondary',
    children: 'Voir le détail',
  },
};

export const Quiet: Story = {
  args: {
    variant: 'quiet',
    children: 'Filtrer',
  },
};

export const Danger: Story = {
  args: {
    variant: 'danger',
    children: 'Archiver',
  },
};