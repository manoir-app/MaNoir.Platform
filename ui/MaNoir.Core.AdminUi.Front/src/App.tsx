import './app.css';

export function App() {
  return (
    <main className="front-shell">
      <div className="front-aurora front-aurora-left" aria-hidden="true" />
      <div className="front-aurora front-aurora-right" aria-hidden="true" />

      <section className="front-card">
        <span className="front-badge">MA NOIR</span>
        <h1>The real front is being wired here.</h1>
        <p>
          This placeholder is served once the local Core instance is already initialized.
          The authenticated admin surface will replace it as the dedicated front modules arrive.
        </p>

        <div className="front-signals" aria-label="Current front status">
          <article>
            <strong>Shell</strong>
            <span>Ready</span>
          </article>
          <article>
            <strong>Bootstrap</strong>
            <span>Completed</span>
          </article>
          <article>
            <strong>Front</strong>
            <span>Waiting for features</span>
          </article>
        </div>
      </section>
    </main>
  );
}