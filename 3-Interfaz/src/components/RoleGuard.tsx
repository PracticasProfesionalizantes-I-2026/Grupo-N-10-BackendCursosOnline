import { Navigate, Outlet, useLocation } from 'react-router-dom'
import type { Role } from '@/types'
import { useApp } from '@/hooks/useApp'

export function RoleGuard({ role }: { role: Role }) {
  const { currentUser } = useApp()
  const location = useLocation()

  if (!currentUser) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }
  if (currentUser.role !== role) {
    const routeByRole: Record<Role, string> = {
      ALUMNO: '/student',
      PROFESOR: '/teacher',
      ADMINISTRADOR: '/admin',
    }
    return <Navigate to={routeByRole[currentUser.role]} replace />
  }
  return <Outlet />
}
