import React from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import 'bootstrap/dist/css/bootstrap.min.css'
import './index.css'

import { AuthProvider } from './context/AuthContext'
import { AppNavbar } from './components/Navbar'
import { ProtectedRoute } from './components/ProtectedRoute'
import { HomePage } from './pages/HomePage'
import { PositionsPage } from './pages/PositionsPage'
import { SettingsPage } from './pages/SettingsPage'
import { AttributesPage } from './pages/AttributesPage'
import { ProfilePage } from './pages/ProfilePage'
import { SearchResultsPage } from './pages/SearchResultsPage'
import { AdminUsersPage } from './pages/AdminUsersPage'

function ProfilePlaceholder() {
  return (
    <div className="container py-5 text-center">
      <h3 className="fw-bold mb-3">User Profile (Virtual CV)</h3>
      <p className="text-muted">
        Profile builder (Me attributes, dynamic EAV values, Cloudinary photo upload, and projects) is scheduled for Phase 6.
      </p>
    </div>
  )
}

function AttributesPlaceholder() {
  return (
    <div className="container py-5 text-center">
      <h3 className="fw-bold mb-3">Attribute Library</h3>
      <p className="text-muted">
        Attribute Library (full table toolbar, types, and search) is scheduled for Phase 5.
      </p>
    </div>
  )
}


function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <div className="d-flex flex-column min-vh-100">
          <AppNavbar />

          <main className="flex-grow-1">
            <Routes>
              <Route path="/" element={<HomePage />} />
              <Route path="/positions" element={<PositionsPage />} />
              <Route path="/search" element={<SearchResultsPage />} />
              
              {/* Authenticated routes */}
              <Route
                path="/settings"
                element={
                  <ProtectedRoute>
                    <SettingsPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/profile"
                element={
                  <ProtectedRoute>
                    <ProfilePage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/profile/:id"
                element={
                  <ProtectedRoute>
                    <ProfilePage />
                  </ProtectedRoute>
                }
              />

              {/* Recruiter & Admin routes */}
              <Route
                path="/attributes"
                element={
                  <ProtectedRoute allowedRoles={['Recruiter', 'Administrator']}>
                    <AttributesPage />
                  </ProtectedRoute>
                }
              />

              {/* Admin only routes */}
              <Route
                path="/admin/users"
                element={
                  <ProtectedRoute allowedRoles={['Administrator']}>
                    <AdminUsersPage />
                  </ProtectedRoute>
                }
              />

              {/* Fallback */}
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </main>

          <footer className="py-4 border-top text-muted small glass-panel mt-auto">
            <div className="container d-flex flex-column flex-md-row justify-content-between align-items-center gap-3">
              <div className="d-flex align-items-center gap-2">
                <span className="fw-bold fs-5 tracking-tight text-body">
                  re<span className="text-primary">C</span>ruito<span className="text-primary">V</span>
                </span>
                <span className="text-muted ms-2">&copy; {new Date().getFullYear()} reCruitoV Platform. All rights reserved.</span>
              </div>
              <div className="d-flex align-items-center gap-3 text-muted">
                <span className="text-success d-inline-flex align-items-center gap-1">
                  <span className="rounded-circle bg-success d-inline-block" style={{ width: '8px', height: '8px' }}></span>
                  All Systems Operational
                </span>
              </div>
            </div>
          </footer>
        </div>
      </BrowserRouter>
    </AuthProvider>
  )
}

export default App
