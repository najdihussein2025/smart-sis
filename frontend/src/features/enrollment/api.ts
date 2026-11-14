import { api } from '@/shared/api/client'
import type { EnrollResult, MyEnrollments } from './types'

export const enrollmentApi = {
  mine: () => api.get<MyEnrollments>('/api/enrollment/my').then((r) => r.data),
  enroll: (sectionIds: number[]) => api.post<EnrollResult>('/api/enrollment/enroll', { sectionIds }).then((r) => r.data),
  swap: (fromSectionId: number, toSectionId: number) =>
    api.post<MyEnrollments>('/api/enrollment/swap', { fromSectionId, toSectionId }).then((r) => r.data),
  drop: (sectionId: number) => api.post('/api/enrollment/drop', { sectionId }).then(() => undefined),
}
