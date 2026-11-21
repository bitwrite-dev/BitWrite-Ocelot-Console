import { useState } from 'react'
import { useNavigate } from 'react-router-dom'

import { useApi } from '@/app-providers'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAuth } from '@/app-providers'
import { ApiError } from '@/api/errors'

export function LoginPage() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const { http } = useApi()
  const { login } = useAuth()
  const navigate = useNavigate()

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setError(null)
    setIsLoading(true)

    try {
      const response = await http.post<{
        accessToken: string
        refreshToken: string
        expiresIn: number
        userId: string
        username: string
        displayName: string | null
        email: string | null
      }>('/api/v1/auth/login', { username, password })

      login(response.accessToken)

      // Persist token so a page reload keeps the user signed in (UI-09).
      try {
        window.localStorage.setItem('dashboard:token', response.accessToken)
      } catch {
        // localStorage refuses in some privacy configs; memory token still works.
      }

      navigate('/', { replace: true })
    } catch (err: unknown) {
      setIsLoading(false)

      if (err instanceof ApiError) {
        setError(err.isUnauthorized ? 'Invalid username or password' : err.message)
      } else if (err instanceof Error) {
        setError(err.message)
      } else {
        setError('Login failed. Please try again.')
      }
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">Sign in to BitWrite</CardTitle>
          <CardDescription>
            Enter your credentials to access the console.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="username">Username</Label>
              <Input
                id="username"
                type="text"
                placeholder="Enter your username"
                value={username}
                onChange={(event) => setUsername(event.currentTarget.value)}
                disabled={isLoading}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                placeholder="Enter your password"
                value={password}
                onChange={(event) => setPassword(event.currentTarget.value)}
                disabled={isLoading}
                required
              />
            </div>

            {error && (
              <div className="rounded-md bg-red-50 p-3 text-sm text-red-700">
                {error}
              </div>
            )}

            <Button
              type="submit"
              className="w-full"
              disabled={isLoading}
            >
              {isLoading ? 'Signing in…' : 'Sign in'}
            </Button>
          </form>
        </CardContent>
        <CardFooter className="flex flex-col gap-2 text-center">
          <p className="text-sm text-muted-foreground">
            Don't have an account? Contact your administrator.
          </p>
        </CardFooter>
      </Card>
    </div>
  )
}
