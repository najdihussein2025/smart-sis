import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { errorMessage } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { TextField } from '@/shared/ui/TextField'
import { useResetPassword } from '../hooks'

const schema = z
  .object({
    newPassword: z
      .string()
      .min(8, 'Use at least 8 characters.')
      .regex(/[A-Za-z]/, 'Include at least one letter.')
      .regex(/[0-9]/, 'Include at least one number.'),
    confirm: z.string(),
  })
  .refine((v) => v.newPassword === v.confirm, { path: ['confirm'], message: "Passwords don't match." })
type FormValues = z.infer<typeof schema>

export function ResetPasswordPage() {
  const [params] = useSearchParams()
  const token = params.get('token') ?? ''
  const navigate = useNavigate()
  const reset = useResetPassword()
  const { register, handleSubmit, formState } = useForm<FormValues>({ resolver: zodResolver(schema) })

  if (!token) {
    return (
      <div className="space-y-6">
        <h1 className="font-display text-3xl font-semibold tracking-tight">Link not valid</h1>
        <Alert>This reset link is incomplete. Request a new one.</Alert>
        <Link to="/forgot-password" className="text-sm font-medium text-brand hover:text-accent">
          Request a new link
        </Link>
      </div>
    )
  }

  return (
    <div>
      <h1 className="font-display text-3xl font-semibold tracking-tight">Set a new password</h1>
      <p className="mt-2 text-muted">At least 8 characters with a letter and a number.</p>

      <form
        noValidate
        className="mt-8 space-y-5"
        onSubmit={handleSubmit((v) =>
          reset.mutate(
            { token, newPassword: v.newPassword },
            { onSuccess: () => navigate('/login', { replace: true, state: { passwordReset: true } }) },
          ),
        )}
      >
        {reset.isError && <Alert>{errorMessage(reset.error)}</Alert>}
        <TextField label="New password" type="password" autoComplete="new-password" autoFocus error={formState.errors.newPassword?.message} {...register('newPassword')} />
        <TextField label="Confirm password" type="password" autoComplete="new-password" error={formState.errors.confirm?.message} {...register('confirm')} />
        <Button type="submit" className="w-full h-11" loading={reset.isPending}>
          Save password
        </Button>
      </form>
    </div>
  )
}
