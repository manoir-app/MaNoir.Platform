import type { Meta, StoryObj } from '@storybook/react-vite';
import { Field } from './Field';
import { SidebarNav } from './SidebarNav';
import { TextField } from './TextField';

const meta = {
  title: 'Patterns/SidebarNav',
  component: SidebarNav,
  args: {
    brand: 'Intendance',
    caption: 'maison · 48 réf.',
    searchSlot: (
      <Field htmlFor="sidebar-search" label="Recherche" style={{ gap: '0.35rem' }}>
        <TextField id="sidebar-search" placeholder="Rechercher" />
      </Field>
    ),
    sections: [
      {
        id: 'primary',
        items: [
          { id: 'dashboard', label: 'Tableau', active: true },
          { id: 'possessions', label: 'Possessions', meta: '20' },
          { id: 'pantry', label: 'Garde-manger', meta: '28' },
          { id: 'loans', label: 'Prêts', meta: '4' },
        ],
      },
      {
        id: 'admin',
        title: 'Admin',
        items: [
          { id: 'rooms', label: 'Pièces & rangements' },
          { id: 'contacts', label: 'Contacts' },
          { id: 'scanner', label: 'Scanner' },
        ],
      },
    ],
  },
} satisfies Meta<typeof SidebarNav>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: (args) => <div style={{ maxWidth: 320 }}><SidebarNav {...args} /></div>,
};