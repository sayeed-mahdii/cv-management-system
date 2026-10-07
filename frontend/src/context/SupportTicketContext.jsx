import React, { createContext, useContext, useState, useCallback } from 'react'
import { SupportTicketModal } from '../components/SupportTicketModal'

const SupportTicketContext = createContext(null)

export function SupportTicketProvider({ children }) {
  const [isOpen, setIsOpen] = useState(false)
  const [contextData, setContextData] = useState({})

  const openTicketModal = useCallback((data = {}) => {
    setContextData(data || {})
    setIsOpen(true)
  }, [])

  const closeTicketModal = useCallback(() => {
    setIsOpen(false)
  }, [])

  return (
    <SupportTicketContext.Provider value={{ openTicketModal, closeTicketModal, isOpen }}>
      {children}
      <SupportTicketModal
        show={isOpen}
        onHide={closeTicketModal}
        initialContext={contextData}
      />
    </SupportTicketContext.Provider>
  )
}

export function useSupportTicket() {
  const ctx = useContext(SupportTicketContext)
  if (!ctx) {
    throw new Error('useSupportTicket must be used within a SupportTicketProvider')
  }
  return ctx
}
