import { createClient } from '@supabase/supabase-js'

const supabaseUrl = import.meta.env.VITE_SUPABASE_URL || 'https://okxxoowiiwycpukzrgwc.supabase.co'
const supabaseAnonKey = import.meta.env.VITE_SUPABASE_ANON_KEY || ''

if (!supabaseAnonKey) {
  console.warn(
    'Supabase anon key is missing! Please configure VITE_SUPABASE_ANON_KEY in frontend/.env.local.'
  )
}

export const isSupabaseConfigured = Boolean(supabaseUrl && supabaseAnonKey)

// Create client or fallback dummy client to prevent runtime crash before key configuration
export const supabase = isSupabaseConfigured
  ? createClient(supabaseUrl, supabaseAnonKey)
  : createClient(supabaseUrl, 'placeholder-anon-key-configure-env-local')
