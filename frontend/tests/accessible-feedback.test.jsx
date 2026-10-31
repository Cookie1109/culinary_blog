import { fireEvent, render, screen } from '@testing-library/react'
import { jest, test, expect } from '@jest/globals'
import { createRef } from 'react'
import { FieldError } from '../src/components/ui/field-error'
import { Modal } from '../src/components/ui/modal'
import { ApiProblem, apiErrorMessage, authErrorMessage } from '../src/lib/api/problem-details'
import { setServerFieldErrors } from '../src/features/culinary/forms/server-errors'
import { Button } from '../src/components/ui/button'
import { Input } from '../src/components/ui/input'

test('React19 UI wrappers forward refs, preserve field association and default to a non-submitting button', () => {
  const buttonRef = createRef()
  const inputRef = createRef()
  const submit = jest.fn((event) => event.preventDefault())
  render(
    <form onSubmit={submit}>
      <Input id="fixture-name" label="Tên" error="Nhập tên." ref={inputRef} />
      <Button ref={buttonRef}>Thao tác</Button>
      <Button type="submit">Lưu</Button>
    </form>,
  )
  expect(buttonRef.current).toBe(screen.getByRole('button', { name: 'Thao tác' }))
  expect(inputRef.current).toBe(screen.getByRole('textbox', { name: 'Tên' }))
  expect(inputRef.current.getAttribute('aria-describedby')).toBe('fixture-name-error')
  expect(document.getElementById('fixture-name-error').textContent).toBe('Nhập tên.')
  inputRef.current.focus()
  expect(document.activeElement).toBe(inputRef.current)
  fireEvent.click(buttonRef.current)
  expect(submit).not.toHaveBeenCalled()
  fireEvent.click(screen.getByRole('button', { name: 'Lưu' }))
  expect(submit).toHaveBeenCalledTimes(1)
})

test('field errors expose an alert only when an actionable message exists', () => {
  const { rerender } = render(<FieldError id="title-error" />)
  expect(screen.queryByRole('alert')).toBeNull()
  rerender(<FieldError id="title-error" message="Nhập tên công thức từ 5 ký tự." />)
  expect(screen.getByRole('alert').id).toBe('title-error')
  expect(screen.getByRole('alert').textContent).toContain('5 ký tự')
})

test('auth errors use stable codes and never expose raw server failures', () => {
  const duplicate = new ApiProblem({ status: 409, title: 'Conflict', code: 'AUTH_EMAIL_EXISTS' })
  expect(authErrorMessage(duplicate)).toContain('đăng nhập hoặc dùng email khác')
  const failure = new ApiProblem({
    status: 500,
    title: 'Internal',
    detail: 'Stack trace: secret connection string',
    code: 'INTERNAL_ERROR',
  })
  expect(authErrorMessage(failure)).not.toMatch(/secret|stack|connection/i)
  expect(authErrorMessage(failure)).toContain('thử lại')
})

test.each([
  ['CATEGORY_DELETE_HAS_RECIPES', 409, 'đang chứa công thức nên không thể xóa'],
  ['RECIPE_CONCURRENCY_CONFLICT', 409, 'tải lại phiên bản mới'],
  ['CATEGORY_NAME_EXISTS', 409, 'chọn tên khác'],
  ['UNKNOWN', 429, 'chờ một phút'],
  ['AUTH_EMAIL_UNAVAILABLE', 503, 'gửi email chưa sẵn sàng'],
  ['UNKNOWN', 500, 'thử lại'],
])('API error %s renders recovery guidance without server details', (code, status, expected) => {
  const message = apiErrorMessage(
    new ApiProblem({ code, status, title: 'Stack trace', detail: 'secret database connection' }),
  )
  expect(message).toContain(expected)
  expect(message).not.toMatch(/stack|secret|database/i)
})

test('server field errors accept only mapped fields and never render raw validation payloads', () => {
  const setError = jest.fn()
  setServerFieldErrors(
    new ApiProblem({
      status: 422,
      code: 'VALIDATION_ERROR',
      title: 'Invalid',
      errors: { NAME: ['secret'], Unknown: ['stack trace'] },
    }),
    setError,
    { Name: ['name', 'Tên cần từ 2 đến 50 ký tự.'] },
  )
  expect(setError).toHaveBeenCalledTimes(1)
  expect(setError).toHaveBeenCalledWith('name', { type: 'server', message: 'Tên cần từ 2 đến 50 ký tự.' })
  setError.mockClear()
  setServerFieldErrors(
    new ApiProblem({ status: 500, code: 'UNKNOWN', title: 'Internal', errors: { Name: ['secret'] } }),
    setError,
    { Name: ['name', 'Tên chưa hợp lệ.'] },
  )
  expect(setError).not.toHaveBeenCalled()
})

test('modal traps keyboard focus and Escape closes unless a write is pending', () => {
  const close = jest.fn()
  const { rerender } = render(
    <Modal open title="Lưu thay đổi" onClose={close}>
      <button type="button">Save</button>
    </Modal>,
  )
  const dialog = screen.getByRole('dialog')
  expect(dialog.contains(document.activeElement)).toBe(true)
  const buttons = dialog.querySelectorAll('button')
  buttons[buttons.length - 1].focus()
  fireEvent.keyDown(document, { key: 'Tab' })
  expect(document.activeElement).toBe(buttons[0])
  fireEvent.keyDown(document, { key: 'Escape' })
  expect(close).toHaveBeenCalledTimes(1)
  rerender(
    <Modal open busy title="Lưu thay đổi" onClose={close}>
      <button type="button">Save</button>
    </Modal>,
  )
  fireEvent.keyDown(document, { key: 'Escape' })
  expect(close).toHaveBeenCalledTimes(1)
})
