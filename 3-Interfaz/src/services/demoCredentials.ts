import type { User } from '@/types'

const hex = (bytes: Uint8Array) =>
  Array.from(bytes, (byte) => byte.toString(16).padStart(2, '0')).join('')

async function digest(password: string, salt: Uint8Array<ArrayBuffer>) {
  const key = await crypto.subtle.importKey(
    'raw', new TextEncoder().encode(password), 'PBKDF2', false, ['deriveBits'],
  )
  const hash = await crypto.subtle.deriveBits(
    { name: 'PBKDF2', hash: 'SHA-256', salt, iterations: 600_000 }, key, 256,
  )
  return hex(new Uint8Array(hash))
}

export async function createDemoCredential(password: string) {
  const salt = crypto.getRandomValues(new Uint8Array(16))
  return { salt: hex(salt), digest: await digest(password, salt) }
}

export async function matchesDemoCredential(password: string, credential?: User['credential']) {
  if (!credential || !/^[a-f0-9]{32}$/.test(credential.salt)) return false
  const salt = new Uint8Array(16)
  for (let i = 0; i < salt.length; i++) salt[i] = Number.parseInt(credential.salt.slice(i * 2, i * 2 + 2), 16)
  return await digest(password, salt) === credential.digest
}

export async function migrateDemoUsers(users: (User & { password?: string })[], seed: User[]): Promise<User[]> {
  return Promise.all(users.map(async ({ password, ...user }) => {
    if (seed.some((demo) => demo.id === user.id && demo.email === user.email)) {
      return { ...user, demoAccount: true as const }
    }
    if (!user.credential && typeof password === 'string') {
      return { ...user, credential: await createDemoCredential(password) }
    }
    return user
  }))
}
