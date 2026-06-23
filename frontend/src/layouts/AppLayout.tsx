import { Bell, FileText, LayoutDashboard, LogOut, Menu, Network, Settings, Users } from 'lucide-react';
import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function AppLayout() {
  const { user, logout } = useAuth();
  const commonItems = [
    { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
    { to: '/colaboradores', label: 'Colaboradores', icon: Users },
    { to: '/alertas', label: 'Alertas', icon: Bell },
    { to: '/organigrama', label: 'Organigrama', icon: Network },
    { to: '/solicitudes', label: 'Solicitudes', icon: FileText }
  ];
  const items = user?.rol === 'Admin'
    ? [
        ...commonItems,
        { to: '/usuarios', label: 'Usuarios', icon: Users },
        { to: '/configuracion', label: 'Configuracion', icon: Settings }
      ]
    : user?.rol === 'RRHH'
      ? commonItems
      : [{ to: '/solicitudes', label: 'Solicitudes', icon: FileText }];

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="brand">
          <Menu size={22} aria-hidden />
          <span>Portal RRHH FZ</span>
        </div>
        <nav className="topnav" aria-label="Principal">
          {items.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink key={item.to} to={item.to} className={({ isActive }) => (isActive ? 'active' : undefined)}>
                <Icon size={17} aria-hidden />
                <span>{item.label}</span>
              </NavLink>
            );
          })}
        </nav>
        <div className="session-pill">
          <span>{user?.nombreUsuario}</span>
          <strong>{user?.rol}</strong>
          <button className="icon-button" onClick={logout} title="Cerrar sesion" aria-label="Cerrar sesion">
            <LogOut size={18} />
          </button>
        </div>
      </header>
      <main className="content">
        <Outlet />
      </main>
    </div>
  );
}
