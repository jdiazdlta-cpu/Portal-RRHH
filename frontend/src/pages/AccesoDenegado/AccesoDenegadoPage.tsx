import { Link } from 'react-router-dom';

export function AccesoDenegadoPage() {
  return (
    <section className="placeholder-page">
      <span className="eyebrow">403</span>
      <h2>Acceso denegado</h2>
      <p>Tu rol no tiene permisos para esta opcion en V1.</p>
      <Link className="primary-link" to="/dashboard">
        Volver al dashboard
      </Link>
    </section>
  );
}
