import { Navigate, Route, Routes } from 'react-router-dom';
import { AppLayout } from './layouts/AppLayout';
import { ProtectedRoute } from './routes/ProtectedRoute';
import { AccesoDenegado } from './pages/AccesoDenegado';
import { AlertasPage } from './pages/AlertasPage';
import { ColaboradoresPage } from './pages/ColaboradoresPage';
import { ConfiguracionPage } from './pages/ConfiguracionPage';
import { DashboardPage } from './pages/DashboardPage';
import { LoginPage } from './pages/LoginPage';
import { NotFound } from './pages/NotFound';
import { PerfilColaboradorPage } from './pages/PerfilColaboradorPage';
import { UsuariosPage } from './pages/UsuariosPage';

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        element={(
          <ProtectedRoute roles={['Admin', 'RRHH']}>
            <AppLayout />
          </ProtectedRoute>
        )}
      >
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/colaboradores" element={<ColaboradoresPage />} />
        <Route path="/colaboradores/:id" element={<PerfilColaboradorPage />} />
        <Route path="/alertas" element={<AlertasPage />} />
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
