import React, { createContext, useState, useEffect, useCallback } from 'react';
import { Center, Loader } from '@mantine/core';
import { setAuthStore } from '@/shared/api/client';
import { refreshTokenApi, decodeJwt } from './authApi';

export interface AuthSession {
  accessToken: string;
  refreshToken: string;
  userId: string;
  username: string;
  role: 'Admin' | 'Staff';
  tenantSlug: string;
  tenantId: string;
}

interface AuthContextValue {
  session: AuthSession | null;
  login: (session: AuthSession) => void;
  logout: () => void;
  refreshSession: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

const LS_REFRESH_TOKEN = 'terminar_refresh_token';
const LS_USER_ID = 'terminar_user_id';
const LS_TENANT_SLUG = 'terminar_tenant_slug';

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(null);
  const [loading, setLoading] = useState(true);

  const logout = useCallback(() => {
    setSession(null);
    localStorage.removeItem(LS_REFRESH_TOKEN);
    localStorage.removeItem(LS_USER_ID);
    localStorage.removeItem(LS_TENANT_SLUG);
  }, []);

  const login = useCallback((newSession: AuthSession) => {
    setSession(newSession);
    localStorage.setItem(LS_REFRESH_TOKEN, newSession.refreshToken);
    localStorage.setItem(LS_USER_ID, newSession.userId);
    localStorage.setItem(LS_TENANT_SLUG, newSession.tenantSlug);
  }, []);

  const refreshSession = useCallback(async () => {
    const storedRefreshToken = localStorage.getItem(LS_REFRESH_TOKEN);
    const storedUserId = localStorage.getItem(LS_USER_ID);

    if (!storedRefreshToken || !storedUserId) {
      logout();
      throw new Error('No refresh token available');
    }

    try {
      const result = await refreshTokenApi(storedUserId, storedRefreshToken);
      setSession(prev => {
        if (!prev) return null;
        return {
          ...prev,
          accessToken: result.accessToken,
          refreshToken: result.refreshToken,
        };
      });
      localStorage.setItem(LS_REFRESH_TOKEN, result.refreshToken);
    } catch {
      logout();
      throw new Error('Session refresh failed');
    }
  }, [logout]);

  useEffect(() => {
    const storedRefreshToken = localStorage.getItem(LS_REFRESH_TOKEN);
    const storedUserId = localStorage.getItem(LS_USER_ID);
    const storedTenantSlug = localStorage.getItem(LS_TENANT_SLUG);

    if (!storedRefreshToken || !storedUserId || !storedTenantSlug) {
      setLoading(false);
      return;
    }

    refreshTokenApi(storedUserId, storedRefreshToken)
      .then(result => {
        const claims = decodeJwt(result.accessToken);
        setSession({
          accessToken: result.accessToken,
          refreshToken: result.refreshToken,
          userId: claims.sub,
          username: claims.username,
          role: claims.role as 'Admin' | 'Staff',
          tenantSlug: storedTenantSlug,
          tenantId: claims.tenantId,
        });
        localStorage.setItem(LS_REFRESH_TOKEN, result.refreshToken);
      })
      .catch(() => {
        logout();
      })
      .finally(() => {
        setLoading(false);
      });
  }, [logout]);

  useEffect(() => {
    setAuthStore({
      getSession: () =>
        session ? { accessToken: session.accessToken, tenantSlug: session.tenantSlug } : null,
      refreshSession,
    });
  }, [session, refreshSession]);

  if (loading) {
    return (
      <Center h="100vh">
        <Loader size="xl" />
      </Center>
    );
  }

  return (
    <AuthContext.Provider value={{ session, login, logout, refreshSession }}>
      {children}
    </AuthContext.Provider>
  );
}
