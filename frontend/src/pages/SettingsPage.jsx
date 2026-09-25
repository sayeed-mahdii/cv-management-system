import React, { useState } from 'react'
import { Container, Card, Form, Button, Alert } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useAuth } from '../context/AuthContext'

export function SettingsPage() {
  const { t, i18n } = useTranslation()
  const { profile, theme, updatePreferences, toggleTheme } = useAuth()
  const [selectedLanguage, setSelectedLanguage] = useState(
    profile?.preferredLanguage || localStorage.getItem('recruitov_language') || i18n.language || 'en'
  )
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState(null)

  const handleSave = async (e) => {
    e.preventDefault()
    setSaving(true)
    setSaved(false)
    setError(null)
    try {
      i18n.changeLanguage(selectedLanguage)
      localStorage.setItem('recruitov_language', selectedLanguage)
      await updatePreferences({
        preferredLanguage: selectedLanguage,
        preferredTheme: theme
      })
      setSaved(true)
      setTimeout(() => setSaved(false), 3000)
    } catch (err) {
      setError(err.message || 'Failed to update preferences.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Container className="py-5" style={{ maxWidth: '640px' }}>
      <h3 className="fw-bold mb-1">Account Preferences</h3>
      <p className="text-muted small mb-4">
        Customize your display theme and regional language settings.
      </p>

      {saved && (
        <Alert variant="success" className="small py-2">
          <i className="bi bi-check-circle-fill me-2"></i>
          Preferences saved successfully!
        </Alert>
      )}

      {error && (
        <Alert variant="danger" className="small py-2">
          <i className="bi bi-x-circle-fill me-2"></i>
          {error}
        </Alert>
      )}

      <Card className="card-pro p-4">
        <Form onSubmit={handleSave}>
          <Form.Group className="mb-4">
            <Form.Label className="fw-semibold">Theme Mode</Form.Label>
            <div className="d-flex gap-3">
              <button
                type="button"
                className={`btn flex-fill py-3 d-flex flex-column align-items-center gap-2 ${
                  theme === 'light' ? 'btn-primary' : 'btn-outline-secondary'
                }`}
                onClick={() => {
                  if (theme !== 'light') toggleTheme()
                }}
              >
                <i className="bi bi-sun-fill fs-4"></i>
                <span className="small fw-semibold">Light Mode</span>
              </button>
              <button
                type="button"
                className={`btn flex-fill py-3 d-flex flex-column align-items-center gap-2 ${
                  theme === 'dark' ? 'btn-primary' : 'btn-outline-secondary'
                }`}
                onClick={() => {
                  if (theme !== 'dark') toggleTheme()
                }}
              >
                <i className="bi bi-moon-stars-fill fs-4"></i>
                <span className="small fw-semibold">Dark Mode</span>
              </button>
            </div>
          </Form.Group>

          <Form.Group className="mb-4">
            <Form.Label className="fw-semibold">Interface Language</Form.Label>
            <Form.Select
              value={selectedLanguage}
              onChange={(e) => setSelectedLanguage(e.target.value)}
              className="form-control-pro"
            >
              <option value="en">English (US)</option>
              <option value="ru">Русский (Russian)</option>
            </Form.Select>
            <Form.Text className="text-muted small">
              Select your preferred language for navigation, dashboard, and administration.
            </Form.Text>
          </Form.Group>

          <div className="d-flex justify-content-end">
            <Button type="submit" className="btn btn-primary-gradient px-4" disabled={saving}>
              {saving ? 'Saving...' : 'Save Preferences'}
            </Button>
          </div>
        </Form>
      </Card>
    </Container>
  )
}
