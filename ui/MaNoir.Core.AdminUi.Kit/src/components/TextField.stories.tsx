import type { Meta, StoryObj } from '@storybook/react-vite';
import { Field } from './Field';
import { TextField } from './TextField';

const meta = {
  title: 'Forms/TextField',
  component: TextField,
  args: {
    placeholder: 'Rechercher un objet, un contact, un prêt...',
  },
  render: (args) => (
    <div style={{ maxWidth: 460 }}>
      <Field hint="Utilisez un terme simple pour démarrer la recherche." htmlFor="storybook-textfield" label="Recherche rapide">
        <TextField id="storybook-textfield" {...args} />
      </Field>
    </div>
  ),
} satisfies Meta<typeof TextField>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const Invalid: Story = {
  args: {
    invalid: true,
    defaultValue: 'Perceuse Festool',
  },
  render: (args) => (
    <div style={{ maxWidth: 460 }}>
      <Field error="Ce champ doit rester générique et ne pas embarquer de règle métier." htmlFor="storybook-textfield-invalid" label="Nom affiché" required>
        <TextField id="storybook-textfield-invalid" {...args} />
      </Field>
    </div>
  ),
};