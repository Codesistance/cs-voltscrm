import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Zap } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ApiError } from '@/shared/api/http'
import { applyServerErrors } from '@/shared/lib/errors'
import { AnimatedBackground } from '../components/AnimatedBackground'
import { useAuth } from '../AuthContext'
import { loginSchema, type LoginInput } from '../schema'

export function LoginPage() {
  const { status, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation() as { state?: { from?: { pathname?: string } } }
  const from = location.state?.from?.pathname ?? '/dashboard'

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginInput>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  })

  if (status === 'authenticated') return <Navigate to={from} replace />

  const onSubmit = handleSubmit(async (values) => {
    try {
      await login(values.email, values.password)
      navigate(from, { replace: true })
    } catch (error) {
      // Login isn't a TanStack mutation, so the global error toast doesn't cover it —
      // surface field errors inline and everything else (bad credentials, network, etc.) as a toast.
      if (applyServerErrors(error, setError)) return
      toast.error(
        error instanceof ApiError && error.status === 401
          ? "That email and password don't match — give it another go."
          : error instanceof ApiError
            ? error.message
            : "Couldn't sign you in just now — mind trying again?",
      )
    }
  })

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden p-4">
      <AnimatedBackground />
      <Card className="w-full max-w-sm border-white/15 bg-white/10 text-white shadow-2xl backdrop-blur-xl">
        <CardHeader className="space-y-1 text-center">
          <div className="mx-auto flex size-10 items-center justify-center rounded-md bg-primary text-primary-foreground">
            <Zap className="size-5" />
          </div>
          <CardTitle className="text-xl text-white">Sign in to VoltsCRM</CardTitle>
          <CardDescription className="text-white/70">Enter your credentials to continue</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={onSubmit} className="space-y-4" noValidate>
            <div className="space-y-2">
              <Label htmlFor="email" className="text-white/90">
                Email
              </Label>
              <Input
                id="email"
                type="email"
                autoComplete="username"
                aria-invalid={!!errors.email}
                className="border-white/20 bg-white/10 text-white placeholder:text-white/40 focus-visible:ring-white/40"
                {...register('email')}
              />
              {errors.email && <p className="text-sm text-red-300">{errors.email.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="password" className="text-white/90">
                Password
              </Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                aria-invalid={!!errors.password}
                className="border-white/20 bg-white/10 text-white placeholder:text-white/40 focus-visible:ring-white/40"
                {...register('password')}
              />
              {errors.password && <p className="text-sm text-red-300">{errors.password.message}</p>}
            </div>
            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? 'Signing in…' : 'Sign in'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
