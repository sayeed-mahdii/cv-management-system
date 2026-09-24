import React, { useState } from 'react'
import { Modal, Form, Alert } from 'react-bootstrap'
import { useAuth } from '../context/AuthContext'

export function AuthModal({ show, onHide, initialMode = 'login' }) {
  const [mode, setMode] = useState(initialMode) // 'login' or 'register'
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [successMessage, setSuccessMessage] = useState(null)

  const { loginWithEmail, registerWithEmail, loginWithOAuth, isSupabaseConfigured } = useAuth()

  const resetForm = () => {
    setEmail('')
    setPassword('')
    setConfirmPassword('')
    setError(null)
    setSuccessMessage(null)
  }

  const handleClose = () => {
    resetForm()
    onHide()
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)
    setSuccessMessage(null)

    if (mode === 'register' && password !== confirmPassword) {
      setError('Passwords do not match.')
      return
    }

    if (password.length < 6) {
      setError('Password must be at least 6 characters long.')
      return
    }

    setLoading(true)
    try {
      if (mode === 'login') {
        await loginWithEmail(email, password)
        handleClose()
      } else {
        const data = await registerWithEmail(email, password)
        if (data?.user && !data?.session) {
          setSuccessMessage('Registration successful! Please check your email inbox to confirm your account.')
        } else {
          setSuccessMessage('Account created and signed in successfully!')
          setTimeout(() => handleClose(), 1200)
        }
      }
    } catch (err) {
      setError(err.message || 'Authentication failed. Please check your credentials.')
    } finally {
      setLoading(false)
    }
  }

  const handleOAuth = async (provider) => {
    setError(null)
    setLoading(true)
    try {
      await loginWithOAuth(provider)
    } catch (err) {
      setError(err.message || `Failed to sign in with ${provider}.`)
      setLoading(false)
    }
  }

  return (
    <Modal show={show} onHide={handleClose} centered backdrop="static" keyboard={!loading}>
      <div className="p-4 bg-body rounded-4 shadow">
        <div className="d-flex justify-content-between align-items-center mb-3">
          <div>
            <h4 className="fw-bold mb-1">
              {mode === 'login' ? 'Sign In to reCruitoV' : 'Create an Account'}
            </h4>
            <p className="text-muted small mb-0">
              {mode === 'login'
                ? 'Welcome back! Choose your preferred login method.'
                : 'Join the platform to build CVs, post positions, or review candidates.'}
            </p>
          </div>
          <button
            type="button"
            className="btn-close"
            aria-label="Close"
            onClick={handleClose}
            disabled={loading}
          />
        </div>

        {!isSupabaseConfigured && (
          <Alert variant="warning" className="small py-2">
            <i className="bi bi-exclamation-triangle-fill me-2"></i>
            Supabase client key not configured in <code>.env.local</code>. Please set <code>VITE_SUPABASE_ANON_KEY</code>.
          </Alert>
        )}

        {error && (
          <Alert variant="danger" className="small py-2">
            <i className="bi bi-x-circle-fill me-2"></i>
            {error}
          </Alert>
        )}

        {successMessage && (
          <Alert variant="success" className="small py-2">
            <i className="bi bi-check-circle-fill me-2"></i>
            {successMessage}
          </Alert>
        )}

        {/* Social Logins (Spec §3.1, §4.1) */}
        <div className="d-grid gap-2 mb-3">
          <button
            type="button"
            className="btn btn-outline-secondary d-flex align-items-center justify-content-center py-2"
            onClick={() => handleOAuth('google')}
            disabled={loading || !isSupabaseConfigured}
          >
            <i className="bi bi-google me-2 text-danger"></i>
            Continue with Google
          </button>
          <button
            type="button"
            className="btn btn-outline-secondary d-flex align-items-center justify-content-center py-2"
            onClick={() => handleOAuth('github')}
            disabled={loading || !isSupabaseConfigured}
          >
            <i className="bi bi-github me-2"></i>
            Continue with GitHub
          </button>
        </div>

        <div className="position-relative text-center my-3">
          <hr className="my-2" />
          <span className="position-absolute top-50 start-50 translate-middle bg-body px-3 text-muted small">
            or with email & password
          </span>
        </div>

        {/* Email & Password Form */}
        <Form onSubmit={handleSubmit}>
          <Form.Group className="mb-3">
            <Form.Label className="small fw-semibold">Email address</Form.Label>
            <Form.Control
              type="email"
              placeholder="name@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              disabled={loading || !isSupabaseConfigured}
              className="form-control-pro"
            />
          </Form.Group>

          <Form.Group className="mb-3">
            <Form.Label className="small fw-semibold">Password</Form.Label>
            <Form.Control
              type="password"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              disabled={loading || !isSupabaseConfigured}
              className="form-control-pro"
            />
          </Form.Group>

          {mode === 'register' && (
            <Form.Group className="mb-3">
              <Form.Label className="small fw-semibold">Confirm Password</Form.Label>
              <Form.Control
                type="password"
                placeholder="••••••••"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                required
                disabled={loading || !isSupabaseConfigured}
                className="form-control-pro"
              />
            </Form.Group>
          )}

          <div className="d-grid mt-4">
            <button
              type="submit"
              className="btn btn-primary-gradient py-2"
              disabled={loading || !isSupabaseConfigured}
            >
              {loading ? (
                <>
                  <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true" />
                  Please wait...
                </>
              ) : mode === 'login' ? (
                'Sign In'
              ) : (
                'Create Account'
              )}
            </button>
          </div>
        </Form>

        <div className="text-center mt-3 pt-2 border-top">
          {mode === 'login' ? (
            <p className="text-muted small mb-0">
              Don't have an account?{' '}
              <button
                type="button"
                className="btn btn-link p-0 text-decoration-none small fw-semibold"
                onClick={() => {
                  setError(null)
                  setMode('register')
                }}
              >
                Sign up
              </button>
            </p>
          ) : (
            <p className="text-muted small mb-0">
              Already have an account?{' '}
              <button
                type="button"
                className="btn btn-link p-0 text-decoration-none small fw-semibold"
                onClick={() => {
                  setError(null)
                  setMode('login')
                }}
              >
                Sign in
              </button>
            </p>
          )}
        </div>
      </div>
    </Modal>
  )
}
