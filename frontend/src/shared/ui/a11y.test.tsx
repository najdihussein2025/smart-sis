import { useState } from 'react'
import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import { Button } from './Button'
import { Dialog } from './Dialog'
import { Tabs } from './Tabs'

// T77 (AB#144): keyboard access to the shared dialog, tabs and mobile menu.

function DialogHarness() {
  const [open, setOpen] = useState(false)
  return (
    <>
      <Button onClick={() => setOpen(true)}>Open form</Button>
      <Dialog open={open} title="Edit" onClose={() => setOpen(false)} actions={<Button onClick={() => setOpen(false)}>Save</Button>}>
        <label>
          Name <input />
        </label>
      </Dialog>
    </>
  )
}

describe('Dialog', () => {
  it('moves focus inside, keeps Tab inside, closes on Escape and returns focus to the opener', async () => {
    const user = userEvent.setup()
    render(<DialogHarness />)
    const opener = screen.getByRole('button', { name: 'Open form' })
    await user.click(opener)

    const dialog = screen.getByRole('dialog', { name: 'Edit' })
    expect(screen.getByRole('textbox', { name: 'Name' })).toHaveFocus()

    for (let i = 0; i < 5; i++) {
      await user.tab()
      expect(dialog).toContainElement(document.activeElement as HTMLElement)
    }
    await user.tab({ shift: true })
    expect(dialog).toContainElement(document.activeElement as HTMLElement)

    await user.keyboard('{Escape}')
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(opener).toHaveFocus()
  })
})

function TabsHarness() {
  const [day, setDay] = useState<'mon' | 'tue' | 'wed'>('mon')
  return (
    <Tabs
      label="Days"
      value={day}
      onChange={setDay}
      items={[{ value: 'mon', label: 'Mon' }, { value: 'tue', label: 'Tue' }, { value: 'wed', label: 'Wed' }]}
    />
  )
}

describe('Tabs', () => {
  it('moves with the arrow keys, Home and End', async () => {
    const user = userEvent.setup()
    render(<TabsHarness />)
    await user.click(screen.getByRole('tab', { name: 'Mon' }))

    await user.keyboard('{End}')
    expect(screen.getByRole('tab', { name: 'Wed' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: 'Wed' })).toHaveFocus()
    await user.keyboard('{ArrowRight}')
    expect(screen.getByRole('tab', { name: 'Mon' })).toHaveFocus()
    await user.keyboard('{ArrowLeft}')
    expect(screen.getByRole('tab', { name: 'Wed' })).toHaveFocus()
    await user.keyboard('{Home}')
    expect(screen.getByRole('tab', { name: 'Mon' })).toHaveAttribute('aria-selected', 'true')
  })
})

describe('Mobile menu', () => {
  it('opens as a modal menu, closes on Escape and gives focus back to the menu button', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))))
    const { user } = renderAt('/student', { as: 'Student' })

    const menuButton = await screen.findByRole('button', { name: 'Open menu' })
    expect(menuButton).toHaveAttribute('aria-expanded', 'false')
    await user.click(menuButton)

    const menu = screen.getByRole('dialog', { name: 'Menu' })
    expect(screen.getByRole('button', { name: 'Close menu' })).toHaveFocus()
    for (let i = 0; i < 20; i++) await user.tab()
    expect(menu).toContainElement(document.activeElement as HTMLElement)

    await user.keyboard('{Escape}')
    expect(screen.queryByRole('dialog', { name: 'Menu' })).not.toBeInTheDocument()
    expect(menuButton).toHaveFocus()
  })

  it('has a skip link to the main content', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))))
    renderAt('/student', { as: 'Student' })
    expect(await screen.findByRole('link', { name: 'Skip to main content' })).toHaveAttribute('href', '#main')
    expect(screen.getByRole('main')).toHaveAttribute('id', 'main')
  })
})
