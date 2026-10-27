import { useLocation, useNavigate } from 'react-router-dom'
import { isLang, type Lang } from '../lib/languages'

/** Swaps the `/:lang` segment of the current URL, keeping path, query and hash. */
export function useSwitchLang() {
  const navigate = useNavigate()
  const { pathname, search, hash } = useLocation()
  return (next: Lang) => {
    const parts = pathname.split('/')
    if (isLang(parts[1])) parts[1] = next
    else parts.splice(1, 0, next)
    navigate(`${parts.join('/')}${search}${hash}`, { replace: true })
  }
}
