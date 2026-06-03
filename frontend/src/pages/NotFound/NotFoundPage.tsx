import { Link } from 'react-router-dom';

export function NotFoundPage() {
  return (
    <section className="placeholder-page">
      <span className="eyebrow">404</span>
      <h2>Pagina no encontrada</h2>
      <p>La ruta solicitada no existe en Portal RRHH FZ.</p>
      <Link className="primary-link" to="/dashboard">
        Ir al dashboard
      </Link>
    </section>
  );
}
