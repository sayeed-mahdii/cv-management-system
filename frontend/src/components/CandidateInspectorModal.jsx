import React, { useState, useEffect } from 'react'
import { Modal, Button, Spinner, Alert, Badge, Row, Col, Card, Table } from 'react-bootstrap'
import ReactMarkdown from 'react-markdown'
import { api } from '../services/api'

export function CandidateInspectorModal({ show, onHide, positionId, candidateId }) {
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (show && positionId && candidateId) {
      setLoading(true)
      setError(null)
      api.get(`/api/positions/${positionId}/applicants/${candidateId}`)
        .then(res => setData(res))
        .catch(err => setError(err.message || 'Failed to load candidate virtual profile.'))
        .finally(() => setLoading(false))
    } else {
      setData(null)
    }
  }, [show, positionId, candidateId])

  if (!show) return null

  // Extract photo from Me attributes if available
  const photoUrl = data?.meAttributes?.find(a => a.name.toLowerCase().includes('photo'))?.value

  return (
    <Modal show={show} onHide={onHide} size="xl" centered scrollable>
      <div className="p-4 bg-body rounded-4 shadow">
        {/* Header */}
        <div className="d-flex justify-content-between align-items-start mb-3 border-bottom pb-3">
          <div>
            <div className="d-flex align-items-center gap-2 mb-1">
              <h4 className="fw-bold mb-0">Candidate Profile Inspector</h4>
              <Badge bg="primary" pill className="px-2 py-1 small">
                Virtual CV
              </Badge>
            </div>
            <p className="text-muted small mb-0">
              Reviewing applicant for position: <strong className="text-primary">{data?.positionTitle}</strong>
            </p>
          </div>
          <button type="button" className="btn-close" onClick={onHide} />
        </div>

        {loading ? (
          <div className="text-center py-5">
            <Spinner animation="border" variant="primary" />
            <p className="mt-2 text-muted small">Assembling virtual candidate profile...</p>
          </div>
        ) : error ? (
          <Alert variant="danger">
            <i className="bi bi-exclamation-triangle-fill me-2"></i>
            {error}
          </Alert>
        ) : data ? (
          <div className="d-flex flex-column gap-4">
            {/* Candidate Identity Card */}
            <Card className="card-pro p-3 border-0 bg-primary-subtle bg-opacity-25">
              <div className="d-flex flex-column flex-sm-row align-items-center gap-3">
                {photoUrl ? (
                  <img
                    src={photoUrl}
                    alt={data.candidateName}
                    className="rounded-circle shadow-sm object-fit-cover"
                    style={{ width: '80px', height: '80px' }}
                  />
                ) : (
                  <div
                    className="rounded-circle d-flex align-items-center justify-content-center text-white fw-bold shadow-sm"
                    style={{
                      width: '80px',
                      height: '80px',
                      background: 'linear-gradient(135deg, #6366f1 0%, #a855f7 100%)',
                      fontSize: '32px'
                    }}
                  >
                    {(data.candidateName?.[0] || 'C').toUpperCase()}
                  </div>
                )}

                <div className="flex-grow-1 text-center text-sm-start">
                  <h5 className="fw-bold mb-1">{data.candidateName}</h5>
                  <div className="d-flex flex-wrap align-items-center justify-content-center justify-content-sm-start gap-3 text-muted small">
                    <span>
                      <i className="bi bi-envelope me-1"></i> {data.candidateEmail}
                    </span>
                    <span>
                      <i className="bi bi-geo-alt me-1"></i>
                      {data.meAttributes.find(a => a.name.toLowerCase().includes('location'))?.value || 'Location not specified'}
                    </span>
                    <span>
                      <i className="bi bi-calendar-check me-1"></i>
                      Submitted {new Date(data.submittedAt).toLocaleDateString()}
                    </span>
                  </div>
                </div>

                <div>
                  <Badge bg="success" className="px-3 py-2 fs-6">
                    <i className="bi bi-check-circle me-1"></i> {data.state}
                  </Badge>
                </div>
              </div>
            </Card>

            {/* Position Requirements Checklist */}
            <Card className="card-pro p-3">
              <div className="d-flex justify-content-between align-items-center mb-3">
                <div>
                  <h6 className="fw-bold mb-0">Position Requirements Evaluation</h6>
                  <small className="text-muted">
                    Checking candidate's dynamic profile values against required position attributes
                  </small>
                </div>
              </div>

              {data.requiredAttributesCheck.length === 0 ? (
                <p className="text-muted small fst-italic mb-0">
                  This position does not require any custom library attributes.
                </p>
              ) : (
                <div className="table-responsive">
                  <Table hover size="sm" className="align-middle mb-0">
                    <thead className="table-light">
                      <tr>
                        <th>Required Attribute</th>
                        <th>Category</th>
                        <th>Type</th>
                        <th>Candidate Value</th>
                        <th className="text-end">Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.requiredAttributesCheck.map(attr => (
                        <tr key={attr.attributeDefinitionId}>
                          <td className="fw-semibold">{attr.name}</td>
                          <td>
                            <Badge bg="light" text="dark" className="border small">
                              {attr.categoryName || 'General'}
                            </Badge>
                          </td>
                          <td>
                            <Badge bg="primary-subtle" className="text-primary border border-primary-subtle small">
                              {attr.dataType}
                            </Badge>
                          </td>
                          <td>
                            {attr.candidateValue ? (
                              <span className="fw-medium text-dark">{attr.candidateValue}</span>
                            ) : (
                              <span className="text-muted fst-italic small">Not provided</span>
                            )}
                          </td>
                          <td className="text-end">
                            {attr.isSatisfied ? (
                              <Badge bg="success" className="px-2 py-1">
                                <i className="bi bi-check2-circle me-1"></i> Satisfied
                              </Badge>
                            ) : (
                              <Badge bg="warning" text="dark" className="px-2 py-1">
                                <i className="bi bi-exclamation-circle me-1"></i> Missing
                              </Badge>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </Table>
                </div>
              )}
            </Card>

            {/* Additional Candidate Information (Info Attributes) */}
            {data.infoAttributes.length > 0 && (
              <Card className="card-pro p-3">
                <h6 className="fw-bold mb-3">Additional Candidate Skills &amp; Credentials</h6>
                <Row className="g-3">
                  {data.infoAttributes.map(attr => (
                    <Col md={6} key={attr.attributeDefinitionId}>
                      <div className="p-3 border rounded-3 bg-card h-100">
                        <div className="d-flex justify-content-between align-items-center mb-1">
                          <span className="fw-semibold small">{attr.name}</span>
                          <Badge bg="secondary" pill style={{ fontSize: '10px' }}>
                            {attr.categoryName}
                          </Badge>
                        </div>
                        <div className="fw-medium text-primary">
                          {attr.value || <span className="text-muted fst-italic small">Empty</span>}
                        </div>
                      </div>
                    </Col>
                  ))}
                </Row>
              </Card>
            )}

            {/* Filtered Portfolio Projects */}
            <Card className="card-pro p-3">
              <div className="d-flex flex-column flex-sm-row justify-content-between align-items-start align-items-sm-center mb-3 gap-2">
                <div>
                  <h6 className="fw-bold mb-0">
                    Tailored Portfolio Projects ({data.projects.length})
                  </h6>
                  <small className="text-muted">
                    Dynamically filtered to match position tags:{' '}
                    {data.positionTags.length > 0 ? (
                      data.positionTags.map(t => `#${t}`).join(', ')
                    ) : (
                      'All technologies'
                    )}{' '}
                    (limited to top {data.maxProjects} most recent)
                  </small>
                </div>
                <Badge bg="info-subtle" className="text-info-emphasis border px-2 py-1 small">
                  Max Limit: {data.maxProjects}
                </Badge>
              </div>

              {data.projects.length === 0 ? (
                <div className="text-center py-4 text-muted border rounded-3 bg-light">
                  <i className="bi bi-code-slash fs-3 d-block mb-1 text-dimmed"></i>
                  <p className="mb-0 small">No matching portfolio projects found for this candidate.</p>
                </div>
              ) : (
                <div className="d-flex flex-column gap-3">
                  {data.projects.map(proj => (
                    <div key={proj.id} className="p-3 border rounded-3 bg-card">
                      <div className="d-flex justify-content-between align-items-start mb-2">
                        <div>
                          <h6 className="fw-bold mb-1 text-primary">{proj.name}</h6>
                          <small className="text-muted">
                            <i className="bi bi-calendar3 me-1"></i>
                            {proj.startDate} &rarr; {proj.endDate || 'Present'}
                          </small>
                        </div>
                        <div className="d-flex gap-1 flex-wrap justify-content-end">
                          {proj.tags.map((tag, idx) => (
                            <Badge key={idx} bg="body" className="text-primary border small">
                              #{tag}
                            </Badge>
                          ))}
                        </div>
                      </div>

                      <div className="small text-muted border-top pt-2">
                        <ReactMarkdown>{proj.description}</ReactMarkdown>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </Card>
          </div>
        ) : null}

        <div className="d-flex justify-content-end pt-3 mt-3 border-top">
          <Button variant="outline-secondary" onClick={onHide}>
            Close Inspector
          </Button>
        </div>
      </div>
    </Modal>
  )
}
