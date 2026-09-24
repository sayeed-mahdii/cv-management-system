import React, { useState, useEffect, useCallback } from 'react'
import { Container, Table, Badge, Form, Alert, Modal, Button, Spinner, Dropdown } from 'react-bootstrap'
import { api } from '../services/api'
import { useAuth } from '../context/AuthContext'

const DATA_TYPES = [
  { value: 'String', label: 'Single-line String' },
  { value: 'Text', label: 'Multi-line Text' },
  { value: 'Numeric', label: 'Numeric (e.g. Years, Score)' },
  { value: 'Date', label: 'Single Date' },
  { value: 'Period', label: 'Period (Start & End Dates)' },
  { value: 'Boolean', label: 'Yes/No (Boolean Checkbox)' },
  { value: 'Image', label: 'Image URL / Cloudinary' },
  { value: 'OneOfMany', label: 'One of many (Drop-down choices)' }
]

export function AttributesPage() {
  const { isRecruiter, isAdmin } = useAuth()

  // Data states
  const [attributes, setAttributes] = useState([])
  const [categories, setCategories] = useState([])
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(1)
  const [page, setPage] = useState(1)
  const [pageSize] = useState(10)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  // Filters
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [selectedCategory, setSelectedCategory] = useState(0)
  const [recentlyUsedOnly, setRecentlyUsedOnly] = useState(false)

  // Selection states (for Toolbar operations - NO buttons in table rows!)
  const [selectedIds, setSelectedIds] = useState([])

  // Modal states
  const [modalOpen, setModalOpen] = useState(false)
  const [isEditing, setIsEditing] = useState(false)
  const [currentAttribute, setCurrentAttribute] = useState(null)
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    dataType: 'String',
    categoryId: 1,
    version: 1,
    options: []
  })
  const [newOptionInput, setNewOptionInput] = useState('')
  const [formSaving, setFormSaving] = useState(false)
  const [formError, setFormError] = useState(null)
  const [concurrencyConflict, setConcurrencyConflict] = useState(false)

  // Delete confirmation modal
  const [deleteModalOpen, setDeleteModalOpen] = useState(false)
  const [deleting, setDeleting] = useState(false)

  // Debounce search input (350ms)
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(search)
      setPage(1)
    }, 350)
    return () => clearTimeout(handler)
  }, [search])

  // Fetch categories on mount
  useEffect(() => {
    api.get('/api/categories')
      .then(data => setCategories(data || []))
      .catch(err => console.error('Failed to load categories:', err))
  }, [])

  // Fetch attributes
  const fetchAttributes = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: pageSize.toString()
      })
      if (debouncedSearch) params.append('search', debouncedSearch)
      if (selectedCategory > 0) params.append('categoryId', selectedCategory.toString())
      if (recentlyUsedOnly) params.append('recentlyUsedOnly', 'true')

      const res = await api.get(`/api/attributes?${params.toString()}`)
      setAttributes(res.items || [])
      setTotalCount(res.totalCount || 0)
      setTotalPages(res.totalPages || 1)
      setSelectedIds([]) // clear selection when data reloads
    } catch (err) {
      setError(err.message || 'Failed to fetch attributes.')
    } finally {
      setLoading(false)
    }
  }, [page, pageSize, debouncedSearch, selectedCategory, recentlyUsedOnly])

  useEffect(() => {
    fetchAttributes()
  }, [fetchAttributes])

  // Selection handlers
  const handleSelectAll = (e) => {
    if (e.target.checked) {
      // Don't select built-in attributes for bulk delete/edit if recruiter
      const selectable = attributes.map(a => a.id)
      setSelectedIds(selectable)
    } else {
      setSelectedIds([])
    }
  }

  const handleSelectRow = (id) => {
    setSelectedIds(prev =>
      prev.includes(id) ? prev.filter(item => item !== id) : [...prev, id]
    )
  }

  // Open Create Modal
  const handleOpenCreate = () => {
    setIsEditing(false)
    setCurrentAttribute(null)
    setFormData({
      name: '',
      description: '',
      dataType: 'String',
      categoryId: categories[0]?.id || 1,
      version: 1,
      options: []
    })
    setNewOptionInput('')
    setFormError(null)
    setConcurrencyConflict(false)
    setModalOpen(true)
  }

  // Open Edit Modal (Single selected row from Toolbar)
  const handleOpenEdit = () => {
    if (selectedIds.length !== 1) return
    const idToEdit = selectedIds[0]
    const attr = attributes.find(a => a.id === idToEdit)
    if (!attr) return

    if (attr.isBuiltIn && !isAdmin) {
      alert('Built-in attributes cannot be modified.')
      return
    }

    setIsEditing(true)
    setCurrentAttribute(attr)
    setFormData({
      name: attr.name,
      description: attr.description || '',
      dataType: attr.dataType,
      categoryId: attr.categoryId,
      version: attr.version,
      options: [...(attr.options || [])]
    })
    setNewOptionInput('')
    setFormError(null)
    setConcurrencyConflict(false)
    setModalOpen(true)
  }

  // Add option chip for OneOfMany
  const handleAddOption = (e) => {
    e?.preventDefault()
    const val = newOptionInput.trim()
    if (!val) return
    if (formData.options.includes(val)) {
      setFormError(`Option "${val}" is already added.`)
      return
    }
    setFormData(prev => ({ ...prev, options: [...prev.options, val] }))
    setNewOptionInput('')
    setFormError(null)
  }

  const handleRemoveOption = (optToRemove) => {
    setFormData(prev => ({
      ...prev,
      options: prev.options.filter(opt => opt !== optToRemove)
    }))
  }

  // Submit Create or Edit Form
  const handleFormSubmit = async (e) => {
    e.preventDefault()
    setFormError(null)
    setConcurrencyConflict(false)

    if (formData.dataType === 'OneOfMany' && (!formData.options || formData.options.length === 0)) {
      setFormError('At least one choice option is required for "One of many" attributes.')
      return
    }

    setFormSaving(true)
    try {
      if (isEditing) {
        await api.put(`/api/attributes/${currentAttribute.id}`, {
          name: formData.name.trim(),
          description: formData.description.trim(),
          categoryId: parseInt(formData.categoryId, 10),
          version: formData.version,
          options: formData.dataType === 'OneOfMany' ? formData.options : null
        })
      } else {
        await api.post('/api/attributes', {
          name: formData.name.trim(),
          description: formData.description.trim(),
          dataType: formData.dataType,
          categoryId: parseInt(formData.categoryId, 10),
          options: formData.dataType === 'OneOfMany' ? formData.options : null
        })
      }

      setModalOpen(false)
      fetchAttributes()
    } catch (err) {
      if (err.status === 409) {
        setConcurrencyConflict(true)
        setFormError('Concurrency conflict: This attribute was modified by another user while you were editing it.')
      } else {
        setFormError(err.message || 'Operation failed. Please check form values.')
      }
    } finally {
      setFormSaving(false)
    }
  }

  // Delete Selected Attributes from Toolbar
  const handleDeleteConfirm = async () => {
    setDeleting(true)
    try {
      for (const id of selectedIds) {
        await api.delete(`/api/attributes/${id}`)
      }
      setDeleteModalOpen(false)
      setSelectedIds([])
      fetchAttributes()
    } catch (err) {
      alert(`Deletion error: ${err.message}`)
    } finally {
      setDeleting(false)
    }
  }

  // Inspect selected attribute for Toolbar details
  const singleSelectedAttr = selectedIds.length === 1
    ? attributes.find(a => a.id === selectedIds[0])
    : null

  const isBuiltInSelected = selectedIds.some(id => {
    const attr = attributes.find(a => a.id === id)
    return attr?.isBuiltIn
  })

  return (
    <Container className="py-5">
      {/* Page Header */}
      <div className="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center mb-4 gap-3">
        <div>
          <div className="d-flex align-items-center gap-2 mb-1">
            <h3 className="fw-bold mb-0">Attribute Library</h3>
            <Badge bg="primary" pill className="px-2 py-1 small">
              EAV Engine &middot; Spec §4.3
            </Badge>
          </div>
          <p className="text-muted small mb-0">
            Define dynamic attributes for candidate profiles, virtual CVs, and position eligibility criteria.
          </p>
        </div>
      </div>

      {error && (
        <Alert variant="danger" className="mb-4">
          <i className="bi bi-exclamation-triangle-fill me-2"></i>
          {error}
        </Alert>
      )}

      {/* Filter and Search Bar */}
      <div className="card-pro p-3 mb-4">
        <div className="row g-3 align-items-center">
          {/* Prefix Search Box */}
          <div className="col-md-5">
            <div className="input-group">
              <span className="input-group-text bg-body border-end-0">
                <i className="bi bi-search text-muted"></i>
              </span>
              <Form.Control
                type="text"
                placeholder="Search by prefix (e.g. 'CA' finds CAP)..."
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

          {/* Category Filter Pills */}
          <div className="col-md-5 d-flex gap-2 flex-wrap">
            <button
              type="button"
              className={`btn btn-sm rounded-pill px-3 ${
                selectedCategory === 0 ? 'btn-primary' : 'btn-outline-secondary'
              }`}
              onClick={() => { setSelectedCategory(0); setPage(1); }}
            >
              All Categories
            </button>
            {categories.map(c => (
              <button
                key={c.id}
                type="button"
                className={`btn btn-sm rounded-pill px-3 ${
                  selectedCategory === c.id ? 'btn-primary' : 'btn-outline-secondary'
                }`}
                onClick={() => { setSelectedCategory(c.id); setPage(1); }}
              >
                {c.name}
              </button>
            ))}
          </div>

          {/* Recently Used Filter Toggle */}
          <div className="col-md-2 text-md-end">
            <Form.Check
              type="switch"
              id="recent-switch"
              label="Recently used"
              checked={recentlyUsedOnly}
              onChange={(e) => { setRecentlyUsedOnly(e.target.checked); setPage(1); }}
              className="small fw-semibold text-muted d-inline-block"
            />
          </div>
        </div>
      </div>

      {/* STRICT UI COMPLIANCE TOOLBAR (Zero buttons inside rows!) */}
      <div className="table-toolbar rounded-top border d-flex justify-content-between align-items-center p-3 bg-card">
        <div className="d-flex align-items-center gap-2">
          {/* Create Button (Always available for Recruiters/Admins) */}
          {(isRecruiter || isAdmin) && (
            <button
              type="button"
              className="btn btn-primary-gradient btn-sm px-3 d-flex align-items-center gap-2"
              onClick={handleOpenCreate}
            >
              <i className="bi bi-plus-lg"></i>
              <span>New Attribute</span>
            </button>
          )}

          {/* Edit Button (Enabled ONLY when exactly 1 row is selected) */}
          {(isRecruiter || isAdmin) && (
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm px-3 d-flex align-items-center gap-2"
              onClick={handleOpenEdit}
              disabled={selectedIds.length !== 1 || (singleSelectedAttr?.isBuiltIn && !isAdmin)}
              title={
                selectedIds.length !== 1
                  ? 'Select exactly one attribute to edit'
                  : singleSelectedAttr?.isBuiltIn
                  ? 'Built-in attributes cannot be edited'
                  : 'Edit selected attribute'
              }
            >
              <i className="bi bi-pencil"></i>
              <span>Edit</span>
            </button>
          )}

          {/* Delete Button (Enabled when 1 or more rows selected) */}
          {(isRecruiter || isAdmin) && (
            <button
              type="button"
              className="btn btn-outline-danger btn-sm px-3 d-flex align-items-center gap-2"
              onClick={() => setDeleteModalOpen(true)}
              disabled={selectedIds.length === 0 || isBuiltInSelected}
              title={
                selectedIds.length === 0
                  ? 'Select attributes to delete'
                  : isBuiltInSelected
                  ? 'Selected set contains built-in attributes which cannot be deleted'
                  : 'Delete selected attributes'
              }
            >
              <i className="bi bi-trash3"></i>
              <span>Delete</span>
            </button>
          )}

          {/* Selection Counter */}
          {selectedIds.length > 0 && (
            <Badge bg="secondary" pill className="ms-2 px-2 py-1">
              {selectedIds.length} selected
            </Badge>
          )}
        </div>

        <div className="text-muted small">
          Showing <strong>{attributes.length}</strong> of <strong>{totalCount}</strong> attributes
        </div>
      </div>

      {/* Attributes Table (Strictly NO buttons in table rows) */}
      <div className="table-responsive border border-top-0 rounded-bottom shadow-sm bg-card mb-4">
        <Table hover className="mb-0 align-middle">
          <thead className="table-light">
            <tr>
              <th style={{ width: '42px' }} className="text-center">
                <Form.Check
                  type="checkbox"
                  checked={attributes.length > 0 && selectedIds.length === attributes.length}
                  onChange={handleSelectAll}
                  aria-label="Select all attributes"
                />
              </th>
              <th>Attribute Name</th>
              <th>Category</th>
              <th>Data Type</th>
              <th>Choices / Description</th>
              <th className="text-center" style={{ width: '90px' }}>Version</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={6} className="text-center py-5">
                  <Spinner animation="border" variant="primary" />
                  <p className="mt-2 text-muted small mb-0">Loading attribute definitions...</p>
                </td>
              </tr>
            ) : attributes.length === 0 ? (
              <tr>
                <td colSpan={6} className="text-center py-5 text-muted">
                  <i className="bi bi-inbox fs-2 d-block mb-2 text-dimmed"></i>
                  <p className="mb-1 fw-semibold">No attributes found</p>
                  <small>
                    {debouncedSearch
                      ? `No attributes match prefix "${debouncedSearch}". Try another search term.`
                      : 'Get started by creating your first dynamic attribute.'}
                  </small>
                </td>
              </tr>
            ) : (
              attributes.map((attr) => {
                const isSelected = selectedIds.includes(attr.id)
                return (
                  <tr
                    key={attr.id}
                    className={isSelected ? 'table-primary bg-opacity-10' : ''}
                    onClick={() => handleSelectRow(attr.id)}
                    style={{ cursor: 'pointer' }}
                  >
                    <td className="text-center" onClick={(e) => e.stopPropagation()}>
                      <Form.Check
                        type="checkbox"
                        checked={isSelected}
                        onChange={() => handleSelectRow(attr.id)}
                        aria-label={`Select ${attr.name}`}
                      />
                    </td>
                    <td>
                      <div className="d-flex align-items-center gap-2">
                        <span className="fw-semibold">{attr.name}</span>
                        {attr.isBuiltIn && (
                          <Badge bg="secondary" pill style={{ fontSize: '10px' }}>
                            Built-in
                          </Badge>
                        )}
                        {attr.lastUsedAt && (
                          <i
                            className="bi bi-clock-history text-muted"
                            title={`Last used: ${new Date(attr.lastUsedAt).toLocaleDateString()}`}
                            style={{ fontSize: '12px' }}
                          />
                        )}
                      </div>
                      {attr.description && (
                        <small className="text-muted text-truncate d-block" style={{ maxWidth: '280px' }}>
                          {attr.description}
                        </small>
                      )}
                    </td>
                    <td>
                      <Badge bg="light" text="dark" className="border">
                        {attr.categoryName || 'General'}
                      </Badge>
                    </td>
                    <td>
                      <span className="badge bg-primary-subtle text-primary border border-primary-subtle">
                        {attr.dataType}
                      </span>
                    </td>
                    <td>
                      {attr.dataType === 'OneOfMany' && attr.options?.length > 0 ? (
                        <div className="d-flex gap-1 flex-wrap" style={{ maxWidth: '320px' }}>
                          {attr.options.map((opt, idx) => (
                            <span
                              key={idx}
                              className="badge bg-body text-secondary border px-2 py-1"
                              style={{ fontSize: '11px' }}
                            >
                              {opt}
                            </span>
                          ))}
                        </div>
                      ) : (
                        <span className="text-muted small fst-italic">
                          {attr.description || '—'}
                        </span>
                      )}
                    </td>
                    <td className="text-center">
                      <span className="font-monospace text-muted small">
                        v{attr.version}
                      </span>
                    </td>
                  </tr>
                )
              })
            )}
          </tbody>
        </Table>
      </div>

      {/* Pagination Controls */}
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

      {/* CREATE / EDIT MODAL */}
      <Modal show={modalOpen} onHide={() => !formSaving && setModalOpen(false)} centered backdrop="static">
        <div className="p-4 bg-body rounded-4 shadow">
          <div className="d-flex justify-content-between align-items-center mb-3">
            <div>
              <h4 className="fw-bold mb-1">
                {isEditing ? `Edit Attribute: ${currentAttribute?.name}` : 'Create New Attribute'}
              </h4>
              <p className="text-muted small mb-0">
                {isEditing
                  ? `Optimistic lock token: Version ${formData.version}`
                  : 'Add a reusable attribute definition to the system catalog.'}
              </p>
            </div>
            <button
              type="button"
              className="btn-close"
              onClick={() => setModalOpen(false)}
              disabled={formSaving}
            />
          </div>

          {formError && (
            <Alert variant={concurrencyConflict ? 'warning' : 'danger'} className="py-2 small">
              <i className="bi bi-exclamation-triangle-fill me-2"></i>
              {formError}
              {concurrencyConflict && (
                <div className="mt-2 text-end">
                  <Button
                    size="sm"
                    variant="outline-dark"
                    onClick={() => {
                      setModalOpen(false)
                      fetchAttributes()
                    }}
                  >
                    Reload Latest Data
                  </Button>
                </div>
              )}
            </Alert>
          )}

          <Form onSubmit={handleFormSubmit}>
            {/* Attribute Name */}
            <Form.Group className="mb-3">
              <Form.Label className="small fw-semibold">Attribute Name *</Form.Label>
              <Form.Control
                type="text"
                placeholder="e.g. CAP, IELTS Score, Years in React"
                value={formData.name}
                onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
                required
                disabled={formSaving}
                className="form-control-pro"
              />
              <Form.Text className="text-muted small">
                Must be globally unique (case-insensitive).
              </Form.Text>
            </Form.Group>

            {/* Category */}
            <Form.Group className="mb-3">
              <Form.Label className="small fw-semibold">Category *</Form.Label>
              <Form.Select
                value={formData.categoryId}
                onChange={(e) => setFormData(prev => ({ ...prev, categoryId: e.target.value }))}
                required
                disabled={formSaving}
                className="form-control-pro"
              >
                {categories.map(c => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </Form.Select>
            </Form.Group>

            {/* Data Type (Locked if editing existing attribute) */}
            <Form.Group className="mb-3">
              <Form.Label className="small fw-semibold">Data Type *</Form.Label>
              <Form.Select
                value={formData.dataType}
                onChange={(e) => setFormData(prev => ({ ...prev, dataType: e.target.value }))}
                required
                disabled={isEditing || formSaving}
                className="form-control-pro"
              >
                {DATA_TYPES.map(t => (
                  <option key={t.value} value={t.value}>
                    {t.label}
                  </option>
                ))}
              </Form.Select>
              {isEditing && (
                <Form.Text className="text-muted small">
                  Data type cannot be changed after creation to maintain EAV integrity.
                </Form.Text>
              )}
            </Form.Group>

            {/* One of many (Dynamic Options Builder) */}
            {formData.dataType === 'OneOfMany' && (
              <div className="p-3 mb-3 rounded-3 border bg-subtle">
                <Form.Label className="small fw-semibold mb-1">
                  Choice Options * (e.g. Essentials, Pro, Expert)
                </Form.Label>
                <div className="d-flex gap-2 mb-2">
                  <Form.Control
                    type="text"
                    placeholder="Enter an option label..."
                    value={newOptionInput}
                    onChange={(e) => setNewOptionInput(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') {
                        e.preventDefault()
                        handleAddOption()
                      }
                    }}
                    disabled={formSaving}
                    className="form-control-pro form-control-sm"
                  />
                  <Button
                    type="button"
                    size="sm"
                    variant="outline-primary"
                    onClick={handleAddOption}
                    disabled={formSaving || !newOptionInput.trim()}
                  >
                    Add
                  </Button>
                </div>

                {/* Chips of added options */}
                <div className="d-flex gap-1 flex-wrap mt-2">
                  {formData.options.length === 0 ? (
                    <small className="text-muted fst-italic">No options added yet. Type above and click Add.</small>
                  ) : (
                    formData.options.map((opt, i) => (
                      <span key={i} className="badge bg-primary text-white d-inline-flex align-items-center gap-2 py-1 px-2">
                        {opt}
                        <button
                          type="button"
                          className="btn-close btn-close-white"
                          style={{ fontSize: '8px' }}
                          onClick={() => handleRemoveOption(opt)}
                          disabled={formSaving}
                          aria-label={`Remove option ${opt}`}
                        />
                      </span>
                    ))
                  )}
                </div>
              </div>
            )}

            {/* Description */}
            <Form.Group className="mb-4">
              <Form.Label className="small fw-semibold">Description</Form.Label>
              <Form.Control
                as="textarea"
                rows={2}
                placeholder="Brief explanation for candidates and recruiters..."
                value={formData.description}
                onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
                disabled={formSaving}
                className="form-control-pro"
              />
            </Form.Group>

            {/* Action Buttons */}
            <div className="d-flex justify-content-end gap-2">
              <Button
                variant="outline-secondary"
                onClick={() => setModalOpen(false)}
                disabled={formSaving}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                className="btn-primary-gradient px-4"
                disabled={formSaving}
              >
                {formSaving ? (
                  <>
                    <Spinner size="sm" animation="border" className="me-2" />
                    Saving...
                  </>
                ) : isEditing ? (
                  'Update Attribute'
                ) : (
                  'Create Attribute'
                )}
              </Button>
            </div>
          </Form>
        </div>
      </Modal>

      {/* BULK / SINGLE DELETE CONFIRMATION MODAL */}
      <Modal show={deleteModalOpen} onHide={() => !deleting && setDeleteModalOpen(false)} centered>
        <div className="p-4 bg-body rounded-4 shadow">
          <div className="d-flex align-items-center gap-3 text-danger mb-3">
            <i className="bi bi-exclamation-triangle-fill fs-2"></i>
            <div>
              <h5 className="fw-bold mb-0">Confirm Attribute Deletion</h5>
              <small className="text-muted">This action cannot be undone.</small>
            </div>
          </div>
          <p className="text-muted small">
            Are you sure you want to delete <strong>{selectedIds.length}</strong> selected attribute(s)?
            Deleting will permanently remove these attributes and cascade delete all associated candidate profile values
            and position access criteria.
          </p>

          <div className="d-flex justify-content-end gap-2 mt-4">
            <Button
              variant="outline-secondary"
              onClick={() => setDeleteModalOpen(false)}
              disabled={deleting}
            >
              Cancel
            </Button>
            <Button
              variant="danger"
              onClick={handleDeleteConfirm}
              disabled={deleting}
            >
              {deleting ? 'Deleting...' : `Yes, Delete ${selectedIds.length} Attribute(s)`}
            </Button>
          </div>
        </div>
      </Modal>
    </Container>
  )
}
