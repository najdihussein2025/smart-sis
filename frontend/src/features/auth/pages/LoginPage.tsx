import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link, Navigate, useLocation, useNavigate } from 'react-router'
import { useAuth } from '@/shared/auth/AuthProvider'
import { homeFor } from '@/shared/auth/session'
import { errorMessage } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { TextField } from '@/shared/ui/TextField'
import { useLogin } from '../hooks'

const schema = z.object({
  universityId: z
    .string()
    .trim()
    .min(1, 'Enter your University ID.')
    .regex(/^\d+$/, 'University ID contains numbers only.'),
  password: z.string().min(1, 'Enter your password.'),
})
type FormValues = z.infer<typeof schema>

export function LoginPage() {
  const { user, signIn } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const login = useLogin()

  const { register, handleSubmit, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { universityId: '', password: '' },
  })

  if (user) return <Navigate to={homeFor[user.role]} replace />

  const onSubmit = handleSubmit((values) =>
    login.mutate(
      { universityId: Number(values.universityId), password: values.password },
      {
        onSuccess: (session) => {
          signIn(session)
          const from = (location.state as { from?: string } | null)?.from
          navigate(from?.startsWith(homeFor[session.user.role]) ? from : homeFor[session.user.role], { replace: true })
        },
      },
    ),
  )

  // Rule from §3: the error clears as soon as the user types again.
  const clearServerError = () => {
    if (login.isError) login.reset()
  }

  return (
    <div>
      <h1 className="font-display text-3xl font-semibold tracking-tight">Sign in</h1>
      <p className="mt-2 text-muted">Use your University ID and password.</p>

      <form onSubmit={onSubmit} noValidate className="mt-8 space-y-5">
        {login.isError && <Alert>{errorMessage(login.error)}</Alert>}

        <TextField
          label="University ID"
          inputMode="numeric"
          autoComplete="username"
          autoFocus
          placeholder="e.g. 202301234"
          error={formState.errors.universityId?.message}
          {...register('universityId', { onChange: clearServerError })}
        />

        <div className="space-y-2">
          <TextField
            label="Password"
            type="password"
            autoComplete="current-password"
            error={formState.errors.password?.message}
            {...register('password', { onChange: clearServerError })}
          />
          <div className="text-right">
            <Link to="/forgot-password" className="text-sm font-medium text-brand hover:text-accent">
              Forgot password?
            </Link>
          </div>
        </div>

        <Button type="submit" className="w-full h-11" loading={login.isPending}>
          Sign in
        </Button>
      </form>
    </div>
  )
}
