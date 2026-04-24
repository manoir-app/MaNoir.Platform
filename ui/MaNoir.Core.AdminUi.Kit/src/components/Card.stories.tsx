import type { Meta, StoryObj } from '@storybook/react-vite';
import { Card } from './Card';

const meta = {
  title: 'Patterns/Card',
  component: Card,
  args: {
    children: (
      <div style={{ display: 'grid', gap: '0.75rem' }}>
        <div style={{ color: 'var(--mn-color-ink-soft)', fontSize: '0.8rem', letterSpacing: '0.16em', textTransform: 'uppercase' }}>
          Attention
        </div>
        <div style={{ fontFamily: 'var(--mn-font-display)', fontSize: '2rem', lineHeight: 1 }}>2 prêts en retard</div>
        <div style={{ color: 'var(--mn-color-ink-soft)' }}>Ski Rossignol Experience 76, Scie sauteuse Makita</div>
      </div>
    ),
    tone: 'default',
  },
} satisfies Meta<typeof Card>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const Attention: Story = {
  args: {
    tone: 'attention',
  },
};