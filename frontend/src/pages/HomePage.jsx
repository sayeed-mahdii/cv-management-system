import React, { useState, useEffect } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { Container, Row, Col, Card, Table, Badge, Form, Alert, Modal, Button, Spinner } from 'react-bootstrap'
import { TagCloud } from 'react-tagcloud'
import { useTranslation } from 'react-i18next'
import { useAuth } from '../context/AuthContext'
import { api } from '../services/api'
import { AuthModal } from '../components/AuthModal'

export function HomePage() {
  const { t } = useTranslation()
  const { user, roles, isAuthenticated, isCandidate, isBlocked, blockedMessage } = useAuth()
  const navigate = useNavigate()

  // Dashboard Data State
  const [dashboard, setDashboard] = useState({
    stats: {
      totalPositions: 0,
      totalApplications: 0,
      totalCandidates: 0,
      totalRecruiters: 0,
      applicationsLast24Hours: 0
    },
    latestPositions: [],
    popularPositions: [],
    tagCloud: []
  })
  const [dashboardLoading, setDashboardLoading] = useState(true)
  const [dashboardError, setDashboardError] = useState(null)

  // Auth Modal State
  const [authModalOpen, setAuthModalOpen] = useState(false)
  const [authMode, setAuthMode] = useState('login')

  // Table Selection States (STRICT UI COMPLIANCE: Zero buttons inside rows)
  const [selectedLatestIds, setSelectedLatestIds] = useState([])
  const [selectedPopularIds, setSelectedPopularIds] = useState([])

  // Position Details & 1-Click Apply Modal State
  const [detailsModalOpen, setDetailsModalOpen] = useState(false)
  const [viewingPosition, setViewingPosition] = useState(null)
  const [detailsLoading, setDetailsLoading] = useState(false)
  const [applying, setApplying] = useState(false)
  const [applySuccess, setApplySuccess] = useState(false)
  const [applyError, setApplyError] = useState(null)

  // Fetch Dashboard
  const fetchDashboard = async () => {
    setDashboardLoading(true)
    setDashboardError(null)
    try {
      const data = await api.get('/api/dashboard')
      setDashboard({
        stats: data.stats || {
          totalPositions: 0,
          totalApplications: 0,
          totalCandidates: 0,
          totalRecruiters: 0,
          applicationsLast24Hours: 0
        },
        latestPositions: data.latestPositions || [],
        popularPositions: data.popularPositions || [],
        tagCloud: data.tagCloud || []
      })
    } catch (err) {
      setDashboardError(err.message || 'Failed to load dashboard metrics.')
    } finally {
      setDashboardLoading(false)
    }
  }

  useEffect(() => {
    fetchDashboard()
  }, [])

  // Selection handlers for Latest Positions
  const handleSelectAllLatest = (e) => {
    if (e.target.checked) setSelectedLatestIds(dashboard.latestPositions.map(p => p.id))
    else setSelectedLatestIds([])
  }

  const handleSelectLatestRow = (id) => {
    setSelectedLatestIds(prev =>
      prev.includes(id) ? prev.filter(item => item !== id) : [...prev, id]
    )
  }

  // Selection handlers for Popular Positions
  const handleSelectAllPopular = (e) => {
    if (e.target.checked) setSelectedPopularIds(dashboard.popularPositions.map(p => p.id))
    else setSelectedPopularIds([])
  }

  const handleSelectPopularRow = (id) => {
    setSelectedPopularIds(prev =>
      prev.includes(id) ? prev.filter(item => item !== id) : [...prev, id]
    )
  }

  // Open Details Modal
  const handleOpenDetails = async (positionId) => {
    setDetailsModalOpen(true)
    setDetailsLoading(true)
    setApplySuccess(false)
    setApplyError(null)
    try {
      const pos = await api.get(`/api/positions/${positionId}`)
      setViewingPosition(pos)
    } catch (err) {
      setApplyError('Failed to load position details.')
    } finally {
      setDetailsLoading(false)
    }
  }

  // Candidate 1-Click Apply
  const handleApply = async () => {
    if (!viewingPosition) return
    setApplying(true)
    setApplyError(null)
    try {
      await api.post(`/api/positions/${viewingPosition.id}/apply`, {})
      setApplySuccess(true)
      setViewingPosition(prev => ({
        ...prev,
        hasApplied: true,
        applicationsCount: (prev.applicationsCount || 0) + 1
      }))
      fetchDashboard()
    } catch (err) {
      setApplyError(err.message || 'Failed to submit application.')
    } finally {
      setApplying(false)
    }
  }

  // Custom Tag Cloud Tag Renderer
  const customTagRenderer = (tag, size) => {
    return (
      <span
        key={tag.value}
        style={{
          fontSize: `${Math.max(12, Math.min(26, size))}px`,
          margin: '4px',
          padding: '4px 10px',
          display: 'inline-block',
          cursor: 'pointer',
          borderRadius: '8px',
          transition: 'all 0.2s ease',
          verticalAlign: 'middle'
        }}
        className="badge bg-body border text-primary shadow-sm hover-lift"
        onClick={() => navigate(`/positions?tag=${encodeURIComponent(tag.value)}`)}
        title={`${tag.count} vacancy/project occurrences`}
      >
        #{tag.value}{' '}
        <span className="opacity-75 small font-monospace" style={{ fontSize: '0.75em' }}>
          ({tag.count})
        </span>
      </span>
    )
  }

  return (
    <div className="py-4">
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
            <i className="bi bi-award-fill"></i> {t('dashboard.heroBadge')}
          </div>
          <h1 className="display-5 fw-extrabold mb-3 tracking-tight">
            {t('dashboard.heroTitle')} <span className="text-primary">{t('dashboard.heroTitleSpan')}</span>
          </h1>
          <p className="lead text-muted mx-auto" style={{ maxWidth: '750px' }}>
            {t('dashboard.heroSubtitle')}
          </p>

          {!isAuthenticated ? (
            <div className="d-flex justify-content-center gap-3 mt-4">
              <button
                type="button"
                className="btn btn-primary-gradient px-4 py-2 rounded-pill fw-semibold shadow-sm"
                onClick={() => {
                  setAuthMode('register')
                  setAuthModalOpen(true)
                }}
              >
                <i className="bi bi-person-plus-fill me-2"></i>
                {t('dashboard.createCandidateAccount')}
              </button>
              <button
                type="button"
                className="btn btn-outline-secondary px-4 py-2 rounded-pill fw-semibold"
                onClick={() => {
                  setAuthMode('login')
                  setAuthModalOpen(true)
                }}
              >
                <i className="bi bi-box-arrow-in-right me-2"></i>
                {t('nav.signIn')}
              </button>
            </div>
          ) : (
            <div className="mt-3 p-3 bg-card border rounded-4 shadow-sm d-inline-flex align-items-center gap-3 text-start">
              <div
                className="rounded-circle d-flex align-items-center justify-content-center text-white fw-bold shadow-sm"
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
                <small className="text-muted font-monospace">Logged in as {roles.join(', ') || 'Candidate'}</small>
              </div>
              <div className="ms-3 border-start ps-3 d-none d-sm-block">
                <Link to="/positions" className="btn btn-sm btn-outline-primary rounded-pill">
                  Explore Positions <i className="bi bi-arrow-right ms-1"></i>
                </Link>
              </div>
            </div>
          )}
        </div>

        {/* Dashboard Error Alert */}
        {dashboardError && (
          <Alert variant="danger" className="mb-4">
            <i className="bi bi-exclamation-triangle-fill me-2"></i>
            {dashboardError}
          </Alert>
        )}

        {/* 1. Platform Statistics Counters Grid (Spec §4.1 / UC-1.1) */}
        <div className="mb-5">
          <div className="d-flex justify-content-between align-items-center mb-3">
            <h5 className="fw-bold mb-0 d-flex align-items-center gap-2">
              <i className="bi bi-speedometer2 text-primary"></i>
              {t('dashboard.platformLiveStats')}
            </h5>
            <small className="text-muted">{t('dashboard.singleBatchQuery')}</small>
          </div>

          <Row className="g-3">
            {/* 24h Applications */}
            <Col xs={12} sm={6} lg={true}>
              <Card className="card-pro h-100 border-0 shadow-sm p-3">
                <div className="d-flex align-items-center justify-content-between mb-2">
                  <span className="badge bg-warning-subtle text-warning-emphasis border border-warning-subtle small">
                    Last 24 Hours
                  </span>
                  <div className="text-warning fs-4">
                    <i className="bi bi-lightning-charge-fill"></i>
                  </div>
                </div>
                <div className="fs-2 fw-extrabold text-primary mb-1">
                  {dashboardLoading ? <Spinner size="sm" animation="border" /> : dashboard.stats.applicationsLast24Hours}
                </div>
                <div className="fw-semibold text-body small">{t('dashboard.apps24h')}</div>
                <small className="text-muted">{t('dashboard.apps24hSub')}</small>
              </Card>
            </Col>

            {/* Total Positions */}
            <Col xs={12} sm={6} lg={true}>
              <Card className="card-pro h-100 border-0 shadow-sm p-3">
                <div className="d-flex align-items-center justify-content-between mb-2">
                  <span className="badge bg-primary-subtle text-primary border border-primary-subtle small">
                    Active Catalog
                  </span>
                  <div className="text-primary fs-4">
                    <i className="bi bi-briefcase-fill"></i>
                  </div>
                </div>
                <div className="fs-2 fw-extrabold text-primary mb-1">
                  {dashboardLoading ? <Spinner size="sm" animation="border" /> : dashboard.stats.totalPositions}
                </div>
                <div className="fw-semibold text-body small">{t('dashboard.openVacancies')}</div>
                <small className="text-muted">{t('dashboard.openVacanciesSub')}</small>
              </Card>
            </Col>

            {/* Total Candidates */}
            <Col xs={12} sm={6} lg={true}>
              <Card className="card-pro h-100 border-0 shadow-sm p-3">
                <div className="d-flex align-items-center justify-content-between mb-2">
                  <span className="badge bg-success-subtle text-success border border-success-subtle small">
                    Talent Pool
                  </span>
                  <div className="text-success fs-4">
                    <i className="bi bi-people-fill"></i>
                  </div>
                </div>
                <div className="fs-2 fw-extrabold text-primary mb-1">
                  {dashboardLoading ? <Spinner size="sm" animation="border" /> : dashboard.stats.totalCandidates}
                </div>
                <div className="fw-semibold text-body small">{t('dashboard.candidates')}</div>
                <small className="text-muted">{t('dashboard.candidatesSub')}</small>
              </Card>
            </Col>

            {/* Total Recruiters */}
            <Col xs={12} sm={6} lg={true}>
              <Card className="card-pro h-100 border-0 shadow-sm p-3">
                <div className="d-flex align-items-center justify-content-between mb-2">
                  <span className="badge bg-info-subtle text-info border border-info-subtle small">
                    Hiring Teams
                  </span>
                  <div className="text-info fs-4">
                    <i className="bi bi-person-check-fill"></i>
                  </div>
                </div>
                <div className="fs-2 fw-extrabold text-primary mb-1">
                  {dashboardLoading ? <Spinner size="sm" animation="border" /> : dashboard.stats.totalRecruiters}
                </div>
                <div className="fw-semibold text-body small">{t('dashboard.recruiters')}</div>
                <small className="text-muted">{t('dashboard.recruitersSub')}</small>
              </Card>
            </Col>

            {/* Total Applications */}
            <Col xs={12} sm={6} lg={true}>
              <Card className="card-pro h-100 border-0 shadow-sm p-3">
                <div className="d-flex align-items-center justify-content-between mb-2">
                  <span className="badge bg-secondary-subtle text-secondary border border-secondary-subtle small">
                    Platform Volume
                  </span>
                  <div className="text-secondary fs-4">
                    <i className="bi bi-file-earmark-check-fill"></i>
                  </div>
                </div>
                <div className="fs-2 fw-extrabold text-primary mb-1">
                  {dashboardLoading ? <Spinner size="sm" animation="border" /> : dashboard.stats.totalApplications}
                </div>
                <div className="fw-semibold text-body small">{t('dashboard.totalSubmissions')}</div>
                <small className="text-muted">{t('dashboard.totalSubmissionsSub')}</small>
              </Card>
            </Col>
          </Row>
        </div>

        {/* 2. Interactive Technology Tag Cloud (Spec §4.1 / UC-1.1) */}
        <Card className="card-pro border-0 shadow-sm mb-5 overflow-hidden">
          <div className="p-3 bg-light border-bottom d-flex justify-content-between align-items-center">
            <div className="d-flex align-items-center gap-2">
              <i className="bi bi-cloud-haze2 text-primary fs-5"></i>
              <span className="fw-bold">{t('dashboard.trendingSkills')}</span>
              <Badge bg="primary-subtle" className="text-primary border border-primary-subtle">
                {t('dashboard.tagCloudBadge')}
              </Badge>
            </div>
            <small className="text-muted">{t('dashboard.tagCloudHint')}</small>
          </div>
          <Card.Body className="p-4 text-center">
            {dashboardLoading ? (
              <div className="py-4 text-muted">
                <Spinner size="sm" animation="border" className="me-2" />
                Aggregating technology keywords...
              </div>
            ) : dashboard.tagCloud.length === 0 ? (
              <p className="text-muted mb-0 small fst-italic">
                No technology tags found yet. Tags added to vacancies and projects will populate here.
              </p>
            ) : (
              <div className="py-2">
                <TagCloud
                  minSize={13}
                  maxSize={28}
                  tags={dashboard.tagCloud}
                  renderer={customTagRenderer}
                  className="d-flex flex-wrap justify-content-center align-items-center gap-1"
                />
              </div>
            )}
          </Card.Body>
        </Card>

        {/* 3. Latest Positions (Strict UI Compliance: Table layout, Toolbar, zero buttons in rows) */}
        <div className="mb-5">
          <div className="d-flex justify-content-between align-items-center mb-3">
            <div>
              <h5 className="fw-bold mb-0 d-flex align-items-center gap-2">
                <i className="bi bi-clock-history text-primary"></i>
                {t('dashboard.latestPositions')}
              </h5>
              <small className="text-muted">{t('dashboard.latestPositionsSub')}</small>
            </div>
            <Link to="/positions" className="btn btn-sm btn-outline-primary rounded-pill px-3">
              {t('dashboard.viewAllPositions')} ({dashboard.stats.totalPositions}) <i className="bi bi-arrow-right ms-1"></i>
            </Link>
          </div>

          {/* Top Toolbar (NO buttons inside table rows!) */}
          <div className="table-toolbar rounded-top border d-flex justify-content-between align-items-center p-3 bg-card">
            <div className="d-flex align-items-center gap-2 flex-wrap">
              <button
                type="button"
                className="btn btn-primary-gradient btn-sm px-3 d-flex align-items-center gap-2"
                onClick={() => {
                  if (selectedLatestIds.length === 1) handleOpenDetails(selectedLatestIds[0])
                }}
                disabled={selectedLatestIds.length !== 1}
              >
                <i className="bi bi-eye"></i>
                <span>{t('dashboard.viewDetailsApply')}</span>
              </button>

              {selectedLatestIds.length > 0 && (
                <Badge bg="secondary" pill className="ms-2 px-2 py-1">
                  {selectedLatestIds.length} selected
                </Badge>
              )}
            </div>

            <div className="text-muted small">
              Showing <strong>{dashboard.latestPositions.length}</strong> latest vacancies
            </div>
          </div>

          {/* Table */}
          <div className="table-responsive border border-top-0 rounded-bottom shadow-sm bg-card">
            <Table hover className="mb-0 align-middle">
              <thead className="table-light">
                <tr>
                  <th style={{ width: '42px' }} className="text-center">
                    <Form.Check
                      type="checkbox"
                      checked={dashboard.latestPositions.length > 0 && selectedLatestIds.length === dashboard.latestPositions.length}
                      onChange={handleSelectAllLatest}
                      aria-label="Select all latest positions"
                    />
                  </th>
                  <th>Position Title &amp; Details</th>
                  <th>Required Attributes</th>
                  <th>Technology Tags</th>
                  <th className="text-center">Applicants</th>
                  <th className="text-end">Last Updated</th>
                </tr>
              </thead>
              <tbody>
                {dashboardLoading ? (
                  <tr>
                    <td colSpan={6} className="text-center py-4">
                      <Spinner animation="border" size="sm" variant="primary" />
                      <span className="ms-2 text-muted small">Loading latest vacancies...</span>
                    </td>
                  </tr>
                ) : dashboard.latestPositions.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="text-center py-4 text-muted small">
                      No positions published yet.
                    </td>
                  </tr>
                ) : (
                  dashboard.latestPositions.map(pos => {
                    const isSelected = selectedLatestIds.includes(pos.id)
                    return (
                      <tr
                        key={pos.id}
                        className={isSelected ? 'table-primary bg-opacity-10' : ''}
                        onClick={() => handleSelectLatestRow(pos.id)}
                        onDoubleClick={() => handleOpenDetails(pos.id)}
                        style={{ cursor: 'pointer' }}
                      >
                        <td className="text-center" onClick={(e) => e.stopPropagation()}>
                          <Form.Check
                            type="checkbox"
                            checked={isSelected}
                            onChange={() => handleSelectLatestRow(pos.id)}
                            aria-label={`Select ${pos.title}`}
                          />
                        </td>
                        <td>
                          <div className="fw-semibold text-primary">{pos.title}</div>
                          <small className="text-muted text-truncate d-block" style={{ maxWidth: '340px' }}>
                            {pos.shortDescription}
                          </small>
                        </td>
                        <td>
                          <div className="d-flex gap-1 flex-wrap" style={{ maxWidth: '220px' }}>
                            {pos.attributeNames?.length > 0 ? (
                              pos.attributeNames.map((attr, i) => (
                                <Badge key={i} bg="light" text="dark" className="border small">
                                  {attr}
                                </Badge>
                              ))
                            ) : (
                              <span className="text-muted small fst-italic">Standard profile</span>
                            )}
                          </div>
                        </td>
                        <td>
                          <div className="d-flex gap-1 flex-wrap" style={{ maxWidth: '200px' }}>
                            {pos.tags?.length > 0 ? (
                              pos.tags.map((tag, i) => (
                                <span key={i} className="badge bg-body text-primary border small">
                                  #{tag}
                                </span>
                              ))
                            ) : (
                              <span className="text-muted small fst-italic">&mdash;</span>
                            )}
                          </div>
                        </td>
                        <td className="text-center">
                          <Badge bg="secondary-subtle" className="text-secondary border border-secondary-subtle px-2 py-1">
                            {pos.applicationsCount || 0}
                          </Badge>
                        </td>
                        <td className="text-end text-muted small">
                          {new Date(pos.updatedAt).toLocaleDateString()}
                        </td>
                      </tr>
                    )
                  })
                )}
              </tbody>
            </Table>
          </div>
        </div>

        {/* 4. Top 5 Popular Positions (Strict UI Compliance: Table layout, Toolbar, zero buttons in rows) */}
        <div className="mb-5">
          <div className="d-flex justify-content-between align-items-center mb-3">
            <div>
              <h5 className="fw-bold mb-0 d-flex align-items-center gap-2">
                <i className="bi bi-fire text-danger"></i>
                {t('dashboard.top5Popular')}
              </h5>
              <small className="text-muted">{t('dashboard.top5PopularSub')}</small>
            </div>
          </div>

          {/* Top Toolbar (NO buttons inside table rows!) */}
          <div className="table-toolbar rounded-top border d-flex justify-content-between align-items-center p-3 bg-card">
            <div className="d-flex align-items-center gap-2 flex-wrap">
              <button
                type="button"
                className="btn btn-primary-gradient btn-sm px-3 d-flex align-items-center gap-2"
                onClick={() => {
                  if (selectedPopularIds.length === 1) handleOpenDetails(selectedPopularIds[0])
                }}
                disabled={selectedPopularIds.length !== 1}
              >
                <i className="bi bi-eye"></i>
                <span>{t('dashboard.viewDetailsApply')}</span>
              </button>

              {selectedPopularIds.length > 0 && (
                <Badge bg="secondary" pill className="ms-2 px-2 py-1">
                  {selectedPopularIds.length} selected
                </Badge>
              )}
            </div>

            <div className="text-muted small">
              Ranked by total CV submissions
            </div>
          </div>

          {/* Table */}
          <div className="table-responsive border border-top-0 rounded-bottom shadow-sm bg-card">
            <Table hover className="mb-0 align-middle">
              <thead className="table-light">
                <tr>
                  <th style={{ width: '42px' }} className="text-center">
                    <Form.Check
                      type="checkbox"
                      checked={dashboard.popularPositions.length > 0 && selectedPopularIds.length === dashboard.popularPositions.length}
                      onChange={handleSelectAllPopular}
                      aria-label="Select all popular positions"
                    />
                  </th>
                  <th style={{ width: '90px' }} className="text-center">Rank</th>
                  <th>Position Title &amp; Details</th>
                  <th className="text-center">Applications</th>
                  <th>Technology Tags</th>
                  <th className="text-end">Last Updated</th>
                </tr>
              </thead>
              <tbody>
                {dashboardLoading ? (
                  <tr>
                    <td colSpan={6} className="text-center py-4">
                      <Spinner animation="border" size="sm" variant="primary" />
                      <span className="ms-2 text-muted small">Ranking popular positions...</span>
                    </td>
                  </tr>
                ) : dashboard.popularPositions.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="text-center py-4 text-muted small">
                      No applications recorded yet.
                    </td>
                  </tr>
                ) : (
                  dashboard.popularPositions.map((pos, idx) => {
                    const isSelected = selectedPopularIds.includes(pos.id)
                    const rank = idx + 1
                    const rankBadgeClass = rank === 1
                      ? 'bg-warning text-dark fw-bold'
                      : rank === 2
                        ? 'bg-secondary text-white'
                        : rank === 3
                          ? 'bg-secondary-subtle text-secondary border'
                          : 'bg-light text-muted border'

                    return (
                      <tr
                        key={pos.id}
                        className={isSelected ? 'table-primary bg-opacity-10' : ''}
                        onClick={() => handleSelectPopularRow(pos.id)}
                        onDoubleClick={() => handleOpenDetails(pos.id)}
                        style={{ cursor: 'pointer' }}
                      >
                        <td className="text-center" onClick={(e) => e.stopPropagation()}>
                          <Form.Check
                            type="checkbox"
                            checked={isSelected}
                            onChange={() => handleSelectPopularRow(pos.id)}
                            aria-label={`Select ${pos.title}`}
                          />
                        </td>
                        <td className="text-center">
                          <span className={`badge rounded-pill ${rankBadgeClass} px-2 py-1`}>
                            #{rank}
                          </span>
                        </td>
                        <td>
                          <div className="fw-semibold text-primary">{pos.title}</div>
                          <small className="text-muted text-truncate d-block" style={{ maxWidth: '360px' }}>
                            {pos.shortDescription}
                          </small>
                        </td>
                        <td className="text-center">
                          <Badge bg="danger-subtle" className="text-danger border border-danger-subtle px-3 py-1 fw-bold">
                            <i className="bi bi-fire me-1"></i>
                            {pos.applicationsCount || 0} applied
                          </Badge>
                        </td>
                        <td>
                          <div className="d-flex gap-1 flex-wrap" style={{ maxWidth: '220px' }}>
                            {pos.tags?.length > 0 ? (
                              pos.tags.map((tag, i) => (
                                <span key={i} className="badge bg-body text-primary border small">
                                  #{tag}
                                </span>
                              ))
                            ) : (
                              <span className="text-muted small fst-italic">&mdash;</span>
                            )}
                          </div>
                        </td>
                        <td className="text-end text-muted small">
                          {new Date(pos.updatedAt).toLocaleDateString()}
                        </td>
                      </tr>
                    )
                  })
                )}
              </tbody>
            </Table>
          </div>
        </div>

      </Container>

      {/* Position Details & 1-Click Apply Modal */}
      <Modal show={detailsModalOpen} onHide={() => setDetailsModalOpen(false)} size="lg" centered>
        <div className="p-4 bg-body rounded-4 shadow">
          {detailsLoading ? (
            <div className="text-center py-5">
              <Spinner animation="border" variant="primary" />
              <p className="mt-2 text-muted small">Loading position details...</p>
            </div>
          ) : viewingPosition ? (
            <>
              <div className="d-flex justify-content-between align-items-start mb-3">
                <div>
                  <h4 className="fw-bold mb-1">{viewingPosition.title}</h4>
                  <div className="d-flex gap-2 align-items-center">
                    <Badge bg="primary-subtle" className="text-primary border border-primary-subtle">
                      Max {viewingPosition.maxProjects} Projects in CV
                    </Badge>
                    <Badge bg="secondary-subtle" className="text-secondary border border-secondary-subtle">
                      {viewingPosition.applicationsCount || 0} Applicants
                    </Badge>
                  </div>
                </div>
                <button type="button" className="btn-close" onClick={() => setDetailsModalOpen(false)} />
              </div>

              {applySuccess && (
                <Alert variant="success" className="py-2 small">
                  <i className="bi bi-check-circle-fill me-2"></i>
                  Application submitted successfully! Your virtual CV is now available for recruiter review.
                </Alert>
              )}

              {applyError && (
                <Alert variant="danger" className="py-2 small">
                  <i className="bi bi-exclamation-triangle-fill me-2"></i>
                  {applyError}
                </Alert>
              )}

              <div className="mb-4">
                <h6 className="fw-bold">Job Overview</h6>
                <p className="text-muted small">{viewingPosition.shortDescription}</p>
              </div>

              {/* Required Skills & Attributes */}
              <div className="mb-4">
                <h6 className="fw-bold">Required Profile Attributes</h6>
                {viewingPosition.attributes?.length === 0 ? (
                  <p className="text-muted small fst-italic">Standard profile only (Me attributes).</p>
                ) : (
                  <div className="list-group list-group-flush border rounded-3">
                    {viewingPosition.attributes?.map(attr => (
                      <div key={attr.attributeDefinitionId} className="list-group-item d-flex justify-content-between align-items-center py-2">
                        <div>
                          <span className="fw-semibold me-2">{attr.name}</span>
                          <span className="badge bg-light text-dark border small me-1">{attr.categoryName}</span>
                          <span className="badge bg-primary-subtle text-primary border border-primary-subtle small">{attr.dataType}</span>
                        </div>
                        {attr.options?.length > 0 && (
                          <small className="text-muted">
                            Choices: {attr.options.join(', ')}
                          </small>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Relevant Project Tags */}
              <div className="mb-4">
                <h6 className="fw-bold">Technology Tags (Filters recent {viewingPosition.maxProjects} projects)</h6>
                <div className="d-flex gap-1 flex-wrap">
                  {viewingPosition.tags?.map((t, i) => (
                    <span key={i} className="badge bg-body text-primary border px-2 py-1">
                      #{t}
                    </span>
                  ))}
                </div>
              </div>

              {/* Action Buttons */}
              <div className="d-flex justify-content-between align-items-center pt-3 border-top">
                <Button variant="outline-secondary" onClick={() => setDetailsModalOpen(false)}>
                  Close
                </Button>

                {isAuthenticated ? (
                  isCandidate ? (
                    viewingPosition.hasApplied ? (
                      <Badge bg="success" className="px-3 py-2 fs-6">
                        <i className="bi bi-check2-circle me-1"></i> Already Applied
                      </Badge>
                    ) : (
                      <Button
                        className="btn-primary-gradient px-4"
                        onClick={handleApply}
                        disabled={applying}
                      >
                        {applying ? (
                          <>
                            <Spinner size="sm" animation="border" className="me-2" />
                            Submitting...
                          </>
                        ) : (
                          '1-Click Apply'
                        )}
                      </Button>
                    )
                  ) : (
                    <span className="text-muted small fst-italic">Logged in as Recruiter / Administrator (Preview mode)</span>
                  )
                ) : (
                  <Button
                    variant="primary"
                    className="btn-primary-gradient px-3"
                    onClick={() => {
                      setDetailsModalOpen(false)
                      setAuthMode('login')
                      setAuthModalOpen(true)
                    }}
                  >
                    Sign in to Apply
                  </Button>
                )}
              </div>
            </>
          ) : null}
        </div>
      </Modal>

      {/* Auth Modal for Guests */}
      <AuthModal
        show={authModalOpen}
        onHide={() => setAuthModalOpen(false)}
        initialMode={authMode}
      />
    </div>
  )
}
