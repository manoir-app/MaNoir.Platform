import type { Meta, StoryObj } from '@storybook/react-vite';
import { ScanPanel } from './ScanPanel';

const meta = {
  title: 'Mobile/ScanPanel',
  component: ScanPanel,
} satisfies Meta<typeof ScanPanel>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: (args) => <div style={{ maxWidth: 390 }}><ScanPanel {...args} /></div>,
};