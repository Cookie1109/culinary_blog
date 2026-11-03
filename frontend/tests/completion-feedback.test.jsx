import { fireEvent, render, screen } from '@testing-library/react'
import { jest, test, expect } from '@jest/globals'
import { FieldError } from '../src/components/ui/field-error'
import { Modal } from '../src/components/ui/modal'
import { ApiProblem } from '../src/lib/api/problem-details'
import {
  recipeFieldErrors,
  recipeErrorMessage,
  validateStep,
} from '../src/features/culinary/pages/dashboard/recipe-form-validation'

test('field validation keeps its accessible association across an error and recovery', () => {
  const { rerender } = render(<FieldError id="title-error" />)
  expect(screen.queryByRole('alert')).toBeNull()
  rerender(<FieldError id="title-error" message="Nhập tên công thức." />)
  expect(screen.getByRole('alert').id).toBe('title-error')
  expect(screen.getByRole('alert').textContent).toBe('Nhập tên công thức.')
  rerender(<FieldError id="title-error" />)
  expect(screen.queryByRole('alert')).toBeNull()
})

test('confirmation traps focus, preserves pending writes and restores the opener', () => {
  const close = jest.fn()
  const opener = document.createElement('button')
  document.body.append(opener)
  opener.focus()
  const content = <button type="button">Lưu</button>
  const { rerender, unmount } = render(
    <Modal open title="Xác nhận" onClose={close}>
      {content}
    </Modal>,
  )
  const dialog = screen.getByRole('dialog')
  const buttons = dialog.querySelectorAll('button')
  buttons[buttons.length - 1].focus()
  fireEvent.keyDown(document, { key: 'Tab' })
  expect(document.activeElement).toBe(buttons[0])
  rerender(
    <Modal open busy title="Xác nhận" onClose={close}>
      {content}
    </Modal>,
  )
  fireEvent.keyDown(document, { key: 'Escape' })
  fireEvent.click(screen.getByRole('button', { name: 'Đóng hộp thoại' }))
  expect(close).not.toHaveBeenCalled()
  rerender(
    <Modal open title="Xác nhận" onClose={close}>
      {content}
    </Modal>,
  )
  fireEvent.keyDown(document, { key: 'Escape' })
  expect(close).toHaveBeenCalledTimes(1)
  unmount()
  expect(document.activeElement).toBe(opener)
  opener.remove()
})

test.each(['RECIPE_CONCURRENCY_CONFLICT', 'RECIPE_INVALID_TRANSITION', 'INTERNAL_ERROR'])(
  'recipe failure %s never renders private server details',
  (code) => {
    const problem = new ApiProblem({
      code,
      status: 409,
      title: 'Internal',
      detail: 'synthetic-private-connection',
    })
    const message = recipeErrorMessage(problem)
    expect(message.length).toBeGreaterThan(10)
    expect(message).not.toContain('synthetic-private-connection')
  },
)

test('server fields are allowlisted and local step errors stay actionable', () => {
  const errors = recipeFieldErrors(
    new ApiProblem({
      code: 'VALIDATION_ERROR',
      status: 422,
      title: 'Invalid',
      errors: { CategoryId: ['private'], 'Nutrition.Calories': ['private'], Unknown: ['private'] },
    }),
  )
  expect(Object.keys(errors).sort()).toEqual(['categoryId', 'nutrition.calories'])
  expect(JSON.stringify(errors)).not.toContain('private')
  expect(Object.keys(validateStep({ title: '', description: '', timerMinutes: -1 })).sort()).toEqual([
    'description',
    'timerMinutes',
    'title',
  ])
})
