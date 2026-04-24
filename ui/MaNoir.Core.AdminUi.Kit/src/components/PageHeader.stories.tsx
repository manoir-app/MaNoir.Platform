import type { Meta, StoryObj } from '@storybook/react-vite';
import { PageHeader } from './PageHeader';

const meta = {
  title: 'Patterns/PageHeader',
  component: PageHeader,
  args: {
    eyebrow: 'N° XLVII • Lundi 20 avril 2026',
    title: (
      <>
        Bonjour Paul.
        <br />
        Rien de grave aujourd&apos;hui, juste quelques attentions.
      </>
    ),
    description: '7 produits à consommer sous trois jours, 2 prêts en retard, et 10 articles attendent sur votre liste de courses.',
  },
} satisfies Meta<typeof PageHeader>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {};