import React, { useState, useEffect, useCallback } from 'react'
import { Container, Table, Badge, Form, Alert, Modal, Button, Spinner, Row, Col } from 'react-bootstrap'
import CreatableSelect from 'react-select/creatable'
import Select from 'react-select'
import { useSearchParams } from 'react-router-dom'
import { api } from '../services/api'
import { useAuth } from '../context/AuthContext'
import { ApplicantsModal } from '../components/ApplicantsModal'

export function PositionsPage() {
  const { user, isAuthenticated, isCandidate, isRecruiter, isAdmin } = useAuth()
  const canManagePositions = isRecruiter || isAdmin
  const [searchParams, setSearchParams] = useSearchParams()
  const tagFromUrl = searchParams.get('tag') || ''

  // Positions list states
  const [positions, setPositions] = useState([])
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(1)
  const [page, setPage] = useState(1)
  const [pageSize] = useState(10)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  // Filters
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [selectedTag, setSelectedTag] = useState(tagFromUrl)

  // Selection states for Toolbar (NO buttons in table rows!)
  const [selectedIds, setSelectedIds] = useState([])

  // Create / Edit Modal
  const [modalOpen, setModalOpen] = useState(false)
  const [isEditing, setIsEditing] = useState(false)
  const [currentPosition, setCurrentPosition] = useState(null)
  const [formData, setFormData] = useState({
    title: '',
    shortDescription: '',
    maxProjects: 3,
    version: 1,
    attributeIds: [],
    tags: []
  })
  const [formSaving, setFormSaving] = useState(false)
  const [formError, setFormError] = useState(null)
  const [concurrencyConflict, setConcurrencyConflict] = useState(false)

  // Available attributes for picker
  const [availableAttributes, setAvailableAttributes] = useState([])

  // Details & 1-Click Apply Modal
  const [detailsModalOpen, setDetailsModalOpen] = useState(false)
  const [viewingPosition, setViewingPosition] = useState(null)
  const [detailsLoading, setDetailsLoading] = useState(false)
  const [applying, setApplying] = useState(false)
  const [applySuccess, setApplySuccess] = useState(false)
  const [applyError, setApplyError] = useState(null)

  // Delete modal
  const [deleteModalOpen, setDeleteModalOpen] = useState(false)
  const [deleting, setDeleting] = useState(false)

  // Recruiter Applicants Review Modal
  const [applicantsModalOpen, setApplicantsModalOpen] = useState(false)
  const [selectedPositionForApplicants, setSelectedPositionForApplicants] = useState(null)

  // Debounce search
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(search)
      setPage(1)
    }, 350)
    return () => clearTimeout(handler)
  }, [search])

  // Sync URL tag param
  useEffect(() => {
    if (tagFromUrl) {
      setSelectedTag(tagFromUrl)
      setPage(1)
    }
  }, [tagFromUrl])

  // Fetch attributes for form picker
  useEffect(() => {
    if (canManagePositions) {
      api.get('/api/attributes?pageSize=100')
        .then(res => setAvailableAttributes(res.items || []))
        .catch(err => console.error('Failed to load attributes for picker:', err))
    }
  }, [canManagePositions])

  // Fetch Positions
  const fetchPositions = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: pageSize.toString()
      })
      if (debouncedSearch) params.append('search', debouncedSearch)
      if (selectedTag) params.append('tag', selectedTag)

      const res = await api.get(`/api/positions?${params.toString()}`)
      setPositions(res.items || [])
      setTotalCount(res.totalCount || 0)
      setTotalPages(res.totalPages || 1)
      setSelectedIds([])
    } catch (err) {
      setError(err.message || 'Failed to fetch positions.')
    } finally {
      setLoading(false)
    }
  }, [page, pageSize, debouncedSearch, selectedTag])

  useEffect(() => {
    fetchPositions()
  }, [fetchPositions])

  // Selection handlers
  const handleSelectAll = (e) => {
    if (e.target.checked) setSelectedIds(positions.map(p => p.id))
    else setSelectedIds([])
  }

  const handleSelectRow = (id) => {
    setSelectedIds(prev =>
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
      setViewingPosition(prev => ({ ...prev, hasApplied: true, applicationsCount: prev.applicationsCount + 1 }))
      fetchPositions()
    } catch (err) {
      setApplyError(err.message || 'Failed to submit application.')
    } finally {
      setApplying(false)
    }
  }

  // Open Create Modal
  const handleOpenCreate = () => {
    setIsEditing(false)
    setCurrentPosition(null)
    setFormData({
      title: '',
      shortDescription: '',
      maxProjects: 3,
      version: 1,
      attributeIds: [],
      tags: []
    })
    setFormError(null)
    setConcurrencyConflict(false)
    setModalOpen(true)
  }

  // Open Edit Modal
  const handleOpenEdit = async () => {
    if (selectedIds.length !== 1) return
    const idToEdit = selectedIds[0]
    setFormError(null)
    setConcurrencyConflict(false)
    try {
      const pos = await api.get(`/api/positions/${idToEdit}`)
      setIsEditing(true)
      setCurrentPosition(pos)
      setFormData({
        title: pos.title,
        shortDescription: pos.shortDescription,
        maxProjects: pos.maxProjects,
        version: pos.version,
        attributeIds: pos.attributes.map(a => a.attributeDefinitionId),
        tags: pos.tags || []
      })
      setModalOpen(true)
    } catch (err) {
      alert(`Failed to load position: ${err.message}`)
    }
  }

  // Open Recruiter Applicants Review Modal
  const handleOpenApplicants = (positionToInspect = null) => {
    const pos = positionToInspect || (selectedIds.length === 1 ? positions.find(p => p.id === selectedIds[0]) : null)
    if (pos) {
      setSelectedPositionForApplicants(pos)
      setApplicantsModalOpen(true)
    }
  }

  // Duplicate Position (1-Click Clone)
  const handleDuplicate = async () => {
    if (selectedIds.length !== 1) return
    const idToDup = selectedIds[0]
    try {
      await api.post(`/api/positions/${idToDup}/duplicate`, {})
      setSelectedIds([])
      fetchPositions()
    } catch (err) {
      alert(`Duplicate failed: ${err.message}`)
    }
  }

  // Submit Create or Edit Form
  const handleFormSubmit = async (e) => {
    e.preventDefault()
    setFormError(null)
    setConcurrencyConflict(false)
    setFormSaving(true)

    try {
      if (isEditing) {
        await api.put(`/api/positions/${currentPosition.id}`, {
          title: formData.title.trim(),
          shortDescription: formData.shortDescription.trim(),
          maxProjects: parseInt(formData.maxProjects, 10),
          version: formData.version,
          attributeIds: formData.attributeIds,
          tags: formData.tags
        })
      } else {
        await api.post('/api/positions', {
          title: formData.title.trim(),
          shortDescription: formData.shortDescription.trim(),
          maxProjects: parseInt(formData.maxProjects, 10),
          attributeIds: formData.attributeIds,
          tags: formData.tags
        })
      }

      setModalOpen(false)
      fetchPositions()
    } catch (err) {
      if (err.status === 409) {
        setConcurrencyConflict(true)
        setFormError('Concurrency conflict: This position was modified by another user.')
      } else {
        setFormError(err.message || 'Operation failed.')
      }
    } finally {
      setFormSaving(false)
    }
  }

  // Delete Selected Positions
  const handleDeleteConfirm = async () => {
    setDeleting(true)
    try {
      for (const id of selectedIds) {
        await api.delete(`/api/positions/${id}`)
      }
      setDeleteModalOpen(false)
      setSelectedIds([])
      fetchPositions()
    } catch (err) {
      alert(`Deletion error: ${err.message}`)
    } finally {
      setDeleting(false)
    }
  }

  // Prepare attribute options for Select
  const attributeSelectOptions = availableAttributes.map(a => ({
    value: a.id,
    label: `${a.name} (${a.categoryName} - ${a.dataType})`
  }))

  return (
    <Container className="py-5">
      {/* Header */}
      <div className="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center mb-4 gap-3">
        <div>
          <div className="d-flex align-items-center gap-2 mb-1">
            <h3 className="fw-bold mb-0">Open Positions</h3>
            <Badge bg="primary" pill className="px-2 py-1 small">
              Vacancies &amp; Templates
            </Badge>
          </div>
          <p className="text-muted small mb-0">
            Browse company job openings, review eligibility requirements, and submit your portfolio CV.
          </p>
        </div>
      </div>

      {error && (
        <Alert variant="danger" className="mb-4">
          <i className="bi bi-exclamation-triangle-fill me-2"></i>
          {error}
        </Alert>
      )}

      {/* Search and Tag Filter Bar */}
      <div className="card-pro p-3 mb-4">
        <div className="row g-3 align-items-center">
          <div className="col-md-7">
            <div className="input-group">
              <span className="input-group-text bg-body border-end-0">
                <i className="bi bi-search text-muted"></i>
              </span>
              <Form.Control
                type="text"
                placeholder="Search positions by title or keyword..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="border-start-0 form-control-pro"
              />
              {search && (
                <button
                  type="button"
                  className="btn btn-outline-secondary border-start-0"
                  onClick={() => setSearch('')}
                >
                  <i className="bi bi-x"></i>
                </button>
              )}
            </div>
          </div>

          <div className="col-md-5 d-flex gap-2 justify-content-md-end align-items-center">
            {selectedTag && (
              <Badge bg="primary" className="p-2 d-flex align-items-center gap-2">
                <span>Filter: #{selectedTag}</span>
                <i className="bi bi-x-circle" style={{ cursor: 'pointer' }} onClick={() => { setSelectedTag(''); setSearchParams({}); }} />
              </Badge>
            )}
            <small className="text-muted">
              Total: <strong>{totalCount}</strong> open position(s)
            </small>
          </div>
        </div>
      </div>

      {/* STRICT UI COMPLIANCE TOOLBAR (Zero buttons inside rows!) */}
      <div className="table-toolbar rounded-top border d-flex justify-content-between align-items-center p-3 bg-card">
        <div className="d-flex align-items-center gap-2 flex-wrap">
          {/* Action to view details */}
          <button
            type="button"
            className="btn btn-outline-primary btn-sm px-3 d-flex align-items-center gap-2"
            onClick={() => selectedIds.length === 1 && handleOpenDetails(selectedIds[0])}
            disabled={selectedIds.length !== 1}
          >
            <i className="bi bi-eye"></i>
            <span>View &amp; Apply</span>
          </button>

          {/* Recruiter / Admin Management Actions */}
          {canManagePositions && (
            <>
              <button
                type="button"
                className="btn btn-primary-gradient btn-sm px-3 d-flex align-items-center gap-2"
                onClick={handleOpenCreate}
              >
                <i className="bi bi-plus-lg"></i>
                <span>New Position</span>
              </button>

              <button
                type="button"
                className="btn btn-outline-secondary btn-sm px-3 d-flex align-items-center gap-2"
                onClick={handleOpenEdit}
                disabled={selectedIds.length !== 1}
              >
                <i className="bi bi-pencil"></i>
                <span>Edit</span>
              </button>

              <button
                type="button"
                className="btn btn-outline-secondary btn-sm px-3 d-flex align-items-center gap-2"
                onClick={handleDuplicate}
                disabled={selectedIds.length !== 1}
                title="Clone position template"
              >
                <i className="bi bi-copy"></i>
                <span>Duplicate</span>
              </button>

              <button
                type="button"
                className="btn btn-outline-info btn-sm px-3 d-flex align-items-center gap-2"
                onClick={() => handleOpenApplicants()}
                disabled={selectedIds.length !== 1}
                title="Review applicants for selected position"
              >
                <i className="bi bi-people-fill"></i>
                <span>Applicants</span>
              </button>

              <button
                type="button"
                className="btn btn-outline-danger btn-sm px-3 d-flex align-items-center gap-2"
                onClick={() => setDeleteModalOpen(true)}
                disabled={selectedIds.length === 0}
              >
                <i className="bi bi-trash3"></i>
                <span>Delete</span>
              </button>
            </>
          )}

          {selectedIds.length > 0 && (
            <Badge bg="secondary" pill className="ms-2 px-2 py-1">
              {selectedIds.length} selected
            </Badge>
          )}
        </div>

        <div className="text-muted small">
          Showing <strong>{positions.length}</strong> of <strong>{totalCount}</strong>
        </div>
      </div>

      {/* Positions Table (Strictly NO buttons in table rows) */}
      <div className="table-responsive border border-top-0 rounded-bottom shadow-sm bg-card mb-4">
        <Table hover className="mb-0 align-middle">
          <thead className="table-light">
            <tr>
              <th style={{ width: '42px' }} className="text-center">
                <Form.Check
                  type="checkbox"
                  checked={positions.length > 0 && selectedIds.length === positions.length}
                  onChange={handleSelectAll}
                  aria-label="Select all positions"
                />
              </th>
              <th>Position Title</th>
              <th>Required Skills / Attributes</th>
              <th>Technology Tags</th>
              <th className="text-center">Max Projects</th>
              <th className="text-center">Applicants</th>
              <th className="text-end">Last Updated</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={7} className="text-center py-5">
                  <Spinner animation="border" variant="primary" />
                  <p className="mt-2 text-muted small mb-0">Loading open positions...</p>
                </td>
              </tr>
            ) : positions.length === 0 ? (
              <tr>
                <td colSpan={7} className="text-center py-5 text-muted">
                  <i className="bi bi-briefcase fs-2 d-block mb-2 text-dimmed"></i>
                  <p className="mb-1 fw-semibold">No positions found</p>
                  <small>
                    {debouncedSearch
                      ? `No positions match keyword "${debouncedSearch}".`
                      : 'Check back soon or post a new vacancy.'}
                  </small>
                </td>
              </tr>
            ) : (
              positions.map((pos) => {
                const isSelected = selectedIds.includes(pos.id)
                return (
                  <tr
                    key={pos.id}
                    className={isSelected ? 'table-primary bg-opacity-10' : ''}
                    onClick={() => handleSelectRow(pos.id)}
                    onDoubleClick={() => handleOpenDetails(pos.id)}
                    style={{ cursor: 'pointer' }}
                  >
                    <td className="text-center" onClick={(e) => e.stopPropagation()}>
                      <Form.Check
                        type="checkbox"
                        checked={isSelected}
                        onChange={() => handleSelectRow(pos.id)}
                        aria-label={`Select ${pos.title}`}
                      />
                    </td>
                    <td>
                      <div className="fw-semibold text-primary">{pos.title}</div>
                      <small className="text-muted text-truncate d-block" style={{ maxWidth: '300px' }}>
                        {pos.shortDescription}
                      </small>
                    </td>
                    <td>
                      <div className="d-flex gap-1 flex-wrap" style={{ maxWidth: '240px' }}>
                        {pos.attributeNames?.length > 0 ? (
                          pos.attributeNames.map((name, i) => (
                            <span key={i} className="badge bg-light text-dark border small">
                              {name}
                            </span>
                          ))
                        ) : (
                          <span className="text-muted small fst-italic">Standard profile</span>
                        )}
                      </div>
                    </td>
                    <td>
                      <div className="d-flex gap-1 flex-wrap" style={{ maxWidth: '200px' }}>
                        {pos.tags?.map((tag, i) => (
                          <span
                            key={i}
                            className="badge bg-body text-primary border small"
                            onClick={(e) => {
                              e.stopPropagation()
                              setSelectedTag(tag)
                            }}
                            style={{ cursor: 'pointer' }}
                          >
                            #{tag}
                          </span>
                        ))}
                      </div>
                    </td>
                    <td className="text-center">
                      <span className="badge bg-secondary-subtle text-secondary border">
                        {pos.maxProjects}
                      </span>
                    </td>
                    <td className="text-center">
                      <span
                        className={`badge bg-info-subtle text-info-emphasis border ${canManagePositions ? 'cursor-pointer' : ''}`}
                        onClick={(e) => {
                          if (canManagePositions) {
                            e.stopPropagation()
                            handleOpenApplicants(pos)
                          }
                        }}
                        title={canManagePositions ? 'Click to view applicants' : ''}
                        style={{ cursor: canManagePositions ? 'pointer' : 'default' }}
                      >
                        {pos.applicationsCount}
                      </span>
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

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="d-flex justify-content-between align-items-center">
          <small className="text-muted">
            Page {page} of {totalPages}
          </small>
          <div className="d-flex gap-2">
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm px-3"
              disabled={page <= 1 || loading}
              onClick={() => setPage(p => Math.max(1, p - 1))}
            >
              <i className="bi bi-chevron-left me-1"></i> Previous
            </button>
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm px-3"
              disabled={page >= totalPages || loading}
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            >
              Next <i className="bi bi-chevron-right ms-1"></i>
            </button>
          </div>
        </div>
      )}

      {/* MODAL: POSITION DETAILS & 1-CLICK APPLY */}
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
                  <div className="d-flex align-items-center gap-3 text-muted small">
                    <span>
                      <i className="bi bi-people me-1"></i> {viewingPosition.applicationsCount} candidate(s) applied
                    </span>
                    <span>
                      <i className="bi bi-clock me-1"></i> Updated {new Date(viewingPosition.updatedAt).toLocaleDateString()}
                    </span>
                  </div>
                </div>
                <button type="button" className="btn-close" onClick={() => setDetailsModalOpen(false)} />
              </div>

              {applySuccess && (
                <Alert variant="success" className="py-2 small">
                  <i className="bi bi-check-circle-fill me-2"></i>
                  Application submitted successfully! Your virtual CV is now available to recruiters.
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
                {viewingPosition.attributes.length === 0 ? (
                  <p className="text-muted small fst-italic">Standard profile only (Me attributes).</p>
                ) : (
                  <div className="list-group list-group-flush border rounded-3">
                    {viewingPosition.attributes.map(attr => (
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
                  {viewingPosition.tags.map((t, i) => (
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
                        'Submit Profile / Apply'
                      )}
                    </Button>
                  )
                ) : (
                  <span className="text-muted small">Sign in as a candidate to apply.</span>
                )}
              </div>
            </>
          ) : null}
        </div>
      </Modal>

      {/* MODAL: CREATE / EDIT POSITION */}
      <Modal show={modalOpen} onHide={() => !formSaving && setModalOpen(false)} size="lg" centered backdrop="static">
        <div className="p-4 bg-body rounded-4 shadow">
          <div className="d-flex justify-content-between align-items-center mb-3">
            <h5 className="fw-bold mb-0">
              {isEditing ? `Edit Position: ${currentPosition?.title}` : 'Post New Position'}
            </h5>
            <button type="button" className="btn-close" onClick={() => setModalOpen(false)} disabled={formSaving} />
          </div>

          {formError && (
            <Alert variant={concurrencyConflict ? 'warning' : 'danger'} className="py-2 small">
              <i className="bi bi-exclamation-triangle-fill me-2"></i>
              {formError}
              {concurrencyConflict && (
                <div className="mt-2 text-end">
                  <Button size="sm" variant="outline-dark" onClick={() => { setModalOpen(false); fetchPositions(); }}>
                    Reload Latest Data
                  </Button>
                </div>
              )}
            </Alert>
          )}

          <Form onSubmit={handleFormSubmit}>
            <Form.Group className="mb-3">
              <Form.Label className="small fw-semibold">Position Title *</Form.Label>
              <Form.Control
                type="text"
                placeholder="e.g. Junior Data Engineer @ Acme Corp"
                value={formData.title}
                onChange={(e) => setFormData(prev => ({ ...prev, title: e.target.value }))}
                required
                disabled={formSaving}
                className="form-control-pro"
              />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label className="small fw-semibold">Short Description *</Form.Label>
              <Form.Control
                as="textarea"
                rows={3}
                placeholder="Overview of expectations, team, responsibilities..."
                value={formData.shortDescription}
                onChange={(e) => setFormData(prev => ({ ...prev, shortDescription: e.target.value }))}
                required
                disabled={formSaving}
                className="form-control-pro"
              />
            </Form.Group>

            <Row className="g-3 mb-3">
              <Col md={6}>
                <Form.Label className="small fw-semibold">Max Candidate Projects in CV *</Form.Label>
                <Form.Control
                  type="number"
                  min={1}
                  max={10}
                  value={formData.maxProjects}
                  onChange={(e) => setFormData(prev => ({ ...prev, maxProjects: e.target.value }))}
                  required
                  disabled={formSaving}
                  className="form-control-pro"
                />
                <Form.Text className="text-muted small">
                  Limits the most recent matching projects included in virtual CV (default 3).
                </Form.Text>
              </Col>

              <Col md={6}>
                <Form.Label className="small fw-semibold">Technology Filter Tags</Form.Label>
                <CreatableSelect
                  isMulti
                  placeholder="Type technology (e.g. Python, SQL)..."
                  value={formData.tags.map(t => ({ label: t, value: t }))}
                  onChange={(selected) => setFormData(prev => ({
                    ...prev,
                    tags: selected ? selected.map(s => s.value) : []
                  }))}
                  isDisabled={formSaving}
                />
              </Col>
            </Row>

            {/* Required Attributes Multi-Select */}
            <Form.Group className="mb-4">
              <Form.Label className="small fw-semibold">Required Profile Attributes</Form.Label>
              <Select
                isMulti
                options={attributeSelectOptions}
                placeholder="Select attributes from the library (e.g. CAP, IELTS)..."
                value={attributeSelectOptions.filter(opt => formData.attributeIds.includes(opt.value))}
                onChange={(selected) => setFormData(prev => ({
                  ...prev,
                  attributeIds: selected ? selected.map(s => s.value) : []
                }))}
                isDisabled={formSaving}
              />
              <Form.Text className="text-muted small">
                These dynamic attributes will be required on the candidate's CV for this position.
              </Form.Text>
            </Form.Group>

            <div className="d-flex justify-content-end gap-2">
              <Button variant="outline-secondary" onClick={() => setModalOpen(false)} disabled={formSaving}>
                Cancel
              </Button>
              <Button type="submit" className="btn-primary-gradient px-4" disabled={formSaving}>
                {formSaving ? 'Saving...' : isEditing ? 'Update Position' : 'Create Position'}
              </Button>
            </div>
          </Form>
        </div>
      </Modal>

      {/* MODAL: DELETE CONFIRMATION */}
      <Modal show={deleteModalOpen} onHide={() => setDeleteModalOpen(false)} centered>
        <div className="p-4 bg-body rounded-4 shadow">
          <h5 className="fw-bold text-danger mb-2">Delete Position(s)?</h5>
          <p className="text-muted small">
            Are you sure you want to delete {selectedIds.length} position(s)? This will cascade delete associated applications.
          </p>
          <div className="d-flex justify-content-end gap-2 mt-4">
            <Button variant="outline-secondary" onClick={() => setDeleteModalOpen(false)}>
              Cancel
            </Button>
            <Button variant="danger" onClick={handleDeleteConfirm} disabled={deleting}>
              {deleting ? 'Deleting...' : 'Confirm Delete'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* MODAL: RECRUITER APPLICANTS REVIEW */}
      <ApplicantsModal
        show={applicantsModalOpen}
        onHide={() => {
          setApplicantsModalOpen(false)
          fetchPositions()
        }}
        position={selectedPositionForApplicants}
      />
    </Container>
  )
}
