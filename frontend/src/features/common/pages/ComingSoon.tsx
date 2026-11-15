import { Hammer } from 'lucide-react'
import { Card } from '@/shared/ui/Card'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'

/**
 * Placeholder for routes whose feature belongs to a later build phase (PROJECT.md §10). `phase` is for the team (it
 * stays in the route table); the user only sees that the screen isn't available yet.
 */
export function ComingSoon({ title }: { title: string; phase: number }) {
  return (
    <div className="space-y-6">
      <PageHeader title={title} />
      <Card>
        <EmptyState icon={Hammer} title="Coming soon">
          This screen isn’t available yet. Everything else in the menu works as usual.
        </EmptyState>
      </Card>
    </div>
  )
}
