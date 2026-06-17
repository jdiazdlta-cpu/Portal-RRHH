import { Link } from 'react-router-dom';

export function AccesoDenegado() {
  return (
    <main className="center-page">
      <section className="panel narrow">
        <h1>Acceso denegado</h1>
        <Link className="primary-button" to="/dashboard">Volver</Link>
      </section>
    </main>
  );
}
