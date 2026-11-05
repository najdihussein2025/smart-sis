// TODO: replace with generated types from `npm run gen:api` (LookupsDto).

export interface CampusOption {
  id: number
  name: string
}
export interface DepartmentOption {
  id: number
  code: string
  name: string
}
export interface MajorOption {
  id: number
  name: string
  departmentId: number
  defaultMaxCredits: number
}
export interface RoomOption {
  id: number
  name: string
  campusId: number
  campusName: string
  capacity: number
}
/** An active instructor. `id` is the user id to send as `instructorId`. */
export interface InstructorOption {
  id: number
  fullName: string
  universityId: number
  departmentId: number
}

/** Everything the admin forms need for their dropdowns (GET /api/admin/lookups). */
export interface Lookups {
  campuses: CampusOption[]
  departments: DepartmentOption[]
  majors: MajorOption[]
  rooms: RoomOption[]
  instructors: InstructorOption[]
}
