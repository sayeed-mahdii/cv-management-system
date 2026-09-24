import { useState, useEffect } from 'react'
import 'bootstrap/dist/css/bootstrap.min.css'

function App() {
  const [health, setHealth] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const checkHealth = async () => {
    setLoading(true)
    setError(null)
    try {
      const response = await fetch('/api/health')
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }
      const data = await response.json()
      setHealth(data)
    } catch (err) {
      setError(err.message)
      setHealth(null)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    checkHealth()
  }, [])

  return (
    <div className="container py-5">
      <div className="row justify-content-center">
        <div className="col-md-8 col-lg-6">
          <div className="card shadow-sm border-0">
            <div className="card-header bg-primary text-white py-3">
              <h4 className="mb-0 text-center fw-bold">CV Management System</h4>
              <p className="text-center mb-0 text-white-50 small">Phase 2: Walking Skeleton</p>
            </div>
            <div className="card-body p-4">
              <h5 className="card-title fw-bold mb-3">System Health Status</h5>

              {loading && (
                <div className="text-center py-4">
                  <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading...</span>
                  </div>
                  <p className="mt-2 text-muted small">Checking connection to backend and database...</p>
                </div>
              )}

              {error && (
                <div className="alert alert-danger" role="alert">
                  <h6 className="alert-heading fw-bold">Connection Failed</h6>
                  <p className="mb-0 small">{error}</p>
                  <p className="mb-0 small text-muted mt-1">Make sure the backend is running on <code>http://localhost:5190</code>.</p>
                </div>
              )}

              {health && !loading && (
                <div className="list-group list-group-flush mb-4">
                  <div className="list-group-item d-flex justify-content-between align-items-center px-0">
                    <div>
                      <div className="fw-semibold">C# .NET 10 Web API</div>
                      <small className="text-muted">Route: /api/health</small>
                    </div>
                    <span className={`badge ${health.status === 'Healthy' ? 'bg-success' : 'bg-warning text-dark'}`}>
                      {health.status}
                    </span>
                  </div>

                  <div className="list-group-item d-flex justify-content-between align-items-center px-0">
                    <div>
                      <div className="fw-semibold">Supabase PostgreSQL</div>
                      <small className="text-muted">EF Core AppDbContext</small>
                    </div>
                    <span className={`badge ${health.database === 'Connected' ? 'bg-success' : 'bg-danger'}`}>
                      {health.database}
                    </span>
                  </div>

                  {health.error && (
                    <div className="list-group-item px-0 text-danger small">
                      <strong>DB Error:</strong> {health.error}
                    </div>
                  )}

                  <div className="list-group-item d-flex justify-content-between align-items-center px-0">
                    <span className="text-muted small">Server Time (UTC)</span>
                    <span className="text-muted small">{new Date(health.timestamp).toLocaleString()}</span>
                  </div>
                </div>
              )}

              <div className="d-grid mt-3">
                <button
                  className="btn btn-outline-primary"
                  onClick={checkHealth}
                  disabled={loading}
                >
                  {loading ? 'Testing...' : 'Re-check Connection'}
                </button>
              </div>
            </div>
            <div className="card-footer bg-light text-center py-2 text-muted small">
              ASP.NET Core Web API + Supabase PostgreSQL + React (Vite) + Bootstrap
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default App
