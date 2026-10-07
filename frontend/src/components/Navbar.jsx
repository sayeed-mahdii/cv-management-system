import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Navbar, Nav, Container, Dropdown, Badge, Form } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useAuth } from '../context/AuthContext'
import { useSupportTicket } from '../context/SupportTicketContext'
import { AuthModal } from './AuthModal'

export function AppNavbar() {
  const { t, i18n } = useTranslation()
  const { openTicketModal } = useSupportTicket()
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
  const [searchQuery, setSearchQuery] = useState('')
  const navigate = useNavigate()

  const toggleLanguage = () => {
    const nextLang = i18n.language.startsWith('ru') ? 'en' : 'ru'
    i18n.changeLanguage(nextLang)
    localStorage.setItem('recruitov_language', nextLang)
  }

  const handleSearchSubmit = (e) => {
    e.preventDefault()
    if (searchQuery.trim()) {
      navigate(`/search?q=${encodeURIComponent(searchQuery.trim())}`)
    }
  }

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
                <i className="bi bi-compass me-1"></i> {t('nav.positions')}
              </Link>

              {/* Candidate navigation */}
              {isAuthenticated && isCandidate && (
                <>
                  <Link to={user?.id ? `/profile/${user.id}` : '/profile'} className="nav-link px-3 fw-medium">
                    <i className="bi bi-person-badge me-1"></i> {t('nav.myProfile')}
                  </Link>
                  <Link to="/my-cvs" className="nav-link px-3 fw-medium">
                    <i className="bi bi-file-earmark-person me-1"></i> {t('nav.myCvs')}
                  </Link>
                </>
              )}

              {/* Recruiter navigation */}
              {isAuthenticated && isRecruiter && (
                <>
                  <Link to="/candidates" className="nav-link px-3 fw-medium">
                    <i className="bi bi-search me-1"></i> {t('nav.candidates')}
                  </Link>
                  <Link to="/attributes" className="nav-link px-3 fw-medium">
                    <i className="bi bi-tags me-1"></i> {t('nav.attributes')}
                  </Link>
                </>
              )}

              {/* Administrator navigation */}
              {isAuthenticated && isAdmin && (
                <>
                  <Link to="/admin/users" className="nav-link px-3 fw-medium text-danger-emphasis">
                    <i className="bi bi-people-fill me-1"></i> {t('nav.userManagement')}
                  </Link>
                  {!isRecruiter && (
                    <Link to="/attributes" className="nav-link px-3 fw-medium">
                      <i className="bi bi-tags me-1"></i> {t('nav.attributes')}
                    </Link>
                  )}
                </>
              )}
            </Nav>

            {/* Right Controls: Global Search, Language Toggle, Theme Toggle, Auth Buttons, User Profile */}
            <div className="d-flex align-items-center gap-2 mt-3 mt-lg-0">
              {/* Global Full-Text Search Bar */}
              <Form onSubmit={handleSearchSubmit} className="d-flex align-items-center me-1 position-relative" style={{ maxWidth: '220px' }}>
                <div className="input-group input-group-sm">
                  <span className="input-group-text bg-body border-end-0 text-muted px-2">
                    <i className="bi bi-search"></i>
                  </span>
                  <Form.Control
                    type="search"
                    placeholder={t('nav.searchPlaceholder')}
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="border-start-0 rounded-end form-control-pro"
                    style={{ fontSize: '13px' }}
                    aria-label="Global search"
                  />
                </div>
              </Form>

              {/* Language Switcher Toggle */}
              <button
                type="button"
                className="btn btn-sm btn-outline-secondary rounded-pill px-2 d-flex align-items-center justify-content-center fw-bold"
                style={{ height: '36px', minWidth: '44px', fontSize: '12px' }}
                onClick={toggleLanguage}
                title={i18n.language.startsWith('ru') ? 'Switch to English' : 'Переключить на Русский'}
              >
                {i18n.language.startsWith('ru') ? '🇷🇺 RU' : '🇬🇧 EN'}
              </button>

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

              {/* Help & Support Ticket Button */}
              <button
                type="button"
                id="navbar-help-ticket-btn"
                className="btn btn-sm btn-outline-secondary rounded-circle d-flex align-items-center justify-content-center"
                style={{ width: '36px', height: '36px' }}
                onClick={() => openTicketModal()}
                title="Help / Create Support Ticket"
                aria-label="Help and Support Ticket"
              >
                <i className="bi bi-question-circle"></i>
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
                        <i className="bi bi-person me-2"></i> {t('nav.myProfile')}
                      </Dropdown.Item>

                      <Dropdown.Item as={Link} to="/settings" className="rounded-2 py-2">
                        <i className="bi bi-gear me-2"></i> {t('nav.preferences')}
                      </Dropdown.Item>

                      <Dropdown.Divider />

                      <Dropdown.Item onClick={handleSignOut} className="rounded-2 py-2 text-danger">
                        <i className="bi bi-box-arrow-right me-2"></i> {t('nav.signOut')}
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
                    {t('nav.signIn')}
                  </button>
                  <button
                    type="button"
                    className="btn btn-sm btn-primary-gradient px-3 fw-medium"
                    onClick={() => handleOpenAuth('register')}
                  >
                    {t('nav.getStarted')}
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
