import { Navigate, Route, Routes } from 'react-router-dom';
import { AppLayout } from './layouts/AppLayout';
import { ProtectedRoute } from './routes/ProtectedRoute';
import { useAuth } from './auth/AuthContext';
import { AccesoDenegado } from './pages/AccesoDenegado';
import { AlertasPage } from './pages/AlertasPage';
import { ColaboradoresPage } from './pages/ColaboradoresPage';
import { ConfiguracionPage } from './pages/ConfiguracionPage';
import { DashboardPage } from './pages/DashboardPage';
import { LoginPage } from './pages/LoginPage';
import { NotFound } from './pages/NotFound';
import { OrganigramaPage } from './pages/OrganigramaPage';
import { PerfilColaboradorPage } from './pages/PerfilColaboradorPage';
import { SolicitudesPage } from './pages/SolicitudesPage';
import { UsuariosPage } from './pages/UsuariosPage';

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        index
        element={(
          <ProtectedRoute roles={['Admin', 'RRHH', 'Supervisor']}>
            <HomeRedirect />
          </ProtectedRoute>
        )}
      />
      <Route
        element={(
          <ProtectedRoute roles={['Admin', 'RRHH']}>
            <AppLayout />
          </ProtectedRoute>
        )}
      >
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/colaboradores" element={<ColaboradoresPage />} />
        <Route path="/colaboradores/:id" element={<PerfilColaboradorPage />} />
        <Route path="/alertas" element={<AlertasPage />} />
        <Route path="/organigrama" element={<OrganigramaPage />} />
      </Route>
      <Route
        element={(
          <ProtectedRoute roles={['Admin', 'RRHH', 'Supervisor']}>
            <AppLayout />
          </ProtectedRoute>
        )}
      >
        <Route path="/solicitudes" element={<SolicitudesPage />} />
      </Route>
      <Route
        element={(
          <ProtectedRoute roles={['Admin']}>
            <AppLayout />
          </ProtectedRoute>
        )}
      >
        <Route path="/usuarios" element={<UsuariosPage />} />
        <Route path="/configuracion" element={<ConfiguracionPage />} />
      </Route>
      <Route path="/acceso-denegado" element={<AccesoDenegado />} />
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}

function HomeRedirect() {
  const { user } = useAuth();
  return <Navigate to={user?.rol === 'Supervisor' ? '/solicitudes' : '/dashboard'} replace />;
}
