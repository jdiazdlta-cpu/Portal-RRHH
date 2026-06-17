import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { apiGet, apiPost, clearToken, setToken } from '../api/client';
import type { AuthResult, CurrentUser, Role } from '../types/api';

type AuthContextValue = {
  user: CurrentUser | null;
  loading: boolean;
  login: (nombreUsuario: string, password: string) => Promise<void>;
  logout: () => void;
  hasRole: (roles: Role[]) => boolean;
};

const userKey = 'portal_rrhh_fz_user';
const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(() => {
    const stored = localStorage.getItem(userKey);
    return stored ? (JSON.parse(stored) as CurrentUser) : null;
  });
  const [loading, setLoading] = useState(true);

  const logout = useCallback(() => {
    clearToken();
    localStorage.removeItem(userKey);
    setUser(null);
  }, []);

  useEffect(() => {
    let mounted = true;
    apiGet<CurrentUser>('/auth/me')
      .then((me) => {
        if (!mounted) return;
        setUser(me);
        localStorage.setItem(userKey, JSON.stringify(me));
      })
      .catch(() => {
        if (!mounted) return;
        logout();
      })
      .finally(() => {
        if (mounted) setLoading(false);
      });

    const onExpired = () => logout();
    window.addEventListener('auth:expired', onExpired);
    return () => {
      mounted = false;
      window.removeEventListener('auth:expired', onExpired);
    };
  }, [logout]);

  const login = useCallback(async (nombreUsuario: string, password: string) => {
    const result = await apiPost<AuthResult>('/auth/login', { nombreUsuario, password });
    setToken(result.token);
    localStorage.setItem(userKey, JSON.stringify(result.usuario));
    setUser(result.usuario);
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    user,
    loading,
    login,
    logout,
    hasRole: (roles: Role[]) => Boolean(user && roles.includes(user.rol))
  }), [loading, login, logout, user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth debe usarse dentro de AuthProvider.');
  }

  return context;
}
