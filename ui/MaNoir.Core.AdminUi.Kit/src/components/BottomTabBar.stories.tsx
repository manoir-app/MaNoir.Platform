import type { Meta, StoryObj } from '@storybook/react-vite';
import { BottomTabBar } from './BottomTabBar';

const meta = {
  title: 'Mobile/BottomTabBar',
  component: BottomTabBar,
  args: {
    items: [
      { id: 'home', label: 'Accueil', icon: '⌂', active: true },
      { id: 'scan', label: 'Scanner', icon: '⌕' },
      { id: 'food', label: 'Stock', icon: '◫' },
      { id: 'list', label: 'Liste', icon: '≡' },
      { id: 'more', label: 'Plus', icon: '…' },
    ],
  },
} satisfies Meta<typeof BottomTabBar>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: (args) => <div style={{ maxWidth: 390 }}><BottomTabBar {...args} /></div>,
};