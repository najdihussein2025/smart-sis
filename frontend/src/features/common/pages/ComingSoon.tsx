import { Hammer } from 'lucide-react'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'

/** Placeholder for routes whose feature belongs to a later build phase (PROJECT.md §10). */
export function ComingSoon({ title, phase }: { title: string; phase: number }) {
  return (
    <>
      <PageHeader title={title} />
      <Card>
        <EmptyState icon={Hammer} title="Not built yet">
          This screen is part of build phase {phase}. The navigation is in place so the layout can be reviewed now.
        </EmptyState>
      </Card>
    </>
  )
}
