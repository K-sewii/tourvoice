import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import * as authApi from '../api/auth'
import { setToken, setUnauthorizedHandler } from '../api/http'
import type { AuthResponse } from '../api/types'

export type AuthUser = Omit<AuthResponse, 'token'>

interface AuthContextValue {
  user: AuthUser | null
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, fullName: string) => Promise<void>
  logout: () => void
}

const USER_KEY = 'tv.user'
const AuthContext = createContext<AuthContextValue | null>(null)

function readUser(): AuthUser | null {
  try {
    const raw = localStorage.getItem(USER_KEY)
    return raw ? (JSON.parse(raw) as AuthUser) : null
  } catch {
    return null
  }
}

function writeUser(user: AuthUser | null) {
  try {
    if (user) localStorage.setItem(USER_KEY, JSON.stringify(user))
    else localStorage.removeItem(USER_KEY)
  } catch {
    // ignore
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(readUser)

  const accept = useCallback(({ token, ...profile }: AuthResponse) => {
    setToken(token)
    writeUser(profile)
    setUser(profile)
  }, [])

  const logout = useCallback(() => {
    setToken(null)
    writeUser(null)
    setUser(null)
  }, [])

  // Expired/invalid token anywhere → sign out once, centrally; guarded pages then redirect to sign in.
  useEffect(() => {
    setUnauthorizedHandler(logout)
    return () => setUnauthorizedHandler(null)
  }, [logout])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      login: async (email, password) => accept(await authApi.login(email, password)),
      register: async (email, password, fullName) => accept(await authApi.register(email, password, fullName)),
      logout,
    }),
    [accept, logout, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>')
  return ctx
}
