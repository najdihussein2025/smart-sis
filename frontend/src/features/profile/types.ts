// TODO: replace with generated types from `npm run gen:api`.
export interface UserProfile {
  id: number
  universityId: number
  fullName: string
  email: string
  role: 'Student' | 'Instructor' | 'Admin'
  phone: string | null
  dateOfBirth: string | null
  photoUrl: string | null
  student: { majorId: number; majorName: string; campusName: string; enrollmentDate: string; maxCredits: number } | null
  instructor: { departmentId: number; departmentName: string } | null
}
