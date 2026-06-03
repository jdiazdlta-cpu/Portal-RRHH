import { Navigate, Route, Routes } from 'react-router-dom';
import { MainLayout } from '../layouts/MainLayout';
import { AccesoDenegadoPage } from '../pages/AccesoDenegado/AccesoDenegadoPage';
import { AlertasPage } from '../pages/Alertas/AlertasPage';
import { ColaboradoresPage } from '../pages/Colaboradores/ColaboradoresPage';
import { ConfiguracionPage } from '../pages/Configuracion/ConfiguracionPage';
import { DashboardPage } from '../pages/Dashboard/DashboardPage';
import { LoginPage } from '../pages/Login/LoginPage';
import { NotFoundPage } from '../pages/NotFound/NotFoundPage';
import { UsuariosPage } from '../pages/Usuarios/UsuariosPage';
import { ProtectedRoute } from './ProtectedRoute';

const adminOrRrhh = ['Admin', 'RRHH'];

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<MainLayout />}>
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="/acceso-denegado" element={<AccesoDenegadoPage />} />

          <Route element={<ProtectedRoute allowedRoles={adminOrRrhh} />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/colaboradores" element={<ColaboradoresPage />} />
            <Route path="/alertas" element={<AlertasPage />} />
            <Route path="/configuracion" element={<ConfiguracionPage />} />
          </Route>

          <Route element={<ProtectedRoute allowedRoles={['Admin']} />}>
            <Route path="/usuarios" element={<UsuariosPage />} />
          </Route>

          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Route>
    </Routes>
  );
}
