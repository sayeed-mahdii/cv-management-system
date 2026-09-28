import React, { useState, useEffect, useCallback } from 'react'
import { Container, Table, Badge, Form, Alert, Modal, Button, Spinner } from 'react-bootstrap'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { api } from '../services/api'
import { useAuth } from '../context/AuthContext'

export function AdminUsersPage() {
  const { t } = useTranslation()
  const { user: currentUser, fetchProfile } = useAuth()
  const navigate = useNavigate()

  // User list state
  const [users, setUsers] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [actionSuccess, setActionSuccess] = useState(null)

  // Filters & search
  const [search, setSearch] = useState('')
  const [roleFilter, setRoleFilter] = useState('ALL')

  // Selection states for Toolbar (STRICT UI COMPLIANCE: ZERO buttons inside table rows!)
  const [selectedIds, setSelectedIds] = useState([])

  // Action busy states
  const [actionLoading, setActionLoading] = useState(false)

  // Manage Roles Modal state
  const [rolesModalOpen, setRolesModalOpen] = useState(false)
  const [targetUserForRoles, setTargetUserForRoles] = useState(null)
  const [selectedRoles, setSelectedRoles] = useState([])

  // Delete Confirmation Modal state
  const [deleteModalOpen, setDeleteModalOpen] = useState(false)

  // Fetch users
  const fetchUsers = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await api.get('/api/admin/users')
      setUsers(data || [])
      setSelectedIds([])
    } catch (err) {
      setError(err.message || 'Failed to load user roster.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchUsers()
  }, [fetchUsers])

  // Filtered users
  const filteredUsers = users.filter(u => {
    const query = search.trim().toLowerCase()
    const matchesSearch = !query ||
      u.email.toLowerCase().includes(query) ||
      (u.candidateName && u.candidateName.toLowerCase().includes(query)) ||
      u.roles.some(r => r.toLowerCase().includes(query))

    const matchesRole = roleFilter === 'ALL' ||
      (roleFilter === 'BLOCKED' && u.isBlocked) ||
      u.roles.some(r => r.toUpperCase() === roleFilter.toUpperCase())

    return matchesSearch && matchesRole
  })

  // Selection handlers
  const handleSelectAll = (e) => {
    if (e.target.checked) setSelectedIds(filteredUsers.map(u => u.id))
    else setSelectedIds([])
  }

  const handleSelectRow = (id) => {
    setSelectedIds(prev =>
      prev.includes(id) ? prev.filter(item => item !== id) : [...prev, id]
    )
  }

  // Batch Block / Unblock
  const handleToggleBlock = async (shouldBlock) => {
    if (selectedIds.length === 0) return
    setActionLoading(true)
    setActionSuccess(null)
    setError(null)
    try {
      await api.put('/api/admin/users/block', {
        userIds: selectedIds,
        isBlocked: shouldBlock
      })
      setActionSuccess(
        shouldBlock
          ? `Successfully blocked ${selectedIds.length} user(s). Operational modifications suspended.`
          : `Successfully unblocked ${selectedIds.length} user(s). Full platform privileges restored.`
      )
      await fetchUsers()
      if (selectedIds.includes(currentUser?.id)) {
        await fetchProfile()
      }
    } catch (err) {
      setError(err.message || 'Failed to update user block status.')
    } finally {
      setActionLoading(false)
    }
  }

  // Open Manage Roles Modal
  const handleOpenManageRoles = () => {
    if (selectedIds.length !== 1) return
    const target = users.find(u => u.id === selectedIds[0])
    if (!target) return
    setTargetUserForRoles(target)
    setSelectedRoles([...target.roles])
    setRolesModalOpen(true)
  }

  // Save Role Changes (Admins may remove their own role)
  const handleSaveRoles = async () => {
    if (!targetUserForRoles) return
    setActionLoading(true)
    setError(null)
    try {
      const res = await api.put(`/api/admin/users/${targetUserForRoles.id}/roles`, {
        roles: selectedRoles
      })

      setRolesModalOpen(false)
      setActionSuccess(`Successfully updated roles for ${targetUserForRoles.email}.`)
      await fetchUsers()

      // If the caller modified their own roles and lost Administrator privileges
      if (res.callerLostAdmin) {
        await fetchProfile()
        alert('You have removed your Administrator role. You will now be redirected to the home page.')
        navigate('/')
      }
    } catch (err) {
      setError(err.message || 'Failed to update user roles.')
    } finally {
      setActionLoading(false)
    }
  }

  // Batch Delete Users
  const handleDeleteConfirm = async () => {
    if (selectedIds.length === 0) return
    setActionLoading(true)
    setError(null)
    try {
      await api.post('/api/admin/users/batch-delete', {
        userIds: selectedIds
      })
      setDeleteModalOpen(false)
      setActionSuccess(`Successfully deleted ${selectedIds.length} user(s) and cascading records.`)
      await fetchUsers()

      if (selectedIds.includes(currentUser?.id)) {
        alert('Your account has been deleted. Redirecting to home...')
        navigate('/')
      }
    } catch (err) {
      setError(err.message || 'Failed to delete selected users.')
    } finally {
      setActionLoading(false)
    }
  }

  const isSelfInRolesModal = targetUserForRoles?.id === currentUser?.id
  const willLoseAdmin = isSelfInRolesModal && !selectedRoles.includes('Administrator')

  return (
    <Container className="py-5">
      {/* Page Header */}
      <div className="mb-4">
        <div className="d-flex align-items-center gap-2 mb-1">
          <h3 className="fw-bold mb-0">{t('admin.title')}</h3>
          <Badge bg="danger-subtle" className="text-danger border border-danger-subtle px-2 py-1 small">
            Admin Directory
          </Badge>
        </div>
        <p className="text-muted small mb-0">{t('admin.subtitle')}</p>
      </div>

      {/* Notifications */}
      {actionSuccess && (
        <Alert variant="success" dismissible onClose={() => setActionSuccess(null)} className="py-2 small mb-4">
          <i className="bi bi-check-circle-fill me-2"></i>
          {actionSuccess}
        </Alert>
      )}

      {error && (
        <Alert variant="danger" dismissible onClose={() => setError(null)} className="py-2 small mb-4">
          <i className="bi bi-exclamation-triangle-fill me-2"></i>
          {error}
        </Alert>
      )}

      {/* Filter and Search Controls */}
      <div className="p-3 bg-card border rounded-3 mb-4 shadow-sm">
        <div className="row g-2 align-items-center">
          <div className="col-md-6">
            <div className="input-group input-group-sm">
              <span className="input-group-text bg-body text-muted">
                <i className="bi bi-search"></i>
              </span>
              <Form.Control
                type="text"
                placeholder={t('admin.searchPlaceholder')}
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="form-control-pro"
              />
              {search && (
                <button
                  type="button"
                  className="btn btn-outline-secondary"
                  onClick={() => setSearch('')}
                >
                  <i className="bi bi-x"></i>
                </button>
              )}
            </div>
          </div>

          <div className="col-md-6 d-flex gap-2 justify-content-md-end align-items-center flex-wrap">
            <div className="btn-group btn-group-sm" role="group">
              <button
                type="button"
                className={`btn ${roleFilter === 'ALL' ? 'btn-primary' : 'btn-outline-secondary'}`}
                onClick={() => setRoleFilter('ALL')}
              >
                {t('admin.allRoles')}
              </button>
              <button
                type="button"
                className={`btn ${roleFilter === 'Candidate' ? 'btn-primary' : 'btn-outline-secondary'}`}
                onClick={() => setRoleFilter('Candidate')}
              >
                Candidates
              </button>
              <button
                type="button"
                className={`btn ${roleFilter === 'Recruiter' ? 'btn-primary' : 'btn-outline-secondary'}`}
                onClick={() => setRoleFilter('Recruiter')}
              >
                Recruiters
              </button>
              <button
                type="button"
                className={`btn ${roleFilter === 'Administrator' ? 'btn-primary' : 'btn-outline-secondary'}`}
                onClick={() => setRoleFilter('Administrator')}
              >
                Admins
              </button>
              <button
                type="button"
                className={`btn ${roleFilter === 'BLOCKED' ? 'btn-danger' : 'btn-outline-danger'}`}
                onClick={() => setRoleFilter('BLOCKED')}
              >
                Blocked
              </button>
            </div>

            <button
              type="button"
              className="btn btn-sm btn-outline-secondary"
              onClick={fetchUsers}
              disabled={loading}
              title={t('common.refresh')}
            >
              <i className="bi bi-arrow-clockwise"></i>
            </button>
          </div>
        </div>
      </div>

      {/* STRICT UI COMPLIANCE TOOLBAR (ZERO action buttons inside table rows!) */}
      <div className="table-toolbar rounded-top border d-flex justify-content-between align-items-center p-3 bg-card">
        <div className="d-flex align-items-center gap-2 flex-wrap">
          {/* Block Selected */}
          <button
            type="button"
            className="btn btn-outline-danger btn-sm px-3 d-flex align-items-center gap-2"
            onClick={() => handleToggleBlock(true)}
            disabled={selectedIds.length === 0 || actionLoading}
          >
            <i className="bi bi-shield-slash"></i>
            <span>{t('admin.blockSelected')}</span>
          </button>

          {/* Unblock Selected */}
          <button
            type="button"
            className="btn btn-outline-success btn-sm px-3 d-flex align-items-center gap-2"
            onClick={() => handleToggleBlock(false)}
            disabled={selectedIds.length === 0 || actionLoading}
          >
            <i className="bi bi-shield-check"></i>
            <span>{t('admin.unblockSelected')}</span>
          </button>

          {/* Manage Roles */}
          <button
            type="button"
            className="btn btn-primary-gradient btn-sm px-3 d-flex align-items-center gap-2"
            onClick={handleOpenManageRoles}
            disabled={selectedIds.length !== 1 || actionLoading}
          >
            <i className="bi bi-person-gear"></i>
            <span>{t('admin.manageRoles')}</span>
          </button>

          {/* Delete Selected */}
          <button
            type="button"
            className="btn btn-danger btn-sm px-3 d-flex align-items-center gap-2"
            onClick={() => setDeleteModalOpen(true)}
            disabled={selectedIds.length === 0 || actionLoading}
          >
            <i className="bi bi-trash"></i>
            <span>{t('admin.deleteSelected')}</span>
          </button>

          {selectedIds.length > 0 && (
            <Badge bg="secondary" pill className="ms-2 px-2 py-1">
              {t('admin.selectedCount', { count: selectedIds.length })}
            </Badge>
          )}
        </div>

        <div className="text-muted small">
          {t('admin.showingUsers', { count: filteredUsers.length })}
        </div>
      </div>

      {/* Users Table (STRICT UI COMPLIANCE: ZERO action buttons inside table rows!) */}
      <div className="table-responsive border border-top-0 rounded-bottom shadow-sm bg-card mb-4">
        <Table hover className="mb-0 align-middle">
          <thead className="table-light">
            <tr>
              <th style={{ width: '42px' }} className="text-center">
                <Form.Check
                  type="checkbox"
                  checked={filteredUsers.length > 0 && selectedIds.length === filteredUsers.length}
                  onChange={handleSelectAll}
                  aria-label="Select all users"
                />
              </th>
              <th>{t('admin.tableUser')}</th>
              <th>{t('admin.tableRoles')}</th>
              <th className="text-center">{t('admin.tableStatus')}</th>
              <th className="text-center">{t('admin.tableActivity')}</th>
              <th className="text-end">{t('admin.tableJoined')}</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={6} className="text-center py-5">
                  <Spinner animation="border" variant="primary" />
                  <p className="mt-2 text-muted small mb-0">{t('common.loading')}</p>
                </td>
              </tr>
            ) : filteredUsers.length === 0 ? (
              <tr>
                <td colSpan={6} className="text-center py-5 text-muted">
                  <i className="bi bi-people fs-2 d-block mb-2 text-dimmed"></i>
                  <p className="mb-0">{t('admin.noUsersFound')}</p>
                </td>
              </tr>
            ) : (
              filteredUsers.map(u => {
                const isSelected = selectedIds.includes(u.id)
                const isCurrent = u.id === currentUser?.id

                return (
                  <tr
                    key={u.id}
                    className={isSelected ? 'table-primary bg-opacity-10' : ''}
                    onClick={() => handleSelectRow(u.id)}
                    style={{ cursor: 'pointer' }}
                  >
                    <td className="text-center" onClick={(e) => e.stopPropagation()}>
                      <Form.Check
                        type="checkbox"
                        checked={isSelected}
                        onChange={() => handleSelectRow(u.id)}
                        aria-label={`Select ${u.email}`}
                      />
                    </td>
                    <td>
                      <div className="d-flex align-items-center gap-3">
                        <div
                          className="rounded-circle d-flex align-items-center justify-content-center text-white fw-bold shadow-sm"
                          style={{
                            width: '38px',
                            height: '38px',
                            background: u.isBlocked
                              ? '#ef4444'
                              : 'linear-gradient(135deg, #3b82f6 0%, #6366f1 100%)',
                            fontSize: '14px'
                          }}
                        >
                          {(u.email[0] || 'U').toUpperCase()}
                        </div>
                        <div>
                          <div className="fw-semibold d-flex align-items-center gap-2">
                            <span>{u.email}</span>
                            {isCurrent && (
                              <Badge bg="secondary-subtle" className="text-secondary border small">
                                (You)
                              </Badge>
                            )}
                          </div>
                          <small className="text-muted d-block">
                            {u.candidateName || <span className="fst-italic">No profile name</span>}
                          </small>
                        </div>
                      </div>
                    </td>
                    <td>
                      <div className="d-flex gap-1 flex-wrap">
                        {u.roles.map(r => (
                          <Badge
                            key={r}
                            bg={r === 'Administrator' ? 'danger' : r === 'Recruiter' ? 'primary' : 'success'}
                            className="px-2 py-1"
                          >
                            {r}
                          </Badge>
                        ))}
                      </div>
                    </td>
                    <td className="text-center">
                      {u.isBlocked ? (
                        <Badge bg="danger-subtle" className="text-danger border border-danger-subtle px-2 py-1">
                          <i className="bi bi-shield-slash me-1"></i> {t('admin.statusBlocked')}
                        </Badge>
                      ) : (
                        <Badge bg="success-subtle" className="text-success border border-success-subtle px-2 py-1">
                          <i className="bi bi-shield-check me-1"></i> {t('admin.statusActive')}
                        </Badge>
                      )}
                    </td>
                    <td className="text-center text-muted small">
                      <span className="badge bg-light text-dark border me-1">
                        {t('admin.projectsCount', { count: u.projectsCount })}
                      </span>
                      <span className="badge bg-light text-dark border">
                        {t('admin.applicationsCount', { count: u.applicationsCount })}
                      </span>
                    </td>
                    <td className="text-end text-muted small">
                      {new Date(u.createdAt).toLocaleDateString()}
                    </td>
                  </tr>
                )
              })
            )}
          </tbody>
        </Table>
      </div>

      {/* MODAL: MANAGE USER ROLES */}
      <Modal show={rolesModalOpen} onHide={() => setRolesModalOpen(false)} centered backdrop="static">
        <div className="p-4 bg-body rounded-4 shadow">
          <div className="d-flex justify-content-between align-items-center mb-3">
            <h5 className="fw-bold mb-0">
              {t('admin.rolesModalTitle', { email: targetUserForRoles?.email })}
            </h5>
            <button type="button" className="btn-close" onClick={() => setRolesModalOpen(false)} disabled={actionLoading} />
          </div>

          <p className="text-muted small mb-3">
            Configure assigned RBAC roles for this account. Users can possess multiple roles simultaneously.
          </p>

          {willLoseAdmin && (
            <Alert variant="warning" className="small py-2 mb-3">
              <i className="bi bi-exclamation-triangle-fill me-2"></i>
              {t('admin.selfDemoteWarning')}
            </Alert>
          )}

          <div className="p-3 border rounded-3 bg-light mb-4">
            <Form.Check
              type="checkbox"
              id="role-candidate"
              label={
                <div>
                  <strong>Candidate</strong>
                  <div className="text-muted small">Can build profile, manage projects, and submit CVs to vacancies.</div>
                </div>
              }
              checked={selectedRoles.includes('Candidate')}
              onChange={(e) => {
                if (e.target.checked) setSelectedRoles(prev => [...prev, 'Candidate'])
                else setSelectedRoles(prev => prev.filter(r => r !== 'Candidate'))
              }}
              className="mb-3"
            />

            <Form.Check
              type="checkbox"
              id="role-recruiter"
              label={
                <div>
                  <strong>Recruiter</strong>
                  <div className="text-muted small">Can create/edit positions, search candidates, and review applicants.</div>
                </div>
              }
              checked={selectedRoles.includes('Recruiter')}
              onChange={(e) => {
                if (e.target.checked) setSelectedRoles(prev => [...prev, 'Recruiter'])
                else setSelectedRoles(prev => prev.filter(r => r !== 'Recruiter'))
              }}
              className="mb-3"
            />

            <Form.Check
              type="checkbox"
              id="role-admin"
              label={
                <div>
                  <strong className="text-danger">Administrator</strong>
                  <div className="text-muted small">Full system access, block/unblock accounts, and manage user roles.</div>
                </div>
              }
              checked={selectedRoles.includes('Administrator')}
              onChange={(e) => {
                if (e.target.checked) setSelectedRoles(prev => [...prev, 'Administrator'])
                else setSelectedRoles(prev => prev.filter(r => r !== 'Administrator'))
              }}
            />
          </div>

          <div className="d-flex justify-content-end gap-2">
            <Button variant="outline-secondary" onClick={() => setRolesModalOpen(false)} disabled={actionLoading}>
              {t('common.cancel')}
            </Button>
            <Button
              className="btn-primary-gradient px-4"
              onClick={handleSaveRoles}
              disabled={actionLoading || selectedRoles.length === 0}
            >
              {actionLoading ? t('common.saving') : t('common.save')}
            </Button>
          </div>
        </div>
      </Modal>

      {/* MODAL: DELETE CONFIRMATION */}
      <Modal show={deleteModalOpen} onHide={() => setDeleteModalOpen(false)} centered>
        <div className="p-4 bg-body rounded-4 shadow">
          <div className="text-danger fs-1 mb-2">
            <i className="bi bi-exclamation-triangle-fill"></i>
          </div>
          <h5 className="fw-bold text-danger mb-2">{t('admin.confirmDeleteTitle')}</h5>
          <p className="text-muted small">
            {t('admin.confirmDeleteDesc', { count: selectedIds.length })}
          </p>
          <div className="d-flex justify-content-end gap-2 mt-4">
            <Button variant="outline-secondary" onClick={() => setDeleteModalOpen(false)} disabled={actionLoading}>
              {t('common.cancel')}
            </Button>
            <Button variant="danger" onClick={handleDeleteConfirm} disabled={actionLoading}>
              {actionLoading ? t('common.saving') : t('admin.confirmDeleteBtn')}
            </Button>
          </div>
        </div>
      </Modal>
    </Container>
  )
}
