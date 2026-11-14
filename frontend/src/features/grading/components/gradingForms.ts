import { z } from 'zod'

/** "12", "12.5" or "12.25": a non-negative number with at most 2 decimals. */
const decimal2 = /^\d+(\.\d{1,2})?$/

export const isDecimal2 = (s: string) => decimal2.test(s.trim())

/** Mirrors the API's grade component validators (UX only; the API checks again, including the 100% total). */
export const componentSchema = z.object({
  name: z.string().trim().min(1, 'Enter a name, e.g. "Midterm".').max(64, 'Use at most 64 characters.'),
  maxGrade: z
    .string()
    .trim()
    .refine((s) => isDecimal2(s) && Number(s) > 0 && Number(s) <= 1000, 'Max grade must be more than 0 and at most 1000 (2 decimals).'),
  weight: z
    .string()
    .trim()
    .refine((s) => isDecimal2(s) && Number(s) > 0 && Number(s) <= 100, 'Weight must be more than 0% and at most 100% (2 decimals).'),
})

export type ComponentFormValues = z.infer<typeof componentSchema>

/** Score sheet for one component: one text field per student, empty = not graded. */
export const scoreSheetSchema = (maxGrade: number) =>
  z.object({
    scores: z.array(
      z.object({
        enrollmentId: z.number(),
        score: z
          .string()
          .trim()
          .refine((s) => s === '' || (isDecimal2(s) && Number(s) <= maxGrade), `Enter a score between 0 and ${maxGrade} (2 decimals).`),
      }),
    ),
  })

export type ScoreSheetValues = z.infer<ReturnType<typeof scoreSheetSchema>>

/** "40", "12.5": numbers from the API without trailing zeros. */
export const num = (n: number) => String(Number(n.toFixed(2)))
