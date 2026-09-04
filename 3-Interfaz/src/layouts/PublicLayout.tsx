import { Outlet } from 'react-router-dom'
import { Toaster } from 'sonner'

export function PublicLayout() {
  return (
    <>
      <Outlet />
      <Toaster richColors theme="dark" position="top-right" />
    </>
  )
}
