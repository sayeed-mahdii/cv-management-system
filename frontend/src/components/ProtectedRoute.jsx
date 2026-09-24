import React from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { Spinner, Alert, Container } from 'react-bootstrap'

export function ProtectedRoute({ children, allowedRoles = [] }) {
  const { isAuthenticated, isLoading, roles, isBlocked, blockedMessage } = useAuth()

  if (isLoading) {
    return (
      <Container className="d-flex justify-content-center align-items-center py-5" style={{ minHeight: '60vh' }}>
        <div className="text-center">
          <Spinner animation="border" variant="primary" />
          <p className="mt-3 text-muted small">Validating security credentials...</p>
        </div>
      </Container>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/" replace />
  }

  if (isBlocked) {
    return (
      <Container className="py-5">
        <Alert variant="danger" className="text-center p-4 rounded-4 shadow-sm">
          <i className="bi bi-shield-x-fill text-danger fs-1 d-block mb-2"></i>
          <h4 className="fw-bold">Account Access Suspended</h4>
          <p className="mb-0 text-muted">
            {blockedMessage || 'Your account has been blocked by an administrator. All administrative and editing actions are restricted.'}
          </p>
        </Alert>
      </Container>
    )
  }

  if (allowedRoles.length > 0 && !allowedRoles.some(role => roles.includes(role))) {
    return (
      <Container className="py-5">
        <Alert variant="warning" className="text-center p-4 rounded-4 shadow-sm">
          <i className="bi bi-lock-fill text-warning fs-1 d-block mb-2"></i>
          <h4 className="fw-bold">Access Restricted</h4>
          <p className="mb-0 text-muted">
            You do not have the required permissions ({allowedRoles.join(' or ')}) to view this section.
          </p>
        </Alert>
      </Container>
    )
  }

  return children
}
