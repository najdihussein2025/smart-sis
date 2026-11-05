// TODO: replace with generated types from `npm run gen:api` (PagedResult<T>).

/** A page of results, as returned by the list endpoints. */
export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
