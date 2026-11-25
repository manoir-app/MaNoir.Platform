import type * as React from 'react';

const stroke = {
  fill: 'none',
  stroke: 'currentColor',
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
  strokeWidth: 1.6,
};

const paths: Record<string, React.ReactNode> = {
  grid: <><rect height="5.5" rx="1.6" width="5.5" x="3" y="3" /><rect height="5.5" rx="1.6" width="5.5" x="11.5" y="3" /><rect height="5.5" rx="1.6" width="5.5" x="3" y="11.5" /><rect height="5.5" rx="1.6" width="5.5" x="11.5" y="11.5" /></>,
  pulse: <path d="M2 10h3l2.5-6.5 4 13L14 10h4" />,
  plug: <path d="M7 2.5v4M13 2.5v4M5 6.5h10v3a5 5 0 0 1-10 0v-3ZM10 14.5v3" />,
  monitor: <><rect height="10" rx="1.8" width="14" x="3" y="3.5" /><path d="M7.5 16.5h5M10 13.5v3" /></>,
  sliders: <path d="M3 6h8M15 6h2M3 14h2M9 14h8M11 4v4M7 12v4" />,
  gauge: <><path d="M3 14a7 7 0 1 1 14 0" /><path d="m10 14 3-4" /></>,
  'map-pin': <><path d="M10 17s5-4.4 5-8.2A5 5 0 0 0 5 8.8C5 12.6 10 17 10 17Z" /><circle cx="10" cy="8.8" r="1.8" /></>,
  users: <><circle cx="7.5" cy="7" r="2.6" /><path d="M2.5 16c.4-2.8 2.4-4.2 5-4.2s4.6 1.4 5 4.2" /><path d="M13 4.6a2.4 2.4 0 0 1 0 4.8M14.5 12c1.7.3 2.7 1.6 3 4" /></>,
  server: <><rect height="5" rx="1.6" width="14" x="3" y="3.5" /><rect height="5" rx="1.6" width="14" x="3" y="11.5" /><path d="M6 6h.01M6 14h.01" /></>,
  'file-text': <><path d="M5 2.5h6l4 4V16a1.5 1.5 0 0 1-1.5 1.5h-8A1.5 1.5 0 0 1 4 16V4A1.5 1.5 0 0 1 5 2.5Z" /><path d="M11 2.5v4h4M7 10h6M7 13h6" /></>,
  puzzle: <path d="M8 3.5h3v2a1.5 1.5 0 1 0 3 0v-2h2.5v4h-2a1.5 1.5 0 1 0 0 3h2v6H11v-2a1.5 1.5 0 1 0-3 0v2H3.5v-4h2a1.5 1.5 0 1 0 0-3h-2v-6H8Z" />,
  package: <><path d="m10 2.5 6.5 3.5v8L10 17.5 3.5 14V6L10 2.5Z" /><path d="m3.5 6 6.5 3.5L16.5 6M10 9.5v8" /></>,
};

const aliases: Record<string, string> = {
  status: 'gauge',
  places: 'map-pin',
  agents: 'users',
  services: 'server',
  logs: 'file-text',
  catalog: 'puzzle',
  installed: 'package',
};

function resolve(kind?: string | null) {
  const key = kind ? kind.trim().toLowerCase() : '';
  return paths[key] ?? paths[aliases[key] ?? ''];
}

export function hasPageIcon(kind?: string | null) {
  return Boolean(resolve(kind));
}

interface PageIconProps {
  kind?: string | null;
}

/** Line icon for a navigation page. Unknown kinds return null so the shell shows the initial instead. */
export function PageIcon({ kind }: PageIconProps) {
  const glyph = resolve(kind);

  if (!glyph) {
    return null;
  }

  return (
    <svg aria-hidden="true" height="20" viewBox="0 0 20 20" width="20" {...stroke}>
      {glyph}
    </svg>
  );
}
