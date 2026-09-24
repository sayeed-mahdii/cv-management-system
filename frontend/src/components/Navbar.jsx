import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Navbar, Nav, Container, Dropdown, Badge } from 'react-bootstrap'
import { useAuth } from '../context/AuthContext'
import { AuthModal } from './AuthModal'

export function AppNavbar() {
  const {
    user,
    profile,
    roles,
    isAuthenticated,
    isCandidate,
    isRecruiter,
    isAdmin,
    isBlocked,
    theme,
    toggleTheme,
    logout
  } = useAuth()

  const [authModalOpen, setAuthModalOpen] = useState(false)
  const [authMode, setAuthMode] = useState('login')
  const navigate = useNavigate()

  const handleOpenAuth = (mode) => {
    setAuthMode(mode)
    setAuthModalOpen(true)
  }

  const handleSignOut = async () => {
    await logout()
    navigate('/')
  }

  // Determine highest-priority badge to display
  const primaryRole = isAdmin
    ? 'Administrator'
    : isRecruiter
      ? 'Recruiter'
      : isCandidate
        ? 'Candidate'
        : null

  const roleBadgeClass = isAdmin
    ? 'badge-role badge-role-admin'
    : isRecruiter
      ? 'badge-role badge-role-recruiter'
      : 'badge-role badge-role-candidate'

  return (
    <>
      <Navbar expand="lg" className="border-bottom glass-panel sticky-top py-2">
        <Container>
          {/* Brand */}
          <Link to="/" className="navbar-brand d-flex align-items-center gap-2 text-decoration-none">
            <span
              className="d-inline-flex align-items-center justify-content-center text-white rounded-3 shadow-sm"
              style={{
                width: '36px',
                height: '36px',
                background: 'linear-gradient(135deg, #3b82f6 0%, #6366f1 100%)',
                fontSize: '18px'
              }}
            >
              <i className="bi bi-briefcase-fill"></i>
            </span>
            <span className="fw-bold fs-4 tracking-tight" style={{ letterSpacing: '-0.03em' }}>
              re<span className="text-primary">C</span>ruito<span className="text-primary">V</span>
            </span>
          </Link>

          <Navbar.Toggle aria-controls="main-navbar-nav" />

          <Navbar.Collapse id="main-navbar-nav">
            <Nav className="me-auto ms-lg-4 gap-1">
              {/* Public / Common links */}
              <Link to="/positions" className="nav-link px-3 fw-medium">
                <i className="bi bi-compass me-1"></i> Positions
              </Link>

              {/* Candidate navigation */}
              {isAuthenticated && isCandidate && (
                <>
                  <Link to={user?.id ? `/profile/${user.id}` : '/profile'} className="nav-link px-3 fw-medium">
                    <i className="bi bi-person-badge me-1"></i> My Profile
                  </Link>
                  <Link to="/my-cvs" className="nav-link px-3 fw-medium">
                    <i className="bi bi-file-earmark-person me-1"></i> My CVs
                  </Link>
                </>
              )}

              {/* Recruiter navigation */}
              {isAuthenticated && isRecruiter && (
                <>
                  <Link to="/candidates" className="nav-link px-3 fw-medium">
                    <i className="bi bi-search me-1"></i> Candidate Search
                  </Link>
                  <Link to="/attributes" className="nav-link px-3 fw-medium">
                    <i className="bi bi-tags me-1"></i> Attribute Library
                  </Link>
                </>
              )}

              {/* Administrator navigation */}
              {isAuthenticated && isAdmin && (
                <>
                  <Link to="/admin/users" className="nav-link px-3 fw-medium text-danger-emphasis">
                    <i className="bi bi-people-fill me-1"></i> User Management
                  </Link>
                  {!isRecruiter && (
                    <Link to="/attributes" className="nav-link px-3 fw-medium">
                      <i className="bi bi-tags me-1"></i> Attribute Library
                    </Link>
                  )}
                </>
              )}
            </Nav>

            {/* Right Controls: Theme Toggle, Auth Buttons, User Profile */}
            <div className="d-flex align-items-center gap-2 mt-3 mt-lg-0">
              {/* Theme Switcher Button */}
              <button
                type="button"
                className="btn btn-sm btn-outline-secondary rounded-circle d-flex align-items-center justify-content-center"
                style={{ width: '36px', height: '36px' }}
                onClick={toggleTheme}
                title={theme === 'dark' ? 'Switch to Light Mode' : 'Switch to Dark Mode'}
              >
                {theme === 'dark' ? (
                  <i className="bi bi-sun-fill text-warning"></i>
                ) : (
                  <i className="bi bi-moon-stars-fill text-primary"></i>
                )}
              </button>

              {isAuthenticated ? (
                <div className="d-flex align-items-center gap-2">
                  {/* Primary Role Badge */}
                  {primaryRole && (
                    <span className={roleBadgeClass}>
                      {primaryRole}
                    </span>
                  )}

                  {isBlocked && (
                    <Badge bg="danger" className="text-uppercase">
                      Blocked
                    </Badge>
                  )}

                  {/* User Profile Dropdown */}
                  <Dropdown align="end">
                    <Dropdown.Toggle
                      variant="link"
                      id="dropdown-user"
                      className="d-flex align-items-center text-decoration-none p-0 border-0"
                    >
                      <div
                        className="rounded-circle d-flex align-items-center justify-content-center text-white fw-semibold shadow-sm"
                        style={{
                          width: '36px',
                          height: '36px',
                          background: 'linear-gradient(135deg, #6366f1 0%, #a855f7 100%)',
                          fontSize: '14px'
                        }}
                      >
                        {(user?.email?.[0] || 'U').toUpperCase()}
                      </div>
                    </Dropdown.Toggle>

                    <Dropdown.Menu className="shadow-lg border-0 rounded-3 mt-2 p-2" style={{ minWidth: '220px' }}>
                      <div className="px-3 py-2 border-bottom mb-1">
                        <p className="mb-0 fw-semibold text-truncate small">{user?.email}</p>
                        <small className="text-muted text-uppercase" style={{ fontSize: '10px' }}>
                          Roles: {roles.join(', ') || 'Candidate'}
                        </small>
                      </div>

                      <Dropdown.Item as={Link} to={user?.id ? `/profile/${user.id}` : '/profile'} className="rounded-2 py-2">
                        <i className="bi bi-person me-2"></i> View Profile
                      </Dropdown.Item>

                      <Dropdown.Item as={Link} to="/settings" className="rounded-2 py-2">
                        <i className="bi bi-gear me-2"></i> Preferences
                      </Dropdown.Item>

                      <Dropdown.Divider />

                      <Dropdown.Item onClick={handleSignOut} className="rounded-2 py-2 text-danger">
                        <i className="bi bi-box-arrow-right me-2"></i> Sign Out
                      </Dropdown.Item>
                    </Dropdown.Menu>
                  </Dropdown>
                </div>
              ) : (
                <div className="d-flex align-items-center gap-2">
                  <button
                    type="button"
                    className="btn btn-sm btn-outline-primary px-3 fw-medium"
                    onClick={() => handleOpenAuth('login')}
                  >
                    Sign In
                  </button>
                  <button
                    type="button"
                    className="btn btn-sm btn-primary-gradient px-3 fw-medium"
                    onClick={() => handleOpenAuth('register')}
                  >
                    Get Started
                  </button>
                </div>
              )}
            </div>
          </Navbar.Collapse>
        </Container>
      </Navbar>

      <AuthModal
        show={authModalOpen}
        onHide={() => setAuthModalOpen(false)}
        initialMode={authMode}
      />
    </>
  )
}
