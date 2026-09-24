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

function AdminUsersPlaceholder() {
  return (
    <div className="container py-5 text-center">
      <h3 className="fw-bold mb-3">User Management</h3>
      <p className="text-muted">
        Admin user list, role promotion/demotion, and account blocking is scheduled for Phase 9.
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
                    <AdminUsersPlaceholder />
                  </ProtectedRoute>
                }
              />

              {/* Fallback */}
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </main>

          <footer className="py-4 border-top text-center text-muted small glass-panel mt-auto">
            <div className="container">
              <p className="mb-1">
                <strong>reCruitoV</strong> &mdash; Web-Based CV &amp; Candidate Management System
              </p>
              <p className="mb-0 text-dimmed">
                ASP.NET Core Web API (.NET 10) &middot; Supabase PostgreSQL &middot; React 19 + Bootstrap
              </p>
            </div>
          </footer>
        </div>
      </BrowserRouter>
    </AuthProvider>
  )
}

export default App
