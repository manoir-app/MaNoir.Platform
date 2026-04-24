import type { Meta, StoryObj } from '@storybook/react-vite';
import { SearchInput } from './SearchInput';

const meta = {
  title: 'Forms/SearchInput',
  component: SearchInput,
  args: {
    id: 'search-input-story',
    label: 'Recherche globale',
    placeholder: 'Rechercher un objet, un contact, un prêt...',
    hint: 'Raccourci de recherche rapide pour le backoffice.',
  },
} satisfies Meta<typeof SearchInput>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: (args) => (
    <div style={{ maxWidth: 480 }}>
      <SearchInput {...args} />
    </div>
  ),
};

export const Invalid: Story = {
  args: {
    error: 'La recherche n’accepte pas encore ce filtre.',
    invalid: true,
    shortcut: 'Ctrl K',
  },
  render: (args) => (
    <div style={{ maxWidth: 480 }}>
      <SearchInput {...args} />
    </div>
  ),
};