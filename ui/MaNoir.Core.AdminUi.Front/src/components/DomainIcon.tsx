interface DomainIconProps {
  kind?: string | null;
}

// One icon per application domain declared by the Core API (ApplicationDomains), plus "generic" for any other.
const domainIconPaths: Record<string, string> = {
  today: 'M10 2.5c.5 2.1 1.5 3.1 3.6 3.6-2.1.5-3.1 1.5-3.6 3.6-.5-2.1-1.5-3.1-3.6-3.6 2.1-.5 3.1-1.5 3.6-3.6Zm5.2 8.7c.3 1.3.9 1.9 2.2 2.2-1.3.3-1.9.9-2.2 2.2-.3-1.3-.9-1.9-2.2-2.2 1.3-.3 1.9-.9 2.2-2.2ZM6.4 11c.6 2.3 1.7 3.4 4 4-2.3.6-3.4 1.7-4 4-.6-2.3-1.7-3.4-4-4 2.3-.6 3.4-1.7 4-4Z',
  kitchen: 'M4 2.5h1.2V7h1V2.5h1.2V7h1V2.5h1.2v5.2c0 1.1-.7 2-1.7 2.3V17a1 1 0 0 1-2 0v-7C4.7 9.7 4 8.8 4 7.7V2.5Zm11.5 0V17a1 1 0 0 1-2 0v-4.5H12V7c0-2.5 1.5-4.5 3.5-4.5Z',
  home: 'M3 9.2 10 3l7 6.2V17a1 1 0 0 1-1 1h-3.4v-4.2H7.4V18H4a1 1 0 0 1-1-1V9.2Z',
  projects: 'M8 3.5a2 2 0 1 1 4 0V5h3a1 1 0 0 1 1 1v3h-1.5a2 2 0 1 0 0 4H16v3a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1h3V3.5Z',
  settings: 'M3 4.5h7.2a2 2 0 0 1 3.6 0H17V6h-3.2a2 2 0 0 1-3.6 0H3V4.5Zm0 9.5h3.2a2 2 0 0 1 3.6 0H17v1.5H9.8a2 2 0 0 1-3.6 0H3V14Zm0-4.75h14v1.5H3v-1.5Z',
  generic: 'M4 4h12v5H4V4Zm0 7h5v5H4v-5Zm7 0h5v5h-5v-5Z',
};

export function DomainIcon({ kind }: DomainIconProps) {
  const path = domainIconPaths[kind ?? ''] ?? domainIconPaths.generic;

  return (
    <svg aria-hidden="true" className="front-domain-icon" viewBox="0 0 20 20">
      <path d={path} fill="currentColor" />
    </svg>
  );
}
