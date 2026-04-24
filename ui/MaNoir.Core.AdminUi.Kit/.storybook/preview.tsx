import type { Preview } from '@storybook/react-vite';
import '../src/styles/tokens.css';
import '../src/styles/base.css';

const preview: Preview = {
  decorators: [
    (Story) => (
      <div
        className="mn-kit-theme"
        style={{
          background: 'var(--mn-color-paper)',
          minHeight: '100vh',
          padding: '2rem',
        }}
      >
        <Story />
      </div>
    ),
  ],
  parameters: {
    backgrounds: {
      default: 'paper',
      values: [
        { name: 'paper', value: '#f5f0e8' },
        { name: 'white', value: '#ffffff' },
        { name: 'ink', value: '#1f1916' },
      ],
    },
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
    layout: 'padded',
  },
};

export default preview;