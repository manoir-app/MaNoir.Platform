import type { Meta, StoryObj } from '@storybook/react-vite';
import { Button } from './Button';
import { SectionTitle } from './SectionTitle';

const meta = {
  title: 'Patterns/SectionTitle',
  component: SectionTitle,
  args: {
    eyebrow: 'À consommer bientôt',
    heading: 'Le garde-manger presse.',
    description: 'Classé par date de péremption. Un clic sort le produit du stock.',
    actions: <Button variant="secondary">Tout voir</Button>,
  },
} satisfies Meta<typeof SectionTitle>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {};