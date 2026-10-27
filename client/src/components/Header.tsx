import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, NavLink, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { useLocalePath } from '../hooks/useLang'
import { CloseIcon, MenuIcon, NoteIcon, UserIcon } from './Icons'
import { LanguageSelect } from './LanguageSelect'

export function Header() {
  const { t } = useTranslation()
  const to = useLocalePath()
  const { user } = useAuth()
  const [open, setOpen] = useState(false)
  const { pathname } = useLocation()
  const [lastPath, setLastPath] = useState(pathname)
  const closeRef = useRef<HTMLButtonElement>(null)
  const drawerRef = useRef<HTMLDivElement>(null)

  // Close the drawer after navigating (adjust state during render, no effect needed).
  if (pathname !== lastPath) {
    setLastPath(pathname)
    setOpen(false)
  }

  useEffect(() => {
    if (!open) return
    closeRef.current?.focus()
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') return setOpen(false)
      if (e.key !== 'Tab' || !drawerRef.current) return
      // Keep focus inside the modal drawer.
      const items = drawerRef.current.querySelectorAll<HTMLElement>('a[href], button:not([disabled])')
      const first = items[0]
      const last = items[items.length - 1]
      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault()
        last?.focus()
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault()
        first?.focus()
      }
    }
    document.addEventListener('keydown', onKey)
    document.body.style.overflow = 'hidden'
    return () => {
      document.removeEventListener('keydown', onKey)
      document.body.style.overflow = ''
    }
  }, [open])

  const links = [
    { to: to(), label: t('nav.home'), end: true },
    { to: to('/explore'), label: t('nav.explore') },
    { to: to('/food-culture'), label: t('nav.food') },
    { to: to('/map'), label: t('nav.map') },
  ]

  const initial = user?.fullName?.trim()?.[0]?.toUpperCase()

  return (
    <header className="header">
      <a className="skip-link" href="#main">
        {t('nav.skip')}
      </a>
      <div className="container header-inner">
        <button
          type="button"
          className="icon-btn menu-btn"
          aria-label={t('nav.menu')}
          aria-expanded={open}
          aria-controls="drawer"
          onClick={() => setOpen(true)}
        >
          <MenuIcon size={26} />
        </button>

        <Link to={to()} className="brand">
          <span className="brand-mark">
            <NoteIcon size={18} />
          </span>
          TourVoice
        </Link>

        <nav className="nav-desktop" aria-label="Main">
          {links.map((l) => (
            <NavLink key={l.to} to={l.to} end={l.end}>
              {l.label}
            </NavLink>
          ))}
        </nav>

        <div className="header-actions">
          <LanguageSelect />
          <Link
            to={user ? to('/account') : to('/auth')}
            className="icon-btn"
            aria-label={user ? t('nav.account') : t('nav.signIn')}
            title={user ? user.fullName : t('nav.signIn')}
          >
            <span className="avatar">{initial ?? <UserIcon size={22} />}</span>
          </Link>
        </div>
      </div>

      {open && (
        <>
          <div className="drawer-backdrop" onClick={() => setOpen(false)} />
          <div id="drawer" ref={drawerRef} className="drawer" role="dialog" aria-modal="true" aria-label={t('nav.menu')}>
            <div className="drawer-head">
              <span className="brand">
                <span className="brand-mark">
                  <NoteIcon size={18} />
                </span>
                TourVoice
              </span>
              <button
                ref={closeRef}
                type="button"
                className="icon-btn"
                aria-label={t('nav.close')}
                onClick={() => setOpen(false)}
              >
                <CloseIcon />
              </button>
            </div>
            <nav aria-label="Main" className="drawer-nav">
              {links.map((l) => (
                <NavLink key={l.to} to={l.to} end={l.end} className="drawer-link">
                  {l.label}
                </NavLink>
              ))}
              <NavLink to={user ? to('/account') : to('/auth')} className="drawer-link">
                {user ? t('nav.account') : t('nav.signIn')}
              </NavLink>
            </nav>
          </div>
        </>
      )}
    </header>
  )
}
