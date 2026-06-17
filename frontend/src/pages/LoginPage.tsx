import { FormEvent, useEffect, useState } from 'react';
import { Lock, ShieldCheck } from 'lucide-react';
import { Navigate, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function LoginPage() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const [nombreUsuario, setNombreUsuario] = useState('admin');
  const [password, setPassword] = useState('Admin123*');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    document.title = 'Portal RRHH FZ';
  }, []);

  if (user) {
    return <Navigate to="/dashboard" replace />;
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setError('');
    try {
      await login(nombreUsuario, password);
      navigate('/dashboard', { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo iniciar sesion.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="login-page">
      <section className="login-panel">
        <div className="login-mark">
          <ShieldCheck size={32} aria-hidden />
        </div>
        <h1>Portal RRHH FZ</h1>
        <form onSubmit={submit} className="form-stack">
          <label>
            Usuario
            <input value={nombreUsuario} onChange={(event) => setNombreUsuario(event.target.value)} autoComplete="username" />
          </label>
          <label>
            Contrasena
            <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} autoComplete="current-password" />
          </label>
          {error && <div className="error-box">{error}</div>}
          <button className="primary-button" disabled={submitting}>
            <Lock size={18} aria-hidden />
            {submitting ? 'Validando' : 'Ingresar'}
          </button>
        </form>
      </section>
    </main>
  );
}
