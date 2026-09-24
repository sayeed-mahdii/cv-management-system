import { supabase } from './supabaseClient'

/**
 * Universal API helper that attaches the Supabase JWT Bearer token to all requests
 */
async function getAuthHeaders() {
  const headers = {
    'Content-Type': 'application/json'
  }

  const { data: { session } } = await supabase.auth.getSession()
  if (session?.access_token) {
    headers['Authorization'] = `Bearer ${session.access_token}`
  }

  return headers
}

export const api = {
  async get(url) {
    const headers = await getAuthHeaders()
    const response = await fetch(url, { method: 'GET', headers })
    return handleResponse(response)
  },

  async post(url, data) {
    const headers = await getAuthHeaders()
    const response = await fetch(url, {
      method: 'POST',
      headers,
      body: JSON.stringify(data)
    })
    return handleResponse(response)
  },

  async put(url, data) {
    const headers = await getAuthHeaders()
    const response = await fetch(url, {
      method: 'PUT',
      headers,
      body: JSON.stringify(data)
    })
    return handleResponse(response)
  },

  async delete(url) {
    const headers = await getAuthHeaders()
    const response = await fetch(url, { method: 'DELETE', headers })
    return handleResponse(response)
  }
}

async function handleResponse(response) {
  if (response.status === 403) {
    const body = await response.json().catch(() => ({}))
    if (body.error === 'AccountBlocked') {
      window.dispatchEvent(new CustomEvent('account-blocked', { detail: body.message }))
    }
    const error = new Error(body.message || 'Access Forbidden')
    error.status = 403
    error.data = body
    throw error
  }

  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    const error = new Error(body.message || `HTTP error ${response.status}`)
    error.status = response.status
    error.data = body
    throw error
  }

  // Handle 204 No Content
  if (response.status === 204) {
    return null
  }

  return response.json()
}
