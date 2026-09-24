import React, { useState, useEffect } from 'react'
import { Modal, Button, Table, Form, Badge, Spinner, Alert } from 'react-bootstrap'
import { api } from '../services/api'
import { CandidateInspectorModal } from './CandidateInspectorModal'

export function ApplicantsModal({ show, onHide, position }) {
  const [applicants, setApplicants] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  // Selection states for Toolbar (STRICT UI COMPLIANCE: NO buttons inside rows!)
  const [selectedIds, setSelectedIds] = useState([])
  const [search, setSearch] = useState('')

  // Inspector modal states
  const [inspectorOpen, setInspectorOpen] = useState(false)
  const [inspectCandidateId, setInspectCandidateId] = useState(null)

  useEffect(() => {
    if (show && position?.id) {
      setLoading(true)
      setError(null)
      setSelectedIds([])
      setSearch('')
      api.get(`/api/positions/${position.id}/applicants`)
        .then(res => setApplicants(res || []))
        .catch(err => setError(err.message || 'Failed to load position applicants.'))
        .finally(() => setLoading(false))
    } else {
      setApplicants([])
      setSelectedIds([])
    }
  }, [show, position])

  if (!show || !position) return null

  // Filter applicants
  const filteredApplicants = applicants.filter(a => {
    if (!search) return true
    const q = search.toLowerCase()
    return (
      a.candidateName?.toLowerCase().includes(q) ||
      a.candidateEmail?.toLowerCase().includes(q) ||
      a.location?.toLowerCase().includes(q)
    )
  })

  // Selection handlers
  const handleSelectAll = (e) => {
    if (e.target.checked) setSelectedIds(filteredApplicants.map(a => a.candidateId))
    else setSelectedIds([])
  }

  const handleSelectRow = (candidateId) => {
    setSelectedIds(prev =>
      prev.includes(candidateId) ? prev.filter(id => id !== candidateId) : [...prev, candidateId]
    )
  }

  const handleOpenInspector = (candidateId) => {
    setInspectCandidateId(candidateId)
    setInspectorOpen(true)
  }

  return (
    <>
      <Modal show={show} onHide={onHide} size="xl" centered>
        <div className="p-4 bg-body rounded-4 shadow">
          {/* Header */}
          <div className="d-flex justify-content-between align-items-start mb-3 border-bottom pb-3">
            <div>
              <div className="d-flex align-items-center gap-2 mb-1">
                <h4 className="fw-bold mb-0">Applicants Review</h4>
                <Badge bg="primary" pill className="px-2 py-1 small">
                  {applicants.length} Applicant(s)
                </Badge>
              </div>
              <p className="text-muted small mb-0">
                Position: <strong className="text-primary">{position.title}</strong> &middot; Max Projects:{' '}
                <strong>{position.maxProjects}</strong>
              </p>
            </div>
            <button type="button" className="btn-close" onClick={onHide} />
          </div>

          {error && (
            <Alert variant="danger" className="py-2 small mb-3">
              <i className="bi bi-exclamation-triangle-fill me-2"></i>
              {error}
            </Alert>
          )}

          {/* Search Filter */}
          <div className="mb-3">
            <Form.Control
              type="text"
              placeholder="Filter applicants by candidate name, email, or location..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="form-control-pro"
            />
          </div>

          {/* STRICT UI COMPLIANCE TOOLBAR (Zero buttons inside table rows!) */}
          <div className="table-toolbar rounded-top border d-flex justify-content-between align-items-center p-3 bg-card">
            <div className="d-flex align-items-center gap-2 flex-wrap">
              <button
                type="button"
                className="btn btn-primary-gradient btn-sm px-3 d-flex align-items-center gap-2"
                onClick={() => selectedIds.length === 1 && handleOpenInspector(selectedIds[0])}
                disabled={selectedIds.length !== 1}
              >
                <i className="bi bi-person-bounding-box"></i>
                <span>Inspect Candidate CV</span>
              </button>

              {selectedIds.length > 0 && (
                <Badge bg="secondary" pill className="ms-2 px-2 py-1">
                  {selectedIds.length} candidate(s) selected
                </Badge>
              )}
            </div>

            <div className="text-muted small">
              Showing <strong>{filteredApplicants.length}</strong> of <strong>{applicants.length}</strong>
            </div>
          </div>

          {/* Applicants Table (NO buttons inside rows!) */}
          <div className="table-responsive border border-top-0 rounded-bottom shadow-sm bg-card mb-4" style={{ maxHeight: '420px', overflowY: 'auto' }}>
            <Table hover className="mb-0 align-middle">
              <thead className="table-light sticky-top">
                <tr>
                  <th style={{ width: '42px' }} className="text-center">
                    <Form.Check
                      type="checkbox"
                      checked={filteredApplicants.length > 0 && selectedIds.length === filteredApplicants.length}
                      onChange={handleSelectAll}
                      aria-label="Select all applicants"
                    />
                  </th>
                  <th>Candidate</th>
                  <th>Email</th>
                  <th>Location</th>
                  <th>Submission Date</th>
                  <th className="text-end">Status</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan={6} className="text-center py-5">
                      <Spinner animation="border" variant="primary" />
                      <p className="mt-2 text-muted small mb-0">Loading applicants...</p>
                    </td>
                  </tr>
                ) : filteredApplicants.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="text-center py-5 text-muted">
                      <i className="bi bi-people fs-2 d-block mb-2 text-dimmed"></i>
                      <p className="mb-1 fw-semibold">No applicants found</p>
                      <small>
                        {search
                          ? `No candidates match "${search}".`
                          : 'No candidates have submitted an application for this position yet.'}
                      </small>
                    </td>
                  </tr>
                ) : (
                  filteredApplicants.map(applicant => {
                    const isSelected = selectedIds.includes(applicant.candidateId)
                    return (
                      <tr
                        key={applicant.candidateId}
                        className={isSelected ? 'table-primary bg-opacity-10' : ''}
                        onClick={() => handleSelectRow(applicant.candidateId)}
                        onDoubleClick={() => handleOpenInspector(applicant.candidateId)}
                        style={{ cursor: 'pointer' }}
                      >
                        <td className="text-center" onClick={(e) => e.stopPropagation()}>
                          <Form.Check
                            type="checkbox"
                            checked={isSelected}
                            onChange={() => handleSelectRow(applicant.candidateId)}
                            aria-label={`Select ${applicant.candidateName}`}
                          />
                        </td>
                        <td>
                          <div className="d-flex align-items-center gap-2">
                            {applicant.photoUrl ? (
                              <img
                                src={applicant.photoUrl}
                                alt={applicant.candidateName}
                                className="rounded-circle object-fit-cover shadow-sm"
                                style={{ width: '32px', height: '32px' }}
                              />
                            ) : (
                              <div
                                className="rounded-circle d-flex align-items-center justify-content-center text-white fw-bold shadow-sm"
                                style={{
                                  width: '32px',
                                  height: '32px',
                                  background: 'linear-gradient(135deg, #6366f1 0%, #a855f7 100%)',
                                  fontSize: '12px'
                                }}
                              >
                                {(applicant.candidateName?.[0] || 'C').toUpperCase()}
                              </div>
                            )}
                            <span className="fw-semibold text-primary">{applicant.candidateName}</span>
                          </div>
                        </td>
                        <td className="text-muted small">{applicant.candidateEmail}</td>
                        <td className="text-muted small">{applicant.location}</td>
                        <td className="text-muted small">
                          {new Date(applicant.submittedAt).toLocaleDateString()}
                        </td>
                        <td className="text-end">
                          <Badge bg="success" className="px-2 py-1">
                            {applicant.state}
                          </Badge>
                        </td>
                      </tr>
                    )
                  })
                )}
              </tbody>
            </Table>
          </div>

          <div className="d-flex justify-content-end">
            <Button variant="outline-secondary" onClick={onHide}>
              Close
            </Button>
          </div>
        </div>
      </Modal>

      {/* Candidate Profile Inspector Modal */}
      <CandidateInspectorModal
        show={inspectorOpen}
        onHide={() => setInspectorOpen(false)}
        positionId={position?.id}
        candidateId={inspectCandidateId}
      />
    </>
  )
}
