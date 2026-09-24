import React, { createContext, useContext, useState, useEffect, useCallback } from 'react'
import { supabase, isSupabaseConfigured } from '../services/supabaseClient'
import { api } from '../services/api'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [session, setSession] = useState(null)
  const [user, setUser] = useState(null)
  const [profile, setProfile] = useState(null)
  const [isLoading, setIsLoading] = useState(true)
  const [blockedMessage, setBlockedMessage] = useState(null)
  const [theme, setTheme] = useState(() => localStorage.getItem('recruitov_theme') || 'light')

  // Apply theme to document
  useEffect(() => {
    document.documentElement.setAttribute('data-bs-theme', theme)
    localStorage.setItem('recruitov_theme', theme)
  }, [theme])

  // Listen for AccountBlocked global events from api.js
  useEffect(() => {
    const handleBlocked = (event) => {
      setBlockedMessage(event.detail || 'Your account has been blocked by an administrator.')
    }
    window.addEventListener('account-blocked', handleBlocked)
    return () => window.removeEventListener('account-blocked', handleBlocked)
  }, [])

  // Synchronize user profile with backend API (/api/auth/me)
  const fetchProfile = useCallback(async () => {
    try {
      const data = await api.get('/api/auth/me')
      setProfile(data)
      if (data.isBlocked) {
        setBlockedMessage('Your account has been blocked by an administrator.')
      } else {
        setBlockedMessage(null)
      }
      if (data.preferredTheme && (data.preferredTheme === 'dark' || data.preferredTheme === 'light')) {
        setTheme(data.preferredTheme)
      }
      return data
    } catch (err) {
      console.warn('Could not fetch user profile from backend:', err.message)
      return null
    }
  }, [])

  // Listen to Supabase Auth state changes
  useEffect(() => {
    if (!isSupabaseConfigured) {
      setIsLoading(false)
      return
    }

    supabase.auth.getSession().then(({ data: { session } }) => {
      setSession(session)
      setUser(session?.user ?? null)
      if (session) {
        fetchProfile().finally(() => setIsLoading(false))
      } else {
        setProfile(null)
        setIsLoading(false)
      }
    })

    const { data: { subscription } } = supabase.auth.onAuthStateChange(async (event, newSession) => {
      setSession(newSession)
      setUser(newSession?.user ?? null)

      if (event === 'SIGNED_IN' || event === 'TOKEN_REFRESHED') {
        if (newSession) {
          await fetchProfile()
        }
      } else if (event === 'SIGNED_OUT') {
        setProfile(null)
        setBlockedMessage(null)
      }
      setIsLoading(false)
    })

    return () => subscription.unsubscribe()
  }, [fetchProfile])

  // Sign in with Email and Password
  const loginWithEmail = async (email, password) => {
    const { data, error } = await supabase.auth.signInWithPassword({ email, password })
    if (error) throw error
    return data
  }

  // Register with Email and Password
  const registerWithEmail = async (email, password) => {
    const { data, error } = await supabase.auth.signUp({
      email,
      password,
      options: {
        emailRedirectTo: window.location.origin
      }
    })
    if (error) throw error
    return data
  }

  // Social Login with OAuth (Google, GitHub, etc.)
  const loginWithOAuth = async (provider) => {
    const { data, error } = await supabase.auth.signInWithOAuth({
      provider,
      options: {
        redirectTo: window.location.origin
      }
    })
    if (error) throw error
    return data
  }

  // Sign out
  const logout = async () => {
    await supabase.auth.signOut()
    setSession(null)
    setUser(null)
    setProfile(null)
    setBlockedMessage(null)
  }

  // Toggle theme and persist to backend if logged in
  const toggleTheme = async () => {
    const nextTheme = theme === 'light' ? 'dark' : 'light'
    setTheme(nextTheme)
    if (user && profile) {
      try {
        await api.put('/api/auth/preferences', {
          preferredTheme: nextTheme,
          preferredLanguage: profile.preferredLanguage || 'en'
        })
      } catch (err) {
        console.warn('Failed to persist theme preference:', err)
      }
    }
  }

  // Update preferences
  const updatePreferences = async ({ preferredLanguage, preferredTheme }) => {
    if (preferredTheme) setTheme(preferredTheme)
    if (user) {
      await api.put('/api/auth/preferences', {
        preferredLanguage,
        preferredTheme: preferredTheme || theme
      })
      await fetchProfile()
    }
  }

  const roles = profile?.roles || []
  const isCandidate = roles.includes('Candidate')
  const isRecruiter = roles.includes('Recruiter')
  const isAdmin = roles.includes('Administrator')
  const isBlocked = Boolean(profile?.isBlocked || blockedMessage)

  const value = {
    session,
    user,
    profile,
    roles,
    isCandidate,
    isRecruiter,
    isAdmin,
    isBlocked,
    blockedMessage,
    isAuthenticated: Boolean(session && user),
    isLoading,
    isSupabaseConfigured,
    theme,
    toggleTheme,
    updatePreferences,
    loginWithEmail,
    registerWithEmail,
    loginWithOAuth,
    logout,
    refreshProfile: fetchProfile
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
