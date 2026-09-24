import React, { useState, useEffect } from 'react'
import { Container, Row, Col, Card, Badge, Alert, Spinner } from 'react-bootstrap'
import { useAuth } from '../context/AuthContext'
import { api } from '../services/api'
import { AuthModal } from '../components/AuthModal'

export function HomePage() {
  const { user, profile, roles, isAuthenticated, isBlocked, blockedMessage } = useAuth()
  const [health, setHealth] = useState(null)
  const [healthLoading, setHealthLoading] = useState(true)
  const [authModalOpen, setAuthModalOpen] = useState(false)
  const [authMode, setAuthMode] = useState('login')

  const fetchHealth = async () => {
    setHealthLoading(true)
    try {
      const res = await fetch('/api/health')
      const data = await res.json()
      setHealth(data)
    } catch (err) {
      setHealth({ status: 'Unreachable', database: 'Disconnected', error: err.message })
    } finally {
      setHealthLoading(false)
    }
  }

  useEffect(() => {
    fetchHealth()
  }, [])

  return (
    <div className="py-5">
      <Container>
        {/* Blocked Account Alert */}
        {isBlocked && (
          <Alert variant="danger" className="mb-4 d-flex align-items-center gap-3 rounded-4 shadow-sm">
            <i className="bi bi-shield-slash-fill fs-2"></i>
            <div>
              <h5 className="fw-bold mb-1">Account Suspended</h5>
              <p className="mb-0 small">
                {blockedMessage || 'Your account has been blocked by an administrator. Modifying CVs, profiles, and positions is disabled.'}
              </p>
            </div>
          </Alert>
        )}

        {/* Hero Section */}
        <div className="text-center py-4 mb-4">
          <div className="d-inline-flex align-items-center gap-2 px-3 py-1 rounded-pill bg-primary-subtle text-primary fw-semibold small mb-3 border border-primary-subtle">
            <i className="bi bi-shield-check"></i> Phase 4 Active &mdash; Supabase Auth &amp; PostgreSQL RBAC
          </div>
          <h1 className="display-4 fw-extrabold mb-3 tracking-tight">
            Enterprise Candidate &amp; <span className="text-primary">CV Management</span>
          </h1>
          <p className="lead text-muted mx-auto" style={{ maxWidth: '720px' }}>
            A high-performance talent platform built with Entity-Attribute-Value (EAV) dynamic schemas,
            automated position access rules, and optimistic locking concurrency control.
          </p>

          {!isAuthenticated ? (
            <div className="d-flex justify-content-center gap-3 mt-4">
              <button
                type="button"
                className="btn btn-primary-gradient px-4 py-2 rounded-pill fw-semibold"
                onClick={() => {
                  setAuthMode('register')
                  setAuthModalOpen(true)
                }}
              >
                Create Candidate Account
              </button>
              <button
                type="button"
                className="btn btn-outline-secondary px-4 py-2 rounded-pill fw-semibold"
                onClick={() => {
                  setAuthMode('login')
                  setAuthModalOpen(true)
                }}
              >
                Sign In
              </button>
            </div>
          ) : (
            <div className="mt-4 p-3 bg-card border rounded-4 shadow-sm d-inline-flex align-items-center gap-3 text-start">
              <div
                className="rounded-circle d-flex align-items-center justify-content-center text-white fw-bold"
                style={{
                  width: '46px',
                  height: '46px',
                  background: 'linear-gradient(135deg, #3b82f6 0%, #6366f1 100%)'
                }}
              >
                {(user?.email?.[0] || 'U').toUpperCase()}
              </div>
              <div>
                <div className="d-flex align-items-center gap-2">
                  <span className="fw-bold">{user?.email}</span>
                  {roles.map(r => (
                    <Badge key={r} bg={r === 'Administrator' ? 'danger' : r === 'Recruiter' ? 'primary' : 'success'}>
                      {r}
                    </Badge>
                  ))}
                </div>
                <small className="text-muted d-block font-monospace">ID: {user?.id}</small>
              </div>
            </div>
          )}
        </div>

        {/* Feature Cards Grid */}
        <Row className="g-4 mb-5">
          <Col md={4}>
            <Card className="card-pro h-100 p-3">
              <Card.Body>
                <div
                  className="d-inline-flex p-3 rounded-3 mb-3"
                  style={{ background: 'rgba(59, 130, 246, 0.1)', color: '#3b82f6' }}
                >
                  <i className="bi bi-file-earmark-person fs-3"></i>
                </div>
                <h5 className="fw-bold">Dynamic Virtual CVs</h5>
                <p className="text-muted small">
                  Never tied to static tables. Profiles are rendered via Entity-Attribute-Value (EAV) records,
                  supporting custom attributes, Markdown projects, and Cloudinary photos.
                </p>
                <span className="badge bg-light text-dark border">Spec §1A &amp; §5</span>
              </Card.Body>
            </Card>
          </Col>

          <Col md={4}>
            <Card className="card-pro h-100 p-3">
              <Card.Body>
                <div
                  className="d-inline-flex p-3 rounded-3 mb-3"
                  style={{ background: 'rgba(99, 102, 241, 0.1)', color: '#6366f1' }}
                >
                  <i className="bi bi-funnel-fill fs-3"></i>
                </div>
                <h5 className="fw-bold">Smart Access Rules</h5>
                <p className="text-muted small">
                  Automated position matching. Define granular criteria (numeric ranges, certifications,
                  experience periods) with instant eligibility evaluation.
                </p>
                <span className="badge bg-light text-dark border">Spec §4 &amp; §7</span>
              </Card.Body>
            </Card>
          </Col>

          <Col md={4}>
            <Card className="card-pro h-100 p-3">
              <Card.Body>
                <div
                  className="d-inline-flex p-3 rounded-3 mb-3"
                  style={{ background: 'rgba(16, 185, 129, 0.1)', color: '#10b981' }}
                >
                  <i className="bi bi-shield-lock-fill fs-3"></i>
                </div>
                <h5 className="fw-bold">Optimistic Locking &amp; RBAC</h5>
                <p className="text-muted small">
                  Zero silent overwrite data loss. Row-version concurrency tokens safeguard simultaneous
                  saves while database-driven RBAC isolates Candidate, Recruiter, and Admin operations.
                </p>
                <span className="badge bg-light text-dark border">Spec §3.4 &amp; §9</span>
              </Card.Body>
            </Card>
          </Col>
        </Row>

        {/* System Health Status Panel */}
        <Card className="card-pro border-0 shadow-sm overflow-hidden">
          <div className="bg-light p-3 border-bottom d-flex justify-content-between align-items-center">
            <span className="fw-bold small text-uppercase tracking-wider text-muted">
              <i className="bi bi-cpu me-2"></i> System Infrastructure &amp; Connectivity
            </span>
            <button
              type="button"
              className="btn btn-sm btn-outline-secondary py-0 px-2"
              onClick={fetchHealth}
              disabled={healthLoading}
            >
              {healthLoading ? <Spinner size="sm" animation="border" /> : <i className="bi bi-arrow-clockwise"> Refresh</i>}
            </button>
          </div>
          <Card.Body className="p-4">
            <Row className="g-3">
              <Col sm={6} md={3}>
                <div className="p-3 rounded-3 border bg-body">
                  <div className="text-muted small">ASP.NET Core API</div>
                  <div className="fw-bold fs-5 mt-1 text-primary">.NET 10.0</div>
                  <Badge bg="success" className="mt-1">Active</Badge>
                </div>
              </Col>
              <Col sm={6} md={3}>
                <div className="p-3 rounded-3 border bg-body">
                  <div className="text-muted small">Database Provider</div>
                  <div className="fw-bold fs-5 mt-1 text-primary">Supabase Postgres</div>
                  <Badge bg={health?.database === 'Connected' ? 'success' : 'danger'} className="mt-1">
                    {health?.database || 'Checking...'}
                  </Badge>
                </div>
              </Col>
              <Col sm={6} md={3}>
                <div className="p-3 rounded-3 border bg-body">
                  <div className="text-muted small">Authentication</div>
                  <div className="fw-bold fs-5 mt-1 text-primary">Supabase JWT Bearer</div>
                  <Badge bg="success" className="mt-1">ES256 JWKS</Badge>
                </div>
              </Col>
              <Col sm={6} md={3}>
                <div className="p-3 rounded-3 border bg-body">
                  <div className="text-muted small">Frontend Framework</div>
                  <div className="fw-bold fs-5 mt-1 text-primary">React 19 + Vite</div>
                  <Badge bg="info" className="mt-1">Bootstrap 5</Badge>
                </div>
              </Col>
            </Row>
          </Card.Body>
        </Card>
      </Container>

      <AuthModal
        show={authModalOpen}
        onHide={() => setAuthModalOpen(false)}
        initialMode={authMode}
      />
    </div>
  )
}
