import { createClient, fetch as fetchPlugin, type ClientPlugin } from 'villus'

export const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5199'

const STORAGE_KEY = 'tracker_secret_key'

export function getSecretKey(): string {
  return localStorage.getItem(STORAGE_KEY) ?? ''
}

export function setSecretKey(key: string) {
  localStorage.setItem(STORAGE_KEY, key)
}

export function clearSecretKey() {
  localStorage.removeItem(STORAGE_KEY)
}

/** Attaches X-Secret-Key to every request (spec §10 auth). */
const authPlugin: ClientPlugin = ({ opContext }) => {
  opContext.headers['X-Secret-Key'] = getSecretKey()
}

export const client = createClient({
  url: `${API_URL}/graphql`,
  use: [authPlugin, fetchPlugin()],
})

/** Direct calls outside components (stores, wizard submits). Throws the first error message if any. */
export async function execute<TData>(
  query: string,
  variables?: Record<string, unknown>,
): Promise<TData> {
  const { data, error } = await client.executeQuery<TData>({ query, variables })
  if (error) throw new Error(firstGraphQLMessage(error))
  return data as TData
}

export async function executeMutation<TData>(
  query: string,
  variables?: Record<string, unknown>,
): Promise<TData> {
  const { data, error } = await client.executeMutation<TData>({ query, variables })
  if (error) throw new Error(firstGraphQLMessage(error))
  return data as TData
}

function firstGraphQLMessage(error: { message: string; graphqlErrors?: { message: string }[] }): string {
  return error.graphqlErrors?.[0]?.message ?? error.message
}
