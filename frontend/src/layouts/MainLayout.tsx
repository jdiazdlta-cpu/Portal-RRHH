import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/useAuth';

type NavigationItem = {
  to: string;
  label: string;
  marker: string;
  roles: string[];
};

const navigationItems: NavigationItem[] = [
  { to: '/dashboard', label: 'Dashboard', marker: 'D', roles: ['Admin', 'RRHH'] },
  { to: '/colaboradores', label: 'Colaboradores', marker: 'C', roles: ['Admin', 'RRHH'] },
  { to: '/alertas', label: 'Alertas', marker: 'A', roles: ['Admin', 'RRHH'] },
  { to: '/usuarios', label: 'Usuarios', marker: 'U', roles: ['Admin'] },
  { to: '/configuracion', label: 'Configuracion', marker: 'K', roles: ['Admin', 'RRHH'] },
];

export function MainLayout() {
  const { logout, role, user } = useAuth();
  const navigate = useNavigate();
  const visibleItems = navigationItems.filter((item) => role && item.roles.includes(role));

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-mark">FZ</div>
          <div>
            <strong>Portal RRHH FZ</strong>
            <span>Recursos Humanos</span>
          </div>
        </div>

        <nav className="nav-list" aria-label="Navegacion principal">
          {visibleItems.map((item) => (
            <NavLink
              className={({ isActive }) => (isActive ? 'nav-item active' : 'nav-item')}
              key={item.to}
              to={item.to}
            >
              <span className="nav-marker">{item.marker}</span>
              <span>{item.label}</span>
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="content-shell">
        <header className="topbar">
          <div>
            <span className="eyebrow">Sesion activa</span>
            <h1>{user?.nombreUsuario ?? 'Usuario'}</h1>
          </div>
          <div className="user-actions">
            <span className="role-pill">{role ?? 'Sin rol'}</span>
            <button className="secondary-button" type="button" onClick={handleLogout}>
              Cerrar sesion
            </button>
          </div>
        </header>

        <main className="page-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
