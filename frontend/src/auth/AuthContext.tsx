import {
  createContext,
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { getCurrentUserRequest, loginRequest } from '../api/authApi';
import {
  clearStoredSession,
  getStoredToken,
  getStoredUser,
  storeSession,
  storeUser,
} from './session';
import type { AuthUser } from '../types/auth';

type AuthContextValue = {
  token: string | null;
  user: AuthUser | null;
  role: string | null;
  rol: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
};

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);

type AuthProviderProps = {
  children: ReactNode;
};

export function AuthProvider({ children }: AuthProviderProps) {
  const [token, setToken] = useState<string | null>(() => getStoredToken());
  const [user, setUser] = useState<AuthUser | null>(() => getStoredUser());
  const [isLoading, setIsLoading] = useState(true);

  const logout = useCallback(() => {
    clearStoredSession();
    setToken(null);
    setUser(null);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const response = await loginRequest({ email, password });

    if (!response.success || !response.data) {
      throw new Error(response.message || 'No fue posible iniciar sesion.');
    }

    storeSession(response.data.token, response.data.user);
    setToken(response.data.token);
    setUser(response.data.user);
  }, []);

  useEffect(() => {
    const validateSession = async () => {
      const storedToken = getStoredToken();

      if (!storedToken) {
        setIsLoading(false);
        return;
      }

      try {
        const response = await getCurrentUserRequest();

        if (response.success && response.data) {
          storeUser(response.data);
          setToken(storedToken);
          setUser(response.data);
        } else {
          logout();
        }
      } catch {
        logout();
      } finally {
        setIsLoading(false);
      }
    };

    void validateSession();
  }, [logout]);

  useEffect(() => {
    const handleUnauthorized = () => {
      logout();
    };

    window.addEventListener('portalrrhh:unauthorized', handleUnauthorized);

    return () => {
      window.removeEventListener('portalrrhh:unauthorized', handleUnauthorized);
    };
  }, [logout]);

  const value = useMemo<AuthContextValue>(
    () => ({
      token,
      user,
      role: user?.rol ?? null,
      rol: user?.rol ?? null,
      isAuthenticated: Boolean(token && user),
      isLoading,
      login,
      logout,
    }),
    [isLoading, login, logout, token, user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
