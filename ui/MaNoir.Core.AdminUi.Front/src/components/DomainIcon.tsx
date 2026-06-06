interface DomainIconProps {
  kind?: string | null;
}

export function DomainIcon({ kind }: DomainIconProps) {
  if (kind === 'home-automation') {
    return (
      <svg aria-hidden="true" className="front-domain-icon" viewBox="0 0 20 20">
        <path d="M3 9.2 10 3l7 6.2V17a1 1 0 0 1-1 1h-3.4v-4.2H7.4V18H4a1 1 0 0 1-1-1V9.2Z" fill="currentColor" />
      </svg>
    );
  }

  if (kind === 'daily-life') {
    return (
      <svg aria-hidden="true" className="front-domain-icon" viewBox="0 0 20 20">
        <path d="M10 2.5c.5 2.1 1.5 3.1 3.6 3.6-2.1.5-3.1 1.5-3.6 3.6-.5-2.1-1.5-3.1-3.6-3.6 2.1-.5 3.1-1.5 3.6-3.6Zm5.2 8.7c.3 1.3.9 1.9 2.2 2.2-1.3.3-1.9.9-2.2 2.2-.3-1.3-.9-1.9-2.2-2.2 1.3-.3 1.9-.9 2.2-2.2ZM6.4 11c.6 2.3 1.7 3.4 4 4-2.3.6-3.4 1.7-4 4-.6-2.3-1.7-3.4-4-4 2.3-.6 3.4-1.7 4-4Z" fill="currentColor" />
      </svg>
    );
  }

  if (kind === 'generic') {
    return (
      <svg aria-hidden="true" className="front-domain-icon" viewBox="0 0 20 20">
        <path d="M4 4h12v5H4V4Zm0 7h5v5H4v-5Zm7 0h5v5h-5v-5Z" fill="currentColor" />
      </svg>
    );
  }

  return (
    <svg aria-hidden="true" className="front-domain-icon" viewBox="0 0 20 20">
      <path d="M3 4h6v5H3V4Zm8 0h6v5h-6V4ZM3 11h6v5H3v-5Zm8 0h6v5h-6v-5Z" fill="currentColor" />
    </svg>
  );
}