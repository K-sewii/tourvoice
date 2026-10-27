import { useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate, useNavigate, useSearchParams } from 'react-router-dom'
import { ApiError } from '../api/http'
import { useAuth } from '../auth/AuthContext'
import { useLocalePath } from '../hooks/useLang'
import { safeReturn } from '../lib/safeReturn'

type Mode = 'signin' | 'signup'
type Errors = Partial<Record<'email' | 'password' | 'fullName' | 'form', string>>

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/


/** SCR-05 — /:lang/auth?returnUrl= */
export default function AuthPage() {
  const { t } = useTranslation()
  const to = useLocalePath()
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const { user, login, register } = useAuth()
  const returnUrl = safeReturn(params.get('returnUrl')) ?? to('/account')

  const [mode, setMode] = useState<Mode>('signin')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [fullName, setFullName] = useState('')
  const [errors, setErrors] = useState<Errors>({})
  const [submitting, setSubmitting] = useState(false)

  if (user) return <Navigate to={returnUrl} replace />

  const validate = (): Errors => {
    const e: Errors = {}
    if (!EMAIL_RE.test(email.trim())) e.email = t('auth.errEmail')
    if (password.length < 8) e.password = t('auth.errPassword')
    if (mode === 'signup' && !fullName.trim()) e.fullName = t('auth.errName')
    return e
  }

  const onSubmit = async (ev: FormEvent) => {
    ev.preventDefault()
    const e = validate()
    setErrors(e)
    if (Object.keys(e).length) return
    setSubmitting(true)
    try {
      if (mode === 'signin') await login(email.trim(), password)
      else await register(email.trim(), password, fullName.trim())
      navigate(returnUrl, { replace: true })
    } catch (err) {
      const status = err instanceof ApiError ? err.status : 0
      setErrors({
        form:
          status === 401
            ? t('auth.errInvalid')
            : status === 409
              ? t('auth.errExists')
              : status === 403
                ? t('auth.errLocked')
                : t('common.error'),
      })
    } finally {
      setSubmitting(false)
    }
  }

  const switchMode = (m: Mode) => {
    setMode(m)
    setErrors({})
  }

  return (
    <div className="container">
      <div className="auth-card">
        <h1 className="visually-hidden">{mode === 'signin' ? t('auth.signIn') : t('auth.signUp')}</h1>
        <div className="tabs" role="tablist">
          {(['signin', 'signup'] as const).map((m) => (
            <button
              key={m}
              type="button"
              role="tab"
              aria-selected={mode === m}
              aria-controls="auth-form"
              onClick={() => switchMode(m)}
            >
              {m === 'signin' ? t('auth.signIn') : t('auth.signUp')}
            </button>
          ))}
        </div>
        <p className="card-meta" style={{ marginBottom: 20 }}>
          {t('auth.intro')}
        </p>

        <form id="auth-form" onSubmit={onSubmit} noValidate>
          {mode === 'signup' && (
            <Field
              id="fullName"
              label={t('auth.fullName')}
              value={fullName}
              onChange={setFullName}
              error={errors.fullName}
              autoComplete="name"
            />
          )}
          <Field
            id="email"
            label={t('auth.email')}
            type="email"
            value={email}
            onChange={setEmail}
            error={errors.email}
            autoComplete="email"
          />
          <Field
            id="password"
            label={t('auth.password')}
            type="password"
            value={password}
            onChange={setPassword}
            error={errors.password}
            autoComplete={mode === 'signin' ? 'current-password' : 'new-password'}
          />
          {errors.form && (
            <p className="notice error" role="alert" style={{ marginBottom: 16 }}>
              {errors.form}
            </p>
          )}
          <button type="submit" className="btn btn-primary btn-lg btn-block" disabled={submitting}>
            {mode === 'signin' ? t('auth.submitSignIn') : t('auth.submitSignUp')}
          </button>
        </form>
      </div>
    </div>
  )
}

interface FieldProps {
  id: string
  label: string
  value: string
  onChange: (v: string) => void
  error?: string
  type?: string
  autoComplete?: string
}

function Field({ id, label, value, onChange, error, type = 'text', autoComplete }: FieldProps) {
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <input
        id={id}
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        autoComplete={autoComplete}
        aria-invalid={!!error}
        aria-describedby={error ? `${id}-err` : undefined}
      />
      {error && (
        <span id={`${id}-err`} className="error-text">
          {error}
        </span>
      )}
    </div>
  )
}
