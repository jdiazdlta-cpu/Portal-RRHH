import { Link } from 'react-router-dom';

export function NotFound() {
  return (
    <main className="center-page">
      <section className="panel narrow">
        <h1>No encontrado</h1>
        <Link className="primary-button" to="/dashboard">Volver</Link>
      </section>
    </main>
  );
}
