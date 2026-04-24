import type { Meta, StoryObj } from '@storybook/react-vite';
import { EditorialBand } from './EditorialBand';

const meta = {
  title: 'Patterns/EditorialBand',
  component: EditorialBand,
  args: {
    children: (
      <>
        <div style={{ fontFamily: 'var(--mn-font-display)', fontSize: '1.1rem', lineHeight: 1.05 }}>2 prêts en retard de retour.</div>
        <div style={{ color: 'var(--mn-color-ink-soft)', fontSize: '0.9rem' }}>Ski Rossignol Experience 76, Scie sauteuse Makita</div>
      </>
    ),
  },
} satisfies Meta<typeof EditorialBand>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Accent: Story = {};

export const Danger: Story = {
  args: {
    tone: 'danger',
  },
};