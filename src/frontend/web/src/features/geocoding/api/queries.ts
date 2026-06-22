import { useMutation } from '@tanstack/react-query'
import { geocodingApi } from './geocodingApi'

/** On-demand address → coordinates lookup, triggered by the "Locate on map" button. */
export function useGeocode() {
  return useMutation({
    mutationFn: (q: string) => geocodingApi.search(q),
  })
}
