import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { clearSecretKey, getSecretKey, setSecretKey } from '@/api/client'

/** Secret key entered once, kept in localStorage (spec §10). */
export const useSessionStore = defineStore('session', () => {
  const key = ref(getSecretKey())

  const hasKey = computed(() => key.value.length > 0)

  function save(newKey: string) {
    setSecretKey(newKey.trim())
    key.value = newKey.trim()
  }

  function reset() {
    clearSecretKey()
    key.value = ''
  }

  return { key, hasKey, save, reset }
})
