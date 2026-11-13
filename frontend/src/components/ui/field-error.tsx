export function FieldError({ id, message = undefined }: { id: string; message?: string }) {
  return message ? (
    <span id={id} role="alert" className="mt-2 block text-sm text-red-700">
      {message}
    </span>
  ) : null
}
