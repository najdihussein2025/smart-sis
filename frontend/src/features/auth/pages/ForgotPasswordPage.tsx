import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link } from 'react-router'
import { ArrowLeft, MailCheck } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { TextField } from '@/shared/ui/TextField'
import { useForgotPassword } from '../hooks'

const schema = z.object({
  universityId: z.string().trim().min(1, 'Enter your University ID.').regex(/^\d+$/, 'University ID contains numbers only.'),
})
type FormValues = z.infer<typeof schema>

export function ForgotPasswordPage() {
  const forgot = useForgotPassword()
  const { register, handleSubmit, formState } = useForm<FormValues>({ resolver: zodResolver(schema) })

  if (forgot.isSuccess) {
    return (
      <div>
        <span className="grid size-12 place-items-center rounded-full bg-success-soft text-success">
          <MailCheck className="size-6" aria-hidden />
        </span>
        <h1 className="mt-5 font-display text-3xl font-semibold tracking-tight">Check your email</h1>
        <p className="mt-2 text-muted">
          If that University ID has an account, we sent a link to reset the password. The link works for 30 minutes.
        </p>
        <Link to="/login" className="mt-8 inline-flex items-center gap-2 text-sm font-medium text-brand hover:text-accent">
          <ArrowLeft className="size-4" aria-hidden /> Back to sign in
        </Link>
      </div>
    )
  }

  return (
    <div>
      <Link to="/login" className="inline-flex items-center gap-2 text-sm text-muted hover:text-ink">
        <ArrowLeft className="size-4" aria-hidden /> Sign in
      </Link>
      <h1 className="mt-6 font-display text-3xl font-semibold tracking-tight">Forgot password</h1>
      <p className="mt-2 text-muted">Enter your University ID and we'll email you a reset link.</p>

      <form
        onSubmit={handleSubmit((v) => forgot.mutate(Number(v.universityId)))}
        noValidate
        className="mt-8 space-y-5"
      >
        {forgot.isError && <Alert>{errorMessage(forgot.error)}</Alert>}
        <TextField
          label="University ID"
          inputMode="numeric"
          autoFocus
          error={formState.errors.universityId?.message}
          {...register('universityId')}
        />
        <Button type="submit" className="w-full h-11" loading={forgot.isPending}>
          Send reset link
        </Button>
      </form>
    </div>
  )
}
