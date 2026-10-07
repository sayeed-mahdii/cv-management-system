import React, { useState } from 'react'
import { Modal, Button, Form, Alert, Spinner } from 'react-bootstrap'
import { api } from '../services/api'

export function SupportTicketModal({ show, onHide, initialContext = {} }) {
  const [summary, setSummary] = useState('')
  const [priority, setPriority] = useState('Moderate')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)
  const [success, setSuccess] = useState(false)

  const handleClose = () => {
    if (submitting) return
    setError(null)
    setSuccess(false)
    setSummary('')
    setPriority('Moderate')
    onHide()
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (!summary.trim()) {
      setError('Please provide a summary of your inquiry or issue.')
      return
    }

    setSubmitting(true)
    setError(null)

    try {
      // Determine position context if on a position-related page
      let detectedPosition = initialContext?.positionTitle || ''
      if (!detectedPosition && window.location.pathname.startsWith('/positions')) {
        detectedPosition = 'Positions Directory'
      }

      const payload = {
        summary: summary.trim(),
        priority: priority,
        positionTitle: detectedPosition || 'N/A',
        link: window.location.href
      }

      const res = await api.post('/api/support-ticket', payload)
      if (res && res.uploadedToCloud === false) {
        setError(res.message || 'Failed to upload ticket to cloud storage.')
        return
      }

      setSuccess(true)
      setSummary('')
      setTimeout(() => {
        handleClose()
      }, 2500)
    } catch (err) {
      setError(err.message || 'Failed to submit support ticket. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal show={show} onHide={handleClose} centered backdrop="static">
      <Modal.Header closeButton className="border-bottom">
        <div className="d-flex align-items-center gap-2">
          <div
            className="rounded-circle d-flex align-items-center justify-content-center text-white"
            style={{ width: '36px', height: '36px', background: 'linear-gradient(135deg, #0ea5e9 0%, #3b82f6 100%)' }}
          >
            <i className="bi bi-headset fs-5"></i>
          </div>
          <Modal.Title className="fs-5 fw-bold mb-0">Create Support Ticket</Modal.Title>
        </div>
      </Modal.Header>

      <Modal.Body className="py-3">
        {error && (
          <Alert variant="danger" dismissible onClose={() => setError(null)} className="py-2 small">
            <i className="bi bi-exclamation-triangle-fill me-2"></i>
            {error}
          </Alert>
        )}

        {success ? (
          <Alert variant="success" className="py-4 text-center mb-0">
            <i className="bi bi-check-circle-fill fs-2 text-success d-block mb-2"></i>
            <h6 className="fw-bold mb-1">Support Ticket Submitted!</h6>
            <p className="small text-muted mb-0">
              Our team and administrators have been notified. We will review your request promptly.
            </p>
          </Alert>
        ) : (
          <Form onSubmit={handleSubmit}>
            <Form.Group className="mb-3">
              <Form.Label className="fw-semibold small">
                Priority <span className="text-danger">*</span>
              </Form.Label>
              <Form.Select
                value={priority}
                onChange={(e) => setPriority(e.target.value)}
                disabled={submitting}
              >
                <option value="High">High</option>
                <option value="Moderate">Moderate</option>
                <option value="Low">Low</option>
              </Form.Select>
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label className="fw-semibold small">
                Summary <span className="text-danger">*</span>
              </Form.Label>
              <Form.Control
                as="textarea"
                rows={4}
                value={summary}
                onChange={(e) => setSummary(e.target.value)}
                placeholder="Describe your issue, question, or feedback..."
                disabled={submitting}
                required
              />
            </Form.Group>

            <div className="d-flex justify-content-end gap-2 pt-2">
              <Button variant="secondary" onClick={handleClose} disabled={submitting}>
                Cancel
              </Button>
              <Button
                type="submit"
                variant="primary"
                className="btn-primary-gradient px-4"
                disabled={submitting || !summary.trim()}
              >
                {submitting ? (
                  <>
                    <Spinner animation="border" size="sm" className="me-2" />
                    Submitting...
                  </>
                ) : (
                  <>
                    <i className="bi bi-send-fill me-1"></i> Submit Ticket
                  </>
                )}
              </Button>
            </div>
          </Form>
        )}
      </Modal.Body>
    </Modal>
  )
}
