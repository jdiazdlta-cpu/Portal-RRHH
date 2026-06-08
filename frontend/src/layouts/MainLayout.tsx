import { useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/useAuth';

type NavigationItem = {
  to: string;
  label: string;
  roles: string[];
};

const navigationItems: NavigationItem[] = [
  { to: '/dashboard', label: 'Dashboard', roles: ['Admin', 'RRHH'] },
  { to: '/colaboradores', label: 'Colaboradores', roles: ['Admin', 'RRHH'] },
  { to: '/alertas', label: 'Alertas', roles: ['Admin', 'RRHH'] },
  { to: '/usuarios', label: 'Usuarios', roles: ['Admin'] },
  { to: '/configuracion', label: 'Configuracion', roles: ['Admin', 'RRHH'] },
];

export function MainLayout() {
  const { logout, role, user } = useAuth();
  const navigate = useNavigate();
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const visibleItems = navigationItems.filter((item) => role && item.roles.includes(role));

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  return (
    <div className="shell top-shell">
      <header className="top-navigation">
        <div className="top-navigation-inner">
          <NavLink className="top-brand" to="/dashboard" onClick={() => setIsMenuOpen(false)}>
            <span className="brand-mark">FZ</span>
            <span>
              <strong>Portal RRHH FZ</strong>
              <small>Recursos Humanos</small>
            </span>
          </NavLink>

          <button
            aria-expanded={isMenuOpen}
            className="menu-toggle"
            type="button"
            onClick={() => setIsMenuOpen((current) => !current)}
          >
            Menu
          </button>

          <nav
            aria-label="Navegacion principal"
            className={isMenuOpen ? 'top-nav-list open' : 'top-nav-list'}
          >
            {visibleItems.map((item) => (
              <NavLink
                className={({ isActive }) => (isActive ? 'top-nav-item active' : 'top-nav-item')}
                key={item.to}
                to={item.to}
                onClick={() => setIsMenuOpen(false)}
              >
                {item.label}
              </NavLink>
            ))}
          </nav>

          <div className="top-user">
            <div className="user-avatar">{user?.nombreUsuario?.slice(0, 2).toUpperCase() ?? 'US'}</div>
            <div className="top-user-meta">
              <strong>{user?.nombreUsuario ?? 'Usuario'}</strong>
              <span>{role ?? 'Sin rol'}</span>
            </div>
            <button className="logout-button" type="button" onClick={handleLogout}>
              Salir
            </button>
          </div>
        </div>
      </header>

      <main className="page-content">
        <Outlet />
      </main>
    </div>
  );
}
