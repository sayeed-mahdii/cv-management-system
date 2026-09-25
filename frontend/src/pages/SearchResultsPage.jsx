import React, { useState, useEffect } from 'react'
import { useSearchParams, useNavigate, Link } from 'react-router-dom'
import { Container, Table, Badge, Form, Alert, Spinner } from 'react-bootstrap'
import { api } from '../services/api'

export function SearchResultsPage() {
  const [searchParams] = useSearchParams()
  const query = searchParams.get('q') || ''
  const navigate = useNavigate()

  const [results, setResults] = useState([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  // Filter type: "All", "Position", "Project", "Candidate"
  const [activeFilter, setActiveFilter] = useState('All')

  // Selection states for Toolbar (STRICT UI COMPLIANCE: NO buttons inside rows!)
  const [selectedIds, setSelectedIds] = useState([])

  useEffect(() => {
    if (!query.trim()) {
      setResults([])
      setTotalCount(0)
      return
    }

    setLoading(true)
    setError(null)
    setSelectedIds([])

    api.get(`/api/search?q=${encodeURIComponent(query.trim())}`)
      .then(res => {
        setResults(res.results || [])
        setTotalCount(res.totalResults || 0)
      })
      .catch(err => {
        setError(err.message || 'Failed to execute search.')
      })
      .finally(() => {
        setLoading(false)
      })
  }, [query])

  // Filtered results based on type filter
  const filteredResults = activeFilter === 'All'
    ? results
    : results.filter(r => r.type.toLowerCase() === activeFilter.toLowerCase())

  // Counts by category
  const positionCount = results.filter(r => r.type === 'Position').length
  const projectCount = results.filter(r => r.type === 'Project').length
  const candidateCount = results.filter(r => r.type === 'Candidate').length

  // Selection handlers
  const handleSelectAll = (e) => {
    if (e.target.checked) setSelectedIds(filteredResults.map(r => r.id))
    else setSelectedIds([])
  }

  const handleSelectRow = (id) => {
    setSelectedIds(prev =>
      prev.includes(id) ? prev.filter(item => item !== id) : [...prev, id]
    )
  }

  const handleNavigateSelected = () => {
    if (selectedIds.length !== 1) return
    const item = results.find(r => r.id === selectedIds[0])
    if (item?.url) {
      navigate(item.url)
    }
  }

  return (
    <Container className="py-5">
      {/* Header */}
      <div className="mb-4">
        <div className="d-flex align-items-center gap-2 mb-1">
          <h3 className="fw-bold mb-0">Search Results</h3>
          <Badge bg="primary-subtle" className="text-primary border border-primary-subtle px-2 py-1 small">
            Global Search
          </Badge>
        </div>
        <p className="text-muted small mb-0">
          Showing matching positions, candidate portfolios, and profiles for query:{' '}
          <strong className="text-primary">"{query}"</strong>
        </p>
      </div>

      {error && (
        <Alert variant="danger" className="mb-4">
          <i className="bi bi-exclamation-triangle-fill me-2"></i>
          {error}
        </Alert>
      )}

      {/* Filter Category Pills */}
      <div className="d-flex gap-2 flex-wrap mb-4">
        <button
          type="button"
          className={`btn btn-sm ${activeFilter === 'All' ? 'btn-primary' : 'btn-outline-secondary'} rounded-pill px-3`}
          onClick={() => { setActiveFilter('All'); setSelectedIds([]); }}
        >
          All Results <Badge bg="light" text="dark" className="ms-1">{totalCount}</Badge>
        </button>
        <button
          type="button"
          className={`btn btn-sm ${activeFilter === 'Position' ? 'btn-primary' : 'btn-outline-secondary'} rounded-pill px-3`}
          onClick={() => { setActiveFilter('Position'); setSelectedIds([]); }}
        >
          Vacancies <Badge bg="light" text="dark" className="ms-1">{positionCount}</Badge>
        </button>
        <button
          type="button"
          className={`btn btn-sm ${activeFilter === 'Project' ? 'btn-primary' : 'btn-outline-secondary'} rounded-pill px-3`}
          onClick={() => { setActiveFilter('Project'); setSelectedIds([]); }}
        >
          Projects <Badge bg="light" text="dark" className="ms-1">{projectCount}</Badge>
        </button>
        <button
          type="button"
          className={`btn btn-sm ${activeFilter === 'Candidate' ? 'btn-primary' : 'btn-outline-secondary'} rounded-pill px-3`}
          onClick={() => { setActiveFilter('Candidate'); setSelectedIds([]); }}
        >
          Candidates <Badge bg="light" text="dark" className="ms-1">{candidateCount}</Badge>
        </button>
      </div>

      {/* STRICT UI COMPLIANCE TOOLBAR (Zero buttons inside table rows!) */}
      <div className="table-toolbar rounded-top border d-flex justify-content-between align-items-center p-3 bg-card">
        <div className="d-flex align-items-center gap-2 flex-wrap">
          <button
            type="button"
            className="btn btn-primary-gradient btn-sm px-3 d-flex align-items-center gap-2"
            onClick={handleNavigateSelected}
            disabled={selectedIds.length !== 1}
          >
            <i className="bi bi-box-arrow-up-right"></i>
            <span>Open Selected Result</span>
          </button>

          {selectedIds.length > 0 && (
            <Badge bg="secondary" pill className="ms-2 px-2 py-1">
              {selectedIds.length} selected
            </Badge>
          )}
        </div>

        <div className="text-muted small">
          Showing <strong>{filteredResults.length}</strong> matching records
        </div>
      </div>

      {/* Results Table (NO buttons inside rows!) */}
      <div className="table-responsive border border-top-0 rounded-bottom shadow-sm bg-card mb-4">
        <Table hover className="mb-0 align-middle">
          <thead className="table-light">
            <tr>
              <th style={{ width: '42px' }} className="text-center">
                <Form.Check
                  type="checkbox"
                  checked={filteredResults.length > 0 && selectedIds.length === filteredResults.length}
                  onChange={handleSelectAll}
                  aria-label="Select all search results"
                />
              </th>
              <th style={{ width: '120px' }}>Category</th>
              <th>Result Title &amp; Details</th>
              <th>Matching Snippet</th>
              <th>Tags / Metadata</th>
              <th className="text-end">Last Updated</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={6} className="text-center py-5">
                  <Spinner animation="border" variant="primary" />
                  <p className="mt-2 text-muted small mb-0">Searching platform records...</p>
                </td>
              </tr>
            ) : filteredResults.length === 0 ? (
              <tr>
                <td colSpan={6} className="text-center py-5 text-muted">
                  <i className="bi bi-search fs-2 d-block mb-2 text-dimmed"></i>
                  <p className="mb-1 fw-semibold">No matching records found</p>
                  <small>
                    {query
                      ? `No positions, projects, or candidates matched "${query}". Try different keywords.`
                      : 'Enter a search term in the navbar above.'}
                  </small>
                </td>
              </tr>
            ) : (
              filteredResults.map(item => {
                const isSelected = selectedIds.includes(item.id)
                const typeBadgeClass = item.type === 'Position'
                  ? 'bg-primary'
                  : item.type === 'Project'
                    ? 'bg-success'
                    : 'bg-info text-dark'

                return (
                  <tr
                    key={item.id}
                    className={isSelected ? 'table-primary bg-opacity-10' : ''}
                    onClick={() => handleSelectRow(item.id)}
                    onDoubleClick={() => navigate(item.url)}
                    style={{ cursor: 'pointer' }}
                  >
                    <td className="text-center" onClick={(e) => e.stopPropagation()}>
                      <Form.Check
                        type="checkbox"
                        checked={isSelected}
                        onChange={() => handleSelectRow(item.id)}
                        aria-label={`Select ${item.title}`}
                      />
                    </td>
                    <td>
                      <Badge className={`${typeBadgeClass} px-2 py-1 small`}>
                        {item.type}
                      </Badge>
                    </td>
                    <td>
                      <div className="fw-semibold text-primary">{item.title}</div>
                      <small className="text-muted d-block">{item.subtitle}</small>
                    </td>
                    <td>
                      <small className="text-muted text-truncate d-block" style={{ maxWidth: '380px' }}>
                        {item.snippet}
                      </small>
                    </td>
                    <td>
                      <div className="d-flex gap-1 flex-wrap" style={{ maxWidth: '200px' }}>
                        {item.tags?.length > 0 ? (
                          item.tags.map((tag, i) => (
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
                      {new Date(item.updatedAt).toLocaleDateString()}
                    </td>
                  </tr>
                )
              })
            )}
          </tbody>
        </Table>
      </div>
    </Container>
  )
}
