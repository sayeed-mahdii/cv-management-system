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
  ru: {
    translation: {
      nav: {
        brand: 'reCruitoV',
        positions: 'Вакансии',
        myProfile: 'Мой профиль',
        myCvs: 'Мои резюме',
        candidates: 'Поиск кандидатов',
        attributes: 'Библиотека атрибутов',
        userManagement: 'Управление пользователями',
        searchPlaceholder: 'Поиск вакансий, проектов, кандидатов...',
        signIn: 'Войти',
        getStarted: 'Начать',
        preferences: 'Настройки',
        signOut: 'Выйти'
      },
      dashboard: {
        heroBadge: 'Современная платформа найма',
        heroTitle: 'Объединяем лучших специалистов с',
        heroTitleSpan: 'Ведущими командами',
        heroSubtitle: 'Находите актуальные вакансии, демонстрируйте проверенное портфолио и ускоряйте подбор с помощью автоматического подбора.',
        createCandidateAccount: 'Создать аккаунт кандидата',
        platformLiveStats: 'Обзор платформы',
        singleBatchQuery: 'Сводка активности в реальном времени',
        apps24h: 'Активность (24ч)',
        apps24hSub: 'Новые заявки',
        openVacancies: 'Открытые вакансии',
        openVacanciesSub: 'Активные предложения',
        candidates: 'Кандидаты',
        candidatesSub: 'Проверенные профили',
        recruiters: 'Рекрутеры',
        recruitersSub: 'Команды по найму',
        totalSubmissions: 'Всего откликов',
        totalSubmissionsSub: 'За все время',
        trendingSkills: 'Востребованные навыки и технологии',
        tagCloudBadge: 'Популярные навыки',
        tagCloudHint: 'Выберите навык, чтобы увидеть подходящие вакансии',
        latestPositions: 'Свежие вакансии',
        latestPositionsSub: 'Недавно опубликованные предложения',
        viewAllPositions: 'Все вакансии',
        top5Popular: 'Топ-5 популярных вакансий',
        top5PopularSub: 'По наибольшему числу откликов кандидатов',
        viewDetailsApply: 'Подробнее и откликнуться',
        rankMostPopular: '№1 по популярности'
      },
      admin: {
        title: 'Список пользователей и команды',
        subtitle: 'Управление учетными записями, настройка прав доступа и модерация платформы.',
        blockSelected: 'Заблокировать',
        unblockSelected: 'Разблокировать',
        manageRoles: 'Настроить роли',
        deleteSelected: 'Удалить',
        searchPlaceholder: 'Поиск по email, имени или роли...',
        allRoles: 'Все роли',
        selectedCount: 'Выбрано: {{count}}',
        showingUsers: 'Показано пользователей: {{count}}',
        tableUser: 'Пользователь',
        tableRoles: 'Роли',
        tableStatus: 'Статус',
        tableActivity: 'Активность',
        tableJoined: 'Дата регистрации',
        statusActive: 'Активен',
        statusBlocked: 'Заблокирован',
        projectsCount: 'Проектов: {{count}}',
        applicationsCount: 'Откликов: {{count}}',
        noUsersFound: 'Пользователи не найдены',
        confirmDeleteTitle: 'Удалить пользователя(ей)?',
        confirmDeleteDesc: 'Вы уверены, что хотите безвозвратно удалить {{count}} пользователя(ей)? Это действие удалит все связанные заявки и данные профиля.',
        confirmDeleteBtn: 'Подтвердить удаление',
        selfDemoteWarning: 'Внимание: вы редактируете свои роли. Снятие роли Администратора немедленно лишит вас доступа к этой панели.',
        rolesModalTitle: 'Настройка прав: {{email}}'
      },
      common: {
        cancel: 'Отмена',
        save: 'Сохранить изменения',
        saving: 'Сохранение...',
        close: 'Закрыть',
        refresh: 'Обновить',
        loading: 'Загрузка...',
        success: 'Успешно',
        error: 'Ошибка'
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
