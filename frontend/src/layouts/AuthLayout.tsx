import { Outlet } from 'react-router'

export function AuthLayout() {
  return (
    <div className="grid min-h-dvh lg:grid-cols-[1.05fr_1fr]">
      <aside className="relative hidden overflow-hidden bg-brand text-white lg:flex lg:flex-col lg:justify-between lg:p-12">
        {/* Quiet geometric backdrop: concentric arcs, like rings in a courtyard */}
        <svg aria-hidden className="absolute -right-40 -bottom-40 size-[720px] text-white/[0.06]" viewBox="0 0 720 720" fill="none">
          {[120, 200, 280, 360].map((r) => (
            <circle key={r} cx="360" cy="360" r={r} stroke="currentColor" strokeWidth="1.5" />
          ))}
        </svg>
        <div className="relative flex items-center gap-3">
          <img src="/favicon.svg" alt="" className="size-9" />
          <span className="font-display text-lg font-semibold tracking-tight">Smart SIS</span>
        </div>
        <div className="relative max-w-md">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-accent">Antonine University</p>
          <h2 className="mt-4 font-display text-[44px] font-semibold leading-[1.05] tracking-tight">
            Your courses, grades and schedule, in one place.
          </h2>
          <p className="mt-5 text-white/70">
            Students, instructors and administration share one system, with an assistant that knows your academic record.
          </p>
        </div>
        <p className="relative text-sm text-white/50">© {new Date().getFullYear()} Smart SIS</p>
      </aside>

      <main className="flex items-center justify-center px-4 py-12 sm:px-8">
        <div className="w-full max-w-[400px]">
          <div className="mb-10 flex items-center gap-3 lg:hidden">
            <img src="/favicon.svg" alt="" className="size-8" />
            <span className="font-display text-lg font-semibold">Smart SIS</span>
          </div>
          <Outlet />
        </div>
      </main>
    </div>
  )
}
