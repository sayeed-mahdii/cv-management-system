import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'

const resources = {
  en: {
    translation: {
      nav: {
        brand: 'reCruitoV',
        positions: 'Positions',
        myProfile: 'My Profile',
        myCvs: 'My CVs',
        candidates: 'Candidate Search',
        attributes: 'Attribute Library',
        userManagement: 'User Management',
        searchPlaceholder: 'Search positions, projects, candidates...',
        signIn: 'Sign In',
        getStarted: 'Get Started',
        preferences: 'Preferences',
        signOut: 'Sign Out'
      },
      dashboard: {
        heroBadge: 'The Modern Hiring Platform',
        heroTitle: 'Connecting Exceptional Talent with',
        heroTitleSpan: 'Leading Teams',
        heroSubtitle: 'Discover curated vacancies, showcase verified portfolios, and streamline recruitment with automated precision matching.',
        createCandidateAccount: 'Create Candidate Account',
        platformLiveStats: 'Platform Overview',
        singleBatchQuery: 'Live activity summary',
        apps24h: '24h Activity',
        apps24hSub: 'Recent submissions',
        openVacancies: 'Open Vacancies',
        openVacanciesSub: 'Active job listings',
        candidates: 'Candidates',
        candidatesSub: 'Verified talent profiles',
        recruiters: 'Recruiters',
        recruitersSub: 'Hiring teams',
        totalSubmissions: 'Total Applications',
        totalSubmissionsSub: 'All-time volume',
        trendingSkills: 'In-Demand Skills & Technologies',
        tagCloudBadge: 'Popular Skills',
        tagCloudHint: 'Select a skill to explore matching openings',
        latestPositions: 'Latest Positions',
        latestPositionsSub: 'Recently opened job opportunities',
        viewAllPositions: 'View All Positions',
        top5Popular: 'Top 5 Popular Positions',
        top5PopularSub: 'Ranked by highest number of candidate applications',
        viewDetailsApply: 'View Details & Apply',
        rankMostPopular: '#1 Most Popular'
      },
      admin: {
        title: 'Team & User Directory',
        subtitle: 'Manage member accounts, configure access roles, and moderate platform activity.',
        blockSelected: 'Block Selected',
        unblockSelected: 'Unblock Selected',
        manageRoles: 'Manage Roles',
        deleteSelected: 'Delete User(s)',
        searchPlaceholder: 'Search by email, name, or role...',
        allRoles: 'All Roles',
        selectedCount: '{{count}} selected',
        showingUsers: 'Showing {{count}} user(s)',
        tableUser: 'Member',
        tableRoles: 'Assigned Roles',
        tableStatus: 'Status',
        tableActivity: 'Activity',
        tableJoined: 'Joined Date',
        statusActive: 'Active',
        statusBlocked: 'Suspended',
        projectsCount: '{{count}} projects',
        applicationsCount: '{{count}} applications',
        noUsersFound: 'No users found matching your search',
        confirmDeleteTitle: 'Delete User(s)?',
        confirmDeleteDesc: 'Are you sure you want to permanently delete {{count}} user(s)? This will cascade delete associated applications and profile records.',
        confirmDeleteBtn: 'Confirm Delete',
        selfDemoteWarning: 'Notice: You are modifying your own roles. If you remove Administrator, you will immediately lose access to this admin directory.',
        rolesModalTitle: 'Configure Access: {{email}}'
      },
      common: {
        cancel: 'Cancel',
        save: 'Save Changes',
        saving: 'Saving...',
        close: 'Close',
        refresh: 'Refresh',
        loading: 'Loading...',
        success: 'Success',
        error: 'Error'
      }
    }
  },
  es: {
    translation: {
      nav: {
        brand: 'reCruitoV',
        positions: 'Vacantes',
        myProfile: 'Mi Perfil',
        myCvs: 'Mis CVs',
        candidates: 'Buscar Candidatos',
        attributes: 'Biblioteca de Atributos',
        userManagement: 'Gestión de Usuarios',
        searchPlaceholder: 'Buscar vacantes, proyectos, candidatos...',
        signIn: 'Iniciar Sesión',
        getStarted: 'Comenzar',
        preferences: 'Preferencias',
        signOut: 'Cerrar Sesión'
      },
      dashboard: {
        heroBadge: 'La Plataforma Moderna de Empleo',
        heroTitle: 'Conectando Talento Excepcional con',
        heroTitleSpan: 'Grandes Equipos',
        heroSubtitle: 'Descubra vacantes seleccionadas, exhiba portafolios verificados y optimice el reclutamiento con evaluación de precisión.',
        createCandidateAccount: 'Crear Cuenta de Candidato',
        platformLiveStats: 'Resumen de la Plataforma',
        singleBatchQuery: 'Resumen de actividad en vivo',
        apps24h: 'Actividad (24h)',
        apps24hSub: 'Envíos recientes',
        openVacancies: 'Vacantes Abiertas',
        openVacanciesSub: 'Ofertas activas',
        candidates: 'Candidatos',
        candidatesSub: 'Perfiles verificados',
        recruiters: 'Reclutadores',
        recruitersSub: 'Equipos de selección',
        totalSubmissions: 'Total de Postulaciones',
        totalSubmissionsSub: 'Volumen histórico',
        trendingSkills: 'Habilidades y Tecnologías en Tendencia',
        tagCloudBadge: 'Habilidades Populares',
        tagCloudHint: 'Seleccione una habilidad para ver vacantes',
        latestPositions: 'Últimas Vacantes',
        latestPositionsSub: 'Oportunidades laborales recientemente publicadas',
        viewAllPositions: 'Ver Todas las Vacantes',
        top5Popular: 'Top 5 Vacantes Más Populares',
        top5PopularSub: 'Clasificadas por mayor volumen de postulaciones',
        viewDetailsApply: 'Ver Detalles y Postularse',
        rankMostPopular: '#1 Más Popular'
      },
      admin: {
        title: 'Directorio de Usuarios y Equipo',
        subtitle: 'Administre cuentas de miembros, configure roles de acceso y modere la actividad.',
        blockSelected: 'Bloquear Seleccionados',
        unblockSelected: 'Desbloquear Seleccionados',
        manageRoles: 'Gestionar Roles',
        deleteSelected: 'Eliminar Usuario(s)',
        searchPlaceholder: 'Buscar por correo, nombre o rol...',
        allRoles: 'Todos los Roles',
        selectedCount: '{{count}} seleccionados',
        showingUsers: 'Mostrando {{count}} usuario(s)',
        tableUser: 'Miembro',
        tableRoles: 'Roles Asignados',
        tableStatus: 'Estado',
        tableActivity: 'Actividad',
        tableJoined: 'Fecha de Registro',
        statusActive: 'Activo',
        statusBlocked: 'Suspendido',
        projectsCount: '{{count}} proyectos',
        applicationsCount: '{{count}} postulaciones',
        noUsersFound: 'No se encontraron usuarios',
        confirmDeleteTitle: '¿Eliminar Usuario(s)?',
        confirmDeleteDesc: '¿Está seguro de eliminar permanentemente a {{count}} usuario(s)? Esto eliminará en cascada las postulaciones y registros de perfil asociados.',
        confirmDeleteBtn: 'Confirmar Eliminación',
        selfDemoteWarning: 'Aviso: Está modificando sus propios roles. Si elimina Administrador, perderá acceso a este directorio inmediatamente.',
        rolesModalTitle: 'Configurar Acceso: {{email}}'
      },
      common: {
        cancel: 'Cancelar',
        save: 'Guardar Cambios',
        saving: 'Guardando...',
        close: 'Cerrar',
        refresh: 'Actualizar',
        loading: 'Cargando...',
        success: 'Éxito',
        error: 'Error'
      }
    }
  }
}

const savedLanguage = localStorage.getItem('recruitov_language') || 'en'

i18n
  .use(initReactI18next)
  .init({
    resources,
    lng: savedLanguage,
    fallbackLng: 'en',
    interpolation: {
      escapeValue: false
    }
  })

export default i18n
