import React, { useState, useEffect, useRef, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { Container, Row, Col, Card, Nav, Form, Button, Badge, Alert, Modal, Spinner, Table } from 'react-bootstrap'
import CreatableSelect from 'react-select/creatable'
import ReactMarkdown from 'react-markdown'
import { api } from '../services/api'
import { useAuth } from '../context/AuthContext'

export function ProfilePage() {
  const { id: paramId } = useParams()
  const navigate = useNavigate()
  const { user: authUser, isAdmin, isRecruiter } = useAuth()

  // Target User ID (either route param or logged-in user)
  const targetUserId = paramId || authUser?.id
  const isOwner = Boolean(authUser?.id && authUser.id === targetUserId)
  const canEdit = isOwner || isAdmin

  // Profile data state
  const [profile, setProfile] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  // Active Tab: 'me', 'info', 'projects', 'applications'
  const [activeTab, setActiveTab] = useState('me')

  // Attribute values state: Map of attributeDefinitionId -> { value, imagePublicId, attributeDefinitionId }
  const [attributeValues, setAttributeValues] = useState({})
  const [isDirty, setIsDirty] = useState(false)
  const [saving, setSaving] = useState(false)
  const [saveStatus, setSaveStatus] = useState('saved') // 'saved', 'unsaved', 'saving'

  // Library attributes picker modal (for Info tab)
  const [pickerModalOpen, setPickerModalOpen] = useState(false)
  const [libraryAttributes, setLibraryAttributes] = useState([])
  const [libraryLoading, setLibraryLoading] = useState(false)
  const [pickerSearch, setPickerSearch] = useState('')

  // Projects states & Toolbar selection (NO buttons in table rows!)
  const [selectedProjectIds, setSelectedProjectIds] = useState([])
  const [projectModalOpen, setProjectModalOpen] = useState(false)
  const [editingProject, setEditingProject] = useState(null)
  const [projectFormData, setProjectFormData] = useState({
    name: '',
    startDate: '',
    endDate: '',
    description: '',
    version: 1,
    tags: []
  })
  const [projectSaving, setProjectSaving] = useState(false)
  const [projectModalError, setProjectModalError] = useState(null)
  const [deleteProjectModalOpen, setDeleteProjectModalOpen] = useState(false)

  // Concurrency conflict modal
  const [conflictModalOpen, setConflictModalOpen] = useState(false)
  const [conflictMessage, setConflictMessage] = useState('')

  // Photo upload state
  const [uploadingPhoto, setUploadingPhoto] = useState(false)

  // Applications tab states & selection (NO buttons in table rows!)
  const [selectedAppCvIds, setSelectedAppCvIds] = useState([])
  const [withdrawModalOpen, setWithdrawModalOpen] = useState(false)
  const [withdrawing, setWithdrawing] = useState(false)
  const fileInputRef = useRef(null)

  // Auto-save timer ref
  const autoSaveTimerRef = useRef(null)

  // 1. Fetch Profile Data
  const fetchProfile = useCallback(async () => {
    if (!targetUserId) return
    setLoading(true)
    setError(null)
    try {
      const data = await api.get(`/api/profile/${targetUserId}`)
      setProfile(data)

      // Initialize attribute values map
      const initialMap = {}
      data.meAttributes.forEach(attr => {
        initialMap[attr.attributeDefinitionId] = {
          attributeDefinitionId: attr.attributeDefinitionId,
          value: attr.value || '',
          imagePublicId: attr.imagePublicId || null
        }
      })
      data.infoAttributes.forEach(attr => {
        initialMap[attr.attributeDefinitionId] = {
          attributeDefinitionId: attr.attributeDefinitionId,
          value: attr.value || '',
          imagePublicId: attr.imagePublicId || null
        }
      })
      setAttributeValues(initialMap)
      setIsDirty(false)
      setSaveStatus('saved')
    } catch (err) {
      setError(err.message || 'Failed to load candidate profile.')
    } finally {
      setLoading(false)
    }
  }, [targetUserId])

  useEffect(() => {
    fetchProfile()
  }, [fetchProfile])

  // 2. Save Profile (Optimistic Locking)
  const saveProfile = useCallback(async () => {
    if (!profile || !canEdit || !isDirty) return

    setSaving(true)
    setSaveStatus('saving')
    try {
      const attributesPayload = Object.values(attributeValues).map(item => ({
        attributeDefinitionId: item.attributeDefinitionId,
        value: item.value,
        imagePublicId: item.imagePublicId
      }))

      const res = await api.put(`/api/profile/${targetUserId}`, {
        version: profile.version,
        attributes: attributesPayload
      })

      // Update local version token
      setProfile(prev => ({ ...prev, version: res.version }))
      setIsDirty(false)
      setSaveStatus('saved')
    } catch (err) {
      if (err.status === 409) {
        setConflictMessage(err.data?.message || 'Your profile was modified in another window.')
        setConflictModalOpen(true)
      } else {
        alert(`Save failed: ${err.message}`)
      }
      setSaveStatus('unsaved')
    } finally {
      setSaving(false)
    }
  }, [profile, canEdit, isDirty, attributeValues, targetUserId])

  // 3. Auto-save engine (debounced 6 seconds)
  useEffect(() => {
    if (!isDirty || !canEdit) return

    setSaveStatus('unsaved')
    if (autoSaveTimerRef.current) {
      clearTimeout(autoSaveTimerRef.current)
    }

    autoSaveTimerRef.current = setTimeout(() => {
      saveProfile()
    }, 6000)

    return () => {
      if (autoSaveTimerRef.current) {
        clearTimeout(autoSaveTimerRef.current)
      }
    }
  }, [attributeValues, isDirty, canEdit, saveProfile])

  // Handle value change for an attribute
  const handleAttributeChange = (attributeDefinitionId, newValue, newImagePublicId = null) => {
    setAttributeValues(prev => ({
      ...prev,
      [attributeDefinitionId]: {
        attributeDefinitionId,
        value: newValue,
        imagePublicId: newImagePublicId ?? prev[attributeDefinitionId]?.imagePublicId ?? null
      }
    }))
    setIsDirty(true)
  }

  // Handle signed Cloudinary photo upload (Spec §6.3)
  const handlePhotoUpload = async (e) => {
    const file = e.target.files?.[0]
    if (!file) return

    // Find personal photo attribute ID
    const photoAttr = profile.meAttributes.find(a => a.name.toLowerCase().includes('photo'))
    if (!photoAttr) return

    setUploadingPhoto(true)
    try {
      const sigData = await api.get('/api/profile/upload-signature')

      if (sigData.isConfigured) {
        // Direct Signed Upload to Cloudinary CDN
        const formData = new FormData()
        formData.append('file', file)
        formData.append('api_key', sigData.apiKey)
        formData.append('timestamp', sigData.timestamp)
        formData.append('signature', sigData.signature)
        formData.append('folder', sigData.folder)

        const uploadRes = await fetch(
          `https://api.cloudinary.com/v1_1/${sigData.cloudName}/image/upload`,
          { method: 'POST', body: formData }
        )

        if (!uploadRes.ok) {
          throw new Error('Cloudinary upload rejected.')
        }

        const cloudRes = await uploadRes.json()
        handleAttributeChange(photoAttr.attributeDefinitionId, cloudRes.secure_url, cloudRes.public_id)
      } else {
        // Fallback local file reader for testing before live Cloudinary keys are supplied
        const reader = new FileReader()
        reader.onloadend = () => {
          handleAttributeChange(photoAttr.attributeDefinitionId, reader.result, 'local-dev-mock-id')
        }
        reader.readAsDataURL(file)
      }
    } catch (err) {
      alert(`Photo upload failed: ${err.message}`)
    } finally {
      setUploadingPhoto(false)
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  // Open Attribute Picker modal (Info tab)
  const handleOpenPicker = async () => {
    setPickerModalOpen(true)
    setLibraryLoading(true)
    try {
      const res = await api.get('/api/attributes?pageSize=100')
      // Exclude already added attributes
      const currentIds = Object.keys(attributeValues).map(Number)
      const available = (res.items || []).filter(a => !a.isBuiltIn && !currentIds.includes(a.id))
      setLibraryAttributes(available)
    } catch (err) {
      console.error('Failed to load library attributes:', err)
    } finally {
      setLibraryLoading(false)
    }
  }

  // Add attribute from library to profile
  const handleSelectAttributeFromLibrary = (attr) => {
    setProfile(prev => ({
      ...prev,
      infoAttributes: [...prev.infoAttributes, {
        attributeDefinitionId: attr.id,
        name: attr.name,
        dataType: attr.dataType,
        categoryName: attr.categoryName,
        value: '',
        imagePublicId: null,
        isBuiltIn: false,
        options: attr.options || []
      }]
    }))

    handleAttributeChange(attr.id, '', null)
    setPickerModalOpen(false)
  }

  // Remove custom attribute from profile
  const handleRemoveInfoAttribute = async (attrDefId) => {
    if (!window.confirm('Remove this attribute from your profile?')) return

    try {
      await api.delete(`/api/profile/${targetUserId}/attributes/${attrDefId}`)
      setProfile(prev => ({
        ...prev,
        infoAttributes: prev.infoAttributes.filter(a => a.attributeDefinitionId !== attrDefId)
      }))
      setAttributeValues(prev => {
        const copy = { ...prev }
        delete copy[attrDefId]
        return copy
      })
    } catch (err) {
      alert(`Failed to remove attribute: ${err.message}`)
    }
  }

  // Open Project Modal (New)
  const handleOpenNewProject = () => {
    setEditingProject(null)
    setProjectFormData({
      name: '',
      startDate: new Date().toISOString().substring(0, 10),
      endDate: '',
      description: '',
      version: 1,
      tags: []
    })
    setProjectModalError(null)
    setProjectModalOpen(true)
  }

  // Open Project Modal (Edit)
  const handleOpenEditProject = () => {
    if (selectedProjectIds.length !== 1) return
    const proj = profile.projects.find(p => p.id === selectedProjectIds[0])
    if (!proj) return

    setEditingProject(proj)
    setProjectFormData({
      name: proj.name,
      startDate: proj.startDate,
      endDate: proj.endDate || '',
      description: proj.description || '',
      version: proj.version,
      tags: proj.tags || []
    })
    setProjectModalError(null)
    setProjectModalOpen(true)
  }

  // Submit Project Form
  const handleProjectSubmit = async (e) => {
    e.preventDefault()
    setProjectModalError(null)
    setProjectSaving(true)
    try {
      if (editingProject) {
        const updated = await api.put(`/api/profile/${targetUserId}/projects/${editingProject.id}`, {
          name: projectFormData.name,
          startDate: projectFormData.startDate,
          endDate: projectFormData.endDate || null,
          description: projectFormData.description,
          version: projectFormData.version,
          tags: projectFormData.tags
        })

        setProfile(prev => ({
          ...prev,
          projects: prev.projects.map(p => p.id === updated.id ? updated : p)
        }))
      } else {
        const created = await api.post(`/api/profile/${targetUserId}/projects`, {
          name: projectFormData.name,
          startDate: projectFormData.startDate,
          endDate: projectFormData.endDate || null,
          description: projectFormData.description,
          tags: projectFormData.tags
        })

        setProfile(prev => ({
          ...prev,
          projects: [created, ...prev.projects]
        }))
      }

      setProjectModalOpen(false)
      setSelectedProjectIds([])
    } catch (err) {
      setProjectModalError(err.message || 'Failed to save project.')
    } finally {
      setProjectSaving(false)
    }
  }

  // Delete Project from Toolbar
  const handleDeleteProjects = async () => {
    try {
      for (const id of selectedProjectIds) {
        await api.delete(`/api/profile/${targetUserId}/projects/${id}`)
      }
      setProfile(prev => ({
        ...prev,
        projects: prev.projects.filter(p => !selectedProjectIds.includes(p.id))
      }))
      setSelectedProjectIds([])
      setDeleteProjectModalOpen(false)
    } catch (err) {
      alert(`Deletion error: ${err.message}`)
    }
  }

  if (loading) {
    return (
      <Container className="py-5 text-center">
        <Spinner animation="border" variant="primary" />
        <p className="mt-3 text-muted small">Loading candidate profile...</p>
      </Container>
    )
  }

  if (error || !profile) {
    return (
      <Container className="py-5">
        <Alert variant="danger" className="text-center p-4 rounded-4 shadow-sm">
          <i className="bi bi-person-x-fill text-danger fs-1 d-block mb-2"></i>
          <h4 className="fw-bold">Profile Unavailable</h4>
          <p className="text-muted mb-0">{error || 'User profile could not be loaded.'}</p>
        </Alert>
      </Container>
    )
  }

  // Extract Personal Photo
  const photoAttr = profile.meAttributes.find(a => a.name.toLowerCase().includes('photo'))
  const photoUrl = attributeValues[photoAttr?.attributeDefinitionId]?.value

  return (
    <Container className="py-5">
      {/* Profile Header Card */}
      <Card className="card-pro p-4 mb-4">
        <div className="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-3">
          <div className="d-flex align-items-center gap-4">
            {/* Avatar Photo with Upload Overlay */}
            <div className="position-relative">
              <div
                className="rounded-circle overflow-hidden shadow-sm d-flex align-items-center justify-content-center bg-subtle border"
                style={{ width: '84px', height: '84px' }}
              >
                {photoUrl ? (
                  <img
                    src={photoUrl}
                    alt="Candidate Avatar"
                    className="w-100 h-100 object-fit-cover"
                  />
                ) : (
                  <span className="fs-1 fw-bold text-muted">
                    {(profile.email?.[0] || 'C').toUpperCase()}
                  </span>
                )}
              </div>

              {canEdit && (
                <>
                  <input
                    type="file"
                    ref={fileInputRef}
                    onChange={handlePhotoUpload}
                    accept="image/*"
                    style={{ display: 'none' }}
                  />
                  <button
                    type="button"
                    className="btn btn-sm btn-primary rounded-circle position-absolute bottom-0 end-0 p-1 d-flex align-items-center justify-content-center shadow"
                    style={{ width: '28px', height: '28px' }}
                    onClick={() => fileInputRef.current?.click()}
                    disabled={uploadingPhoto}
                    title="Upload photo to Cloudinary"
                  >
                    {uploadingPhoto ? (
                      <Spinner size="sm" animation="border" style={{ width: '12px', height: '12px' }} />
                    ) : (
                      <i className="bi bi-camera-fill" style={{ fontSize: '12px' }}></i>
                    )}
                  </button>
                </>
              )}
            </div>

            <div>
              <div className="d-flex align-items-center gap-2 mb-1">
                <h4 className="fw-bold mb-0">
                  {attributeValues[profile.meAttributes.find(a => a.name.includes('First'))?.attributeDefinitionId]?.value || ''}{' '}
                  {attributeValues[profile.meAttributes.find(a => a.name.includes('Last'))?.attributeDefinitionId]?.value || profile.email}
                </h4>
                {profile.roles.map(r => (
                  <Badge
                    key={r}
                    bg={r === 'Administrator' ? 'danger' : r === 'Recruiter' ? 'primary' : 'success'}
                    pill
                    className="small"
                  >
                    {r}
                  </Badge>
                ))}
              </div>

              <div className="d-flex align-items-center gap-3 text-muted small">
                <span><i className="bi bi-envelope me-1"></i> {profile.email}</span>
                {attributeValues[profile.meAttributes.find(a => a.name.includes('Location'))?.attributeDefinitionId]?.value && (
                  <span>
                    <i className="bi bi-geo-alt me-1"></i>
                    {attributeValues[profile.meAttributes.find(a => a.name.includes('Location'))?.attributeDefinitionId]?.value}
                  </span>
                )}
                <span className="font-monospace text-muted">v{profile.version}</span>
              </div>
            </div>
          </div>

          {/* Auto-Save & Manual Save Controls */}
          {canEdit ? (
            <div className="d-flex align-items-center gap-3">
              <div className="d-flex align-items-center gap-2 small">
                {saveStatus === 'saved' && (
                  <span className="text-success d-flex align-items-center gap-1">
                    <i className="bi bi-check-circle-fill"></i> All changes saved
                  </span>
                )}
                {saveStatus === 'saving' && (
                  <span className="text-primary d-flex align-items-center gap-1">
                    <Spinner size="sm" animation="border" /> Auto-saving...
                  </span>
                )}
                {saveStatus === 'unsaved' && (
                  <span className="text-warning-emphasis d-flex align-items-center gap-1">
                    <i className="bi bi-clock"></i> Unsaved changes
                  </span>
                )}
              </div>

              <Button
                size="sm"
                className="btn-primary-gradient px-3"
                onClick={saveProfile}
                disabled={!isDirty || saving}
              >
                {saving ? 'Saving...' : 'Save Now'}
              </Button>
            </div>
          ) : (
            <Badge bg="secondary" className="px-3 py-2 small">
              <i className="bi bi-eye me-1"></i> Recruiter Read-Only View
            </Badge>
          )}
        </div>
      </Card>

      {/* Tabs Navigation */}
      <Nav variant="tabs" className="mb-4">
        <Nav.Item>
          <Nav.Link
            active={activeTab === 'me'}
            onClick={() => setActiveTab('me')}
            className="fw-semibold px-4"
          >
            <i className="bi bi-person me-2"></i> Me (Mandatory Profile)
          </Nav.Link>
        </Nav.Item>

        <Nav.Item>
          <Nav.Link
            active={activeTab === 'info'}
            onClick={() => setActiveTab('info')}
            className="fw-semibold px-4"
          >
            <i className="bi bi-card-checklist me-2"></i> Info (Skills &amp; Library)
          </Nav.Link>
        </Nav.Item>

        <Nav.Item>
          <Nav.Link
            active={activeTab === 'projects'}
            onClick={() => setActiveTab('projects')}
            className="fw-semibold px-4"
          >
            <i className="bi bi-code-slash me-2"></i> Projects ({profile.projects.length})
          </Nav.Link>
        </Nav.Item>

        <Nav.Item>
          <Nav.Link
            active={activeTab === 'applications'}
            onClick={() => setActiveTab('applications')}
            className="fw-semibold px-4"
          >
            <i className="bi bi-file-earmark-person me-2"></i> Applications ({profile.applications.length})
          </Nav.Link>
        </Nav.Item>
      </Nav>

      {/* TAB 1: ME TAB (Mandatory 4 Built-In Attributes) */}
      {activeTab === 'me' && (
        <Card className="card-pro p-4">
          <div className="mb-4">
            <h5 className="fw-bold mb-1">Core Identity Attributes</h5>
            <p className="text-muted small mb-0">
              The 4 mandatory attributes (First Name, Last Name, Location, Personal Photo) required on every Virtual CV.
            </p>
          </div>

          <Row className="g-4">
            {profile.meAttributes.map(attr => {
              const currentVal = attributeValues[attr.attributeDefinitionId]?.value || ''
              const isPhoto = attr.name.toLowerCase().includes('photo')

              return (
                <Col md={6} key={attr.attributeDefinitionId}>
                  <Form.Group>
                    <Form.Label className="small fw-semibold d-flex justify-content-between">
                      <span>{attr.name} *</span>
                      <Badge bg="secondary" pill style={{ fontSize: '10px' }}>Built-in</Badge>
                    </Form.Label>

                    {isPhoto ? (
                      <div className="d-flex align-items-center gap-3">
                        <Form.Control
                          type="text"
                          placeholder="https://res.cloudinary.com/..."
                          value={currentVal}
                          onChange={(e) => handleAttributeChange(attr.attributeDefinitionId, e.target.value)}
                          disabled={!canEdit}
                          className="form-control-pro"
                        />
                        {canEdit && (
                          <Button
                            variant="outline-primary"
                            size="sm"
                            onClick={() => fileInputRef.current?.click()}
                            disabled={uploadingPhoto}
                            style={{ whiteSpace: 'nowrap' }}
                          >
                            <i className="bi bi-upload me-1"></i> Upload
                          </Button>
                        )}
                      </div>
                    ) : (
                      <Form.Control
                        type="text"
                        placeholder={`Enter your ${attr.name.toLowerCase()}...`}
                        value={currentVal}
                        onChange={(e) => handleAttributeChange(attr.attributeDefinitionId, e.target.value)}
                        disabled={!canEdit}
                        className="form-control-pro"
                      />
                    )}
                  </Form.Group>
                </Col>
              )
            })}
          </Row>
        </Card>
      )}

      {/* TAB 2: INFO TAB (Custom Dynamic Attributes from Library) */}
      {activeTab === 'info' && (
        <div>
          <div className="d-flex justify-content-between align-items-center mb-4">
            <div>
              <h5 className="fw-bold mb-1">Skills &amp; Custom Attributes</h5>
              <p className="text-muted small mb-0">
                Enhance your profile by selecting dynamic attributes from the central catalog.
              </p>
            </div>
            {canEdit && (
              <Button className="btn-primary-gradient btn-sm px-3" onClick={handleOpenPicker}>
                <i className="bi bi-plus-lg me-1"></i> Add Attribute from Library
              </Button>
            )}
          </div>

          {profile.infoAttributes.length === 0 ? (
            <Card className="card-pro p-5 text-center text-muted">
              <i className="bi bi-tags fs-1 mb-2 text-dimmed"></i>
              <h6 className="fw-bold mb-1">No custom attributes added yet</h6>
              <p className="small mb-3">Add certifications, domain expertise, or language scores to your CV.</p>
              {canEdit && (
                <div>
                  <Button variant="outline-primary" size="sm" onClick={handleOpenPicker}>
                    Browse Attribute Library
                  </Button>
                </div>
              )}
            </Card>
          ) : (
            <Row className="g-4">
              {profile.infoAttributes.map(attr => {
                const currentVal = attributeValues[attr.attributeDefinitionId]?.value || ''

                return (
                  <Col md={6} key={attr.attributeDefinitionId}>
                    <Card className="card-pro p-3 h-100">
                      <div className="d-flex justify-content-between align-items-start mb-2">
                        <div>
                          <span className="fw-semibold d-block">{attr.name}</span>
                          <span className="badge bg-light text-dark border small me-2">
                            {attr.categoryName}
                          </span>
                          <span className="badge bg-primary-subtle text-primary border border-primary-subtle small">
                            {attr.dataType}
                          </span>
                        </div>
                        {canEdit && (
                          <button
                            type="button"
                            className="btn btn-sm btn-link text-danger p-0"
                            onClick={() => handleRemoveInfoAttribute(attr.attributeDefinitionId)}
                            title="Remove attribute from profile"
                          >
                            <i className="bi bi-x-circle fs-5"></i>
                          </button>
                        )}
                      </div>

                      {/* Type-Specific Input Controls */}
                      <div className="mt-2">
                        {attr.dataType === 'Boolean' ? (
                          <Form.Check
                            type="checkbox"
                            label="Yes / Certified / Active"
                            checked={currentVal === 'true' || currentVal === 'True'}
                            onChange={(e) => handleAttributeChange(attr.attributeDefinitionId, e.target.checked ? 'true' : 'false')}
                            disabled={!canEdit}
                          />
                        ) : attr.dataType === 'OneOfMany' ? (
                          <Form.Select
                            value={currentVal}
                            onChange={(e) => handleAttributeChange(attr.attributeDefinitionId, e.target.value)}
                            disabled={!canEdit}
                            className="form-control-pro form-control-sm"
                          >
                            <option value="">-- Select an option --</option>
                            {attr.options.map((opt, i) => (
                              <option key={i} value={opt}>{opt}</option>
                            ))}
                          </Form.Select>
                        ) : attr.dataType === 'Numeric' ? (
                          <Form.Control
                            type="number"
                            step="any"
                            placeholder="e.g. 7.5"
                            value={currentVal}
                            onChange={(e) => handleAttributeChange(attr.attributeDefinitionId, e.target.value)}
                            disabled={!canEdit}
                            className="form-control-pro form-control-sm"
                          />
                        ) : attr.dataType === 'Date' ? (
                          <Form.Control
                            type="date"
                            value={currentVal}
                            onChange={(e) => handleAttributeChange(attr.attributeDefinitionId, e.target.value)}
                            disabled={!canEdit}
                            className="form-control-pro form-control-sm"
                          />
                        ) : attr.dataType === 'Text' ? (
                          <Form.Control
                            as="textarea"
                            rows={3}
                            placeholder="Enter detailed description..."
                            value={currentVal}
                            onChange={(e) => handleAttributeChange(attr.attributeDefinitionId, e.target.value)}
                            disabled={!canEdit}
                            className="form-control-pro form-control-sm"
                          />
                        ) : (
                          <Form.Control
                            type="text"
                            placeholder="Enter value..."
                            value={currentVal}
                            onChange={(e) => handleAttributeChange(attr.attributeDefinitionId, e.target.value)}
                            disabled={!canEdit}
                            className="form-control-pro form-control-sm"
                          />
                        )}
                      </div>
                    </Card>
                  </Col>
                )
              })}
            </Row>
          )}
        </div>
      )}

      {/* TAB 3: PROJECTS TAB (Toolbar-driven table, NO buttons in table rows!) */}
      {activeTab === 'projects' && (
        <div>
          {/* Projects Table Toolbar */}
          <div className="table-toolbar rounded-top border d-flex justify-content-between align-items-center p-3 bg-card">
            <div className="d-flex align-items-center gap-2">
              {canEdit && (
                <>
                  <button
                    type="button"
                    className="btn btn-primary-gradient btn-sm px-3 d-flex align-items-center gap-2"
                    onClick={handleOpenNewProject}
                  >
                    <i className="bi bi-plus-lg"></i>
                    <span>New Project</span>
                  </button>

                  <button
                    type="button"
                    className="btn btn-outline-secondary btn-sm px-3 d-flex align-items-center gap-2"
                    onClick={handleOpenEditProject}
                    disabled={selectedProjectIds.length !== 1}
                  >
                    <i className="bi bi-pencil"></i>
                    <span>Edit</span>
                  </button>

                  <button
                    type="button"
                    className="btn btn-outline-danger btn-sm px-3 d-flex align-items-center gap-2"
                    onClick={() => setDeleteProjectModalOpen(true)}
                    disabled={selectedProjectIds.length === 0}
                  >
                    <i className="bi bi-trash3"></i>
                    <span>Delete</span>
                  </button>

                  {selectedProjectIds.length > 0 && (
                    <Badge bg="secondary" pill className="ms-2 px-2 py-1">
                      {selectedProjectIds.length} selected
                    </Badge>
                  )}
                </>
              )}
            </div>

            <div className="text-muted small">
              Portfolio Projects: <strong>{profile.projects.length}</strong>
            </div>
          </div>

          {/* Projects Table (Strictly NO buttons in table rows) */}
          <div className="table-responsive border border-top-0 rounded-bottom shadow-sm bg-card mb-4">
            <Table hover className="mb-0 align-middle">
              <thead className="table-light">
                <tr>
                  {canEdit && (
                    <th style={{ width: '42px' }} className="text-center">
                      <Form.Check
                        type="checkbox"
                        checked={profile.projects.length > 0 && selectedProjectIds.length === profile.projects.length}
                        onChange={(e) => {
                          if (e.target.checked) setSelectedProjectIds(profile.projects.map(p => p.id))
                          else setSelectedProjectIds([])
                        }}
                      />
                    </th>
                  )}
                  <th>Project Name</th>
                  <th>Timeline</th>
                  <th>Technology Tags</th>
                  <th>Description</th>
                </tr>
              </thead>
              <tbody>
                {profile.projects.length === 0 ? (
                  <tr>
                    <td colSpan={canEdit ? 5 : 4} className="text-center py-5 text-muted">
                      <i className="bi bi-code-slash fs-2 d-block mb-2 text-dimmed"></i>
                      <p className="mb-1 fw-semibold">No portfolio projects added yet</p>
                      <small>Add your featured open-source work, client builds, and applications.</small>
                    </td>
                  </tr>
                ) : (
                  profile.projects.map(proj => {
                    const isSelected = selectedProjectIds.includes(proj.id)
                    return (
                      <tr
                        key={proj.id}
                        className={isSelected ? 'table-primary bg-opacity-10' : ''}
                        onClick={() => {
                          if (canEdit) {
                            setSelectedProjectIds(prev =>
                              prev.includes(proj.id) ? prev.filter(id => id !== proj.id) : [...prev, proj.id]
                            )
                          }
                        }}
                        style={{ cursor: canEdit ? 'pointer' : 'default' }}
                      >
                        {canEdit && (
                          <td className="text-center" onClick={(e) => e.stopPropagation()}>
                            <Form.Check
                              type="checkbox"
                              checked={isSelected}
                              onChange={() => {
                                setSelectedProjectIds(prev =>
                                  prev.includes(proj.id) ? prev.filter(id => id !== proj.id) : [...prev, proj.id]
                                )
                              }}
                            />
                          </td>
                        )}
                        <td>
                          <span className="fw-semibold">{proj.name}</span>
                        </td>
                        <td>
                          <small className="text-muted">
                            {proj.startDate} &rarr; {proj.endDate || 'Present'}
                          </small>
                        </td>
                        <td>
                          <div className="d-flex gap-1 flex-wrap">
                            {proj.tags.map((tag, idx) => (
                              <span key={idx} className="badge bg-body text-primary border px-2 py-1 small">
                                #{tag}
                              </span>
                            ))}
                          </div>
                        </td>
                        <td>
                          <div className="text-muted small text-truncate" style={{ maxWidth: '340px' }}>
                            <ReactMarkdown>{proj.description}</ReactMarkdown>
                          </div>
                        </td>
                      </tr>
                    )
                  })
                )}
              </tbody>
            </Table>
          </div>
        </div>
      )}

      {/* TAB 4: APPLICATIONS TAB */}
      {activeTab === 'applications' && (
        <Card className="card-pro p-4">
          <div className="d-flex justify-content-between align-items-center mb-3">
            <div>
              <h5 className="fw-bold mb-1">Submitted Applications</h5>
              <p className="text-muted small mb-0">
                Track job openings where your profile was submitted as a virtual CV.
              </p>
            </div>
          </div>

          {/* STRICT UI COMPLIANCE TOOLBAR (Zero buttons inside rows!) */}
          <div className="table-toolbar rounded-top border d-flex justify-content-between align-items-center p-3 bg-card">
            <div className="d-flex align-items-center gap-2 flex-wrap">
              {canEdit && (
                <button
                  type="button"
                  className="btn btn-outline-danger btn-sm px-3 d-flex align-items-center gap-2"
                  onClick={() => setWithdrawModalOpen(true)}
                  disabled={selectedAppCvIds.length === 0}
                  title="Withdraw selected application"
                >
                  <i className="bi bi-x-circle"></i>
                  <span>Withdraw Application</span>
                </button>
              )}

              {selectedAppCvIds.length > 0 && (
                <Badge bg="secondary" pill className="ms-2 px-2 py-1">
                  {selectedAppCvIds.length} selected
                </Badge>
              )}
            </div>

            <div className="text-muted small">
              Total Applications: <strong>{profile.applications.length}</strong>
            </div>
          </div>

          {/* Applications Table (NO buttons inside rows!) */}
          <div className="table-responsive border border-top-0 rounded-bottom shadow-sm bg-card mb-2">
            <Table hover className="mb-0 align-middle">
              <thead className="table-light">
                <tr>
                  {canEdit && (
                    <th style={{ width: '42px' }} className="text-center">
                      <Form.Check
                        type="checkbox"
                        checked={profile.applications.length > 0 && selectedAppCvIds.length === profile.applications.length}
                        onChange={(e) => {
                          if (e.target.checked) setSelectedAppCvIds(profile.applications.map(a => a.cvId))
                          else setSelectedAppCvIds([])
                        }}
                        aria-label="Select all applications"
                      />
                    </th>
                  )}
                  <th>Position Title</th>
                  <th>Application Status</th>
                  <th className="text-end">Submitted At</th>
                </tr>
              </thead>
              <tbody>
                {profile.applications.length === 0 ? (
                  <tr>
                    <td colSpan={canEdit ? 4 : 3} className="text-center py-5 text-muted">
                      <i className="bi bi-file-earmark-text fs-2 d-block mb-2 text-dimmed"></i>
                      <p className="mb-1 fw-semibold">No applications submitted yet</p>
                      <small>Explore open vacancies on the Positions page and apply with one click.</small>
                    </td>
                  </tr>
                ) : (
                  profile.applications.map(app => {
                    const isSelected = selectedAppCvIds.includes(app.cvId)
                    return (
                      <tr
                        key={app.cvId}
                        className={isSelected ? 'table-primary bg-opacity-10' : ''}
                        onClick={() => {
                          if (canEdit) {
                            setSelectedAppCvIds(prev =>
                              prev.includes(app.cvId) ? prev.filter(id => id !== app.cvId) : [...prev, app.cvId]
                            )
                          }
                        }}
                        style={{ cursor: canEdit ? 'pointer' : 'default' }}
                      >
                        {canEdit && (
                          <td className="text-center" onClick={(e) => e.stopPropagation()}>
                            <Form.Check
                              type="checkbox"
                              checked={isSelected}
                              onChange={() => {
                                setSelectedAppCvIds(prev =>
                                  prev.includes(app.cvId) ? prev.filter(id => id !== app.cvId) : [...prev, app.cvId]
                                )
                              }}
                              aria-label={`Select application for ${app.positionTitle}`}
                            />
                          </td>
                        )}
                        <td>
                          <span className="fw-semibold text-primary">{app.positionTitle}</span>
                        </td>
                        <td>
                          <Badge bg={app.state === 'Published' ? 'success' : 'secondary'}>
                            {app.state}
                          </Badge>
                        </td>
                        <td className="text-end text-muted small">
                          {new Date(app.createdAt).toLocaleDateString()}
                        </td>
                      </tr>
                    )
                  })
                )}
              </tbody>
            </Table>
          </div>
        </Card>
      )}

      {/* MODAL: ATTRIBUTE LIBRARY PICKER */}
      <Modal show={pickerModalOpen} onHide={() => setPickerModalOpen(false)} size="lg" centered>
        <div className="p-4 bg-body rounded-4 shadow">
          <div className="d-flex justify-content-between align-items-center mb-3">
            <h5 className="fw-bold mb-0">Select Attribute from Library</h5>
            <button type="button" className="btn-close" onClick={() => setPickerModalOpen(false)} />
          </div>

          <Form.Control
            type="text"
            placeholder="Search attributes by name..."
            value={pickerSearch}
            onChange={(e) => setPickerSearch(e.target.value)}
            className="mb-3 form-control-pro"
          />

          {libraryLoading ? (
            <div className="text-center py-4">
              <Spinner size="sm" animation="border" variant="primary" />
            </div>
          ) : (
            <div className="list-group list-group-flush border rounded-3" style={{ maxHeight: '350px', overflowY: 'auto' }}>
              {libraryAttributes
                .filter(a => a.name.toLowerCase().includes(pickerSearch.toLowerCase()))
                .map(attr => (
                  <button
                    key={attr.id}
                    type="button"
                    className="list-group-item list-group-item-action d-flex justify-content-between align-items-center py-2"
                    onClick={() => handleSelectAttributeFromLibrary(attr)}
                  >
                    <div>
                      <span className="fw-semibold me-2">{attr.name}</span>
                      <span className="badge bg-light text-dark border small me-1">{attr.categoryName}</span>
                      <span className="badge bg-primary-subtle text-primary border border-primary-subtle small">{attr.dataType}</span>
                    </div>
                    <i className="bi bi-plus-circle text-primary fs-5"></i>
                  </button>
                ))}
            </div>
          )}
        </div>
      </Modal>

      {/* MODAL: ADD / EDIT PROJECT */}
      <Modal show={projectModalOpen} onHide={() => !projectSaving && setProjectModalOpen(false)} size="lg" centered backdrop="static">
        <div className="p-4 bg-body rounded-4 shadow">
          <div className="d-flex justify-content-between align-items-center mb-3">
            <h5 className="fw-bold mb-0">
              {editingProject ? 'Edit Project' : 'Add Project to Portfolio'}
            </h5>
            <button type="button" className="btn-close" onClick={() => setProjectModalOpen(false)} disabled={projectSaving} />
          </div>

          {projectModalError && (
            <Alert variant="danger" className="py-2 small">
              <i className="bi bi-exclamation-triangle-fill me-2"></i>
              {projectModalError}
            </Alert>
          )}

          <Form onSubmit={handleProjectSubmit}>
            <Form.Group className="mb-3">
              <Form.Label className="small fw-semibold">Project Name *</Form.Label>
              <Form.Control
                type="text"
                placeholder="e.g. Distributed Task Scheduler"
                value={projectFormData.name}
                onChange={(e) => setProjectFormData(prev => ({ ...prev, name: e.target.value }))}
                required
                disabled={projectSaving}
                className="form-control-pro"
              />
            </Form.Group>

            <Row className="g-3 mb-3">
              <Col md={6}>
                <Form.Label className="small fw-semibold">Start Date *</Form.Label>
                <Form.Control
                  type="date"
                  value={projectFormData.startDate}
                  onChange={(e) => setProjectFormData(prev => ({ ...prev, startDate: e.target.value }))}
                  required
                  disabled={projectSaving}
                  className="form-control-pro"
                />
              </Col>
              <Col md={6}>
                <Form.Label className="small fw-semibold">End Date (leave empty if current)</Form.Label>
                <Form.Control
                  type="date"
                  value={projectFormData.endDate}
                  onChange={(e) => setProjectFormData(prev => ({ ...prev, endDate: e.target.value }))}
                  disabled={projectSaving}
                  className="form-control-pro"
                />
              </Col>
            </Row>

            {/* Tags with react-select Creatable (Created in DB on first use) */}
            <Form.Group className="mb-3">
              <Form.Label className="small fw-semibold">Technology Tags</Form.Label>
              <CreatableSelect
                isMulti
                placeholder="Type and press enter (e.g. React, Docker, Postgres)..."
                value={projectFormData.tags.map(t => ({ label: t, value: t }))}
                onChange={(selected) => setProjectFormData(prev => ({
                  ...prev,
                  tags: selected ? selected.map(s => s.value) : []
                }))}
                isDisabled={projectSaving}
              />
              <Form.Text className="text-muted small">
                Type a technology keyword and press Enter to add.
              </Form.Text>
            </Form.Group>

            {/* Markdown Description */}
            <Form.Group className="mb-4">
              <Form.Label className="small fw-semibold">Markdown Description</Form.Label>
              <Form.Control
                as="textarea"
                rows={4}
                placeholder="Describe key responsibilities, architecture, outcomes in Markdown..."
                value={projectFormData.description}
                onChange={(e) => setProjectFormData(prev => ({ ...prev, description: e.target.value }))}
                disabled={projectSaving}
                className="form-control-pro"
              />
            </Form.Group>

            <div className="d-flex justify-content-end gap-2">
              <Button variant="outline-secondary" onClick={() => setProjectModalOpen(false)} disabled={projectSaving}>
                Cancel
              </Button>
              <Button type="submit" className="btn-primary-gradient px-4" disabled={projectSaving}>
                {projectSaving ? 'Saving...' : editingProject ? 'Update Project' : 'Add Project'}
              </Button>
            </div>
          </Form>
        </div>
      </Modal>

      {/* MODAL: DELETE PROJECTS CONFIRMATION */}
      <Modal show={deleteProjectModalOpen} onHide={() => setDeleteProjectModalOpen(false)} centered>
        <div className="p-4 bg-body rounded-4 shadow">
          <h5 className="fw-bold text-danger mb-2">Delete Selected Project(s)?</h5>
          <p className="text-muted small">
            Are you sure you want to delete {selectedProjectIds.length} project(s)? This action cannot be undone.
          </p>
          <div className="d-flex justify-content-end gap-2 mt-4">
            <Button variant="outline-secondary" onClick={() => setDeleteProjectModalOpen(false)}>
              Cancel
            </Button>
            <Button variant="danger" onClick={handleDeleteProjects}>
              Confirm Delete
            </Button>
          </div>
        </div>
      </Modal>

      {/* MODAL: OPTIMISTIC LOCKING CONFLICT */}
      <Modal show={conflictModalOpen} onHide={() => setConflictModalOpen(false)} centered backdrop="static">
        <div className="p-4 bg-body rounded-4 shadow text-center">
          <i className="bi bi-arrow-repeat text-warning fs-1 d-block mb-2"></i>
          <h5 className="fw-bold mb-2">Profile Modified Elsewhere</h5>
          <p className="text-muted small mb-4">{conflictMessage}</p>
          <Button
            className="btn-primary-gradient px-4"
            onClick={() => {
              setConflictModalOpen(false)
              fetchProfile()
            }}
          >
            Reload Latest Changes
          </Button>
        </div>
      </Modal>

      {/* MODAL: WITHDRAW APPLICATION CONFIRMATION */}
      <Modal show={withdrawModalOpen} onHide={() => setWithdrawModalOpen(false)} centered>
        <div className="p-4 bg-body rounded-4 shadow">
          <h5 className="fw-bold text-danger mb-2">Withdraw Application?</h5>
          <p className="text-muted small">
            Are you sure you want to withdraw {selectedAppCvIds.length} application(s)? Your virtual CV will no longer be visible to recruiters for this opening.
          </p>
          <div className="d-flex justify-content-end gap-2 mt-4">
            <Button variant="outline-secondary" onClick={() => setWithdrawModalOpen(false)} disabled={withdrawing}>
              Cancel
            </Button>
            <Button
              variant="danger"
              disabled={withdrawing}
              onClick={async () => {
                setWithdrawing(true)
                try {
                  for (const cvId of selectedAppCvIds) {
                    await api.delete(`/api/profile/applications/${cvId}`)
                  }
                  setProfile(prev => ({
                    ...prev,
                    applications: prev.applications.filter(a => !selectedAppCvIds.includes(a.cvId))
                  }))
                  setSelectedAppCvIds([])
                  setWithdrawModalOpen(false)
                } catch (err) {
                  alert(`Withdrawal failed: ${err.message}`)
                } finally {
                  setWithdrawing(false)
                }
              }}
            >
              {withdrawing ? 'Withdrawing...' : 'Confirm Withdrawal'}
            </Button>
          </div>
        </div>
      </Modal>
    </Container>
  )
}
