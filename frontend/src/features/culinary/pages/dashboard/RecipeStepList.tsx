'use client'

import {
  closestCenter,
  DndContext,
  DragOverlay,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type Modifiers,
} from '@dnd-kit/core'
import {
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { ArrowDown, ArrowUp, GripVertical, Trash2 } from 'lucide-react'
import { useId, useRef, useState } from 'react'
import type { RecipeStep } from '@/lib/api/content-client'
import { formatTimerDuration } from '@/lib/recipe-timer'

interface Props {
  steps: RecipeStep[]
  disabled: boolean
  saving: boolean
  onMove: (step: RecipeStep, position: number) => void
  onEdit: (step: RecipeStep) => void
  onDelete: (step: RecipeStep) => void
}

const verticalOnly: Modifiers = [({ transform }) => ({ ...transform, x: 0 })]

function SortableStep({
  step,
  count,
  disabled,
  actionsDisabled,
  onMove,
  onEdit,
  onDelete,
}: Omit<Props, 'steps' | 'saving'> & { step: RecipeStep; count: number; actionsDisabled: boolean }) {
  const { attributes, listeners, setNodeRef, setActivatorNodeRef, transform, transition, isDragging } =
    useSortable({ id: step.id, disabled })

  return (
    <li
      ref={setNodeRef}
      data-step-id={step.id}
      style={{ transform: CSS.Translate.toString(transform), transition }}
      className={`flex gap-3 border border-border bg-background p-4 ${isDragging ? 'opacity-30' : ''}`}
    >
      <button
        ref={setActivatorNodeRef}
        type="button"
        {...attributes}
        {...listeners}
        disabled={disabled}
        aria-label={`Kéo để đổi vị trí bước ${step.stepNumber}`}
        className="touch-none self-start cursor-grab p-1 text-muted-foreground hover:text-primary active:cursor-grabbing disabled:cursor-not-allowed disabled:opacity-50"
      >
        <GripVertical size={18} aria-hidden="true" />
      </button>
      <span className="shrink-0 font-serif text-2xl text-primary">{step.stepNumber}</span>
      <div className="min-w-0 flex-1 wrap-anywhere">
        <strong className="block text-sm text-foreground">{step.title}</strong>
        <p className="mt-1 whitespace-pre-wrap text-sm text-muted-foreground">{step.description}</p>
        {step.timerMinutes != null && (
          <span className="mt-1.5 inline-block border border-amber-200 bg-amber-50 px-2 py-0.5 text-xs text-amber-700">
            ⏱ {formatTimerDuration(step.timerMinutes)}
          </span>
        )}
      </div>
      <div className="flex shrink-0 flex-col items-center gap-1">
        <button
          type="button"
          aria-label="Đưa bước lên"
          disabled={actionsDisabled || step.stepNumber === 1}
          onClick={() => onMove(step, step.stepNumber - 1)}
          className="p-1 text-muted-foreground hover:text-foreground disabled:opacity-40"
        >
          <ArrowUp size={15} />
        </button>
        <button
          type="button"
          aria-label="Đưa bước xuống"
          disabled={actionsDisabled || step.stepNumber === count}
          onClick={() => onMove(step, step.stepNumber + 1)}
          className="p-1 text-muted-foreground hover:text-foreground disabled:opacity-40"
        >
          <ArrowDown size={15} />
        </button>
        <button
          type="button"
          disabled={actionsDisabled}
          onClick={() => onEdit(step)}
          className="py-1 text-xs text-muted-foreground underline hover:text-primary disabled:opacity-40"
        >
          Sửa
        </button>
        <button
          type="button"
          aria-label="Xóa bước"
          disabled={actionsDisabled}
          onClick={() => onDelete(step)}
          className="p-1 text-muted-foreground hover:text-red-600 disabled:opacity-40"
        >
          <Trash2 size={15} />
        </button>
      </div>
    </li>
  )
}

export function RecipeStepList({ steps, disabled, saving, onMove, onEdit, onDelete }: Props) {
  const headingId = useId()
  const scrollRef = useRef<HTMLOListElement>(null)
  const [activeId, setActiveId] = useState<string | null>(null)
  // Cancellation keeps the list's current scroll position.
  const [animateDrop, setAnimateDrop] = useState(true)
  const activeStep = steps.find((step) => step.id === activeId)
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  )
  const reducedMotion =
    typeof window !== 'undefined' && window.matchMedia?.('(prefers-reduced-motion: reduce)').matches

  const handleDragEnd = ({ active, over }: DragEndEvent) => {
    setActiveId(null)
    if (disabled || !over || active.id === over.id) {
      setAnimateDrop(false)
      return
    }
    const step = steps.find((item) => item.id === active.id)
    const position = steps.findIndex((item) => item.id === over.id) + 1
    if (step && position > 0) onMove(step, position)
  }

  return (
    <section
      className="min-w-0 xl:flex xl:h-full xl:min-h-0 xl:flex-col"
      aria-labelledby={headingId}
      aria-busy={saving}
    >
      <div className="mb-3 flex shrink-0 flex-wrap items-center justify-between gap-2">
        <h4 id={headingId} className="text-xs font-medium uppercase tracking-widest text-muted-foreground">
          Danh sách bước ({steps.length})
        </h4>
        {saving && (
          <span role="status" className="text-xs text-muted-foreground">
            Đang lưu thứ tự…
          </span>
        )}
      </div>
      <DndContext
        sensors={sensors}
        collisionDetection={(args) => {
          const point = args.pointerCoordinates
          const bounds = scrollRef.current?.getBoundingClientRect()
          if (
            point &&
            bounds &&
            (point.x < bounds.left ||
              point.x > bounds.right ||
              point.y < bounds.top ||
              point.y > bounds.bottom)
          )
            return []
          return closestCenter(args)
        }}
        modifiers={verticalOnly}
        autoScroll={{ canScroll: (element) => element === scrollRef.current }}
        onDragStart={({ active }) => {
          setAnimateDrop(true)
          setActiveId(String(active.id))
        }}
        onDragCancel={() => {
          setAnimateDrop(false)
          setActiveId(null)
        }}
        onDragEnd={handleDragEnd}
        accessibility={{
          screenReaderInstructions: {
            draggable:
              'Nhấn Space hoặc Enter để chọn bước, dùng phím mũi tên để di chuyển, nhấn lần nữa để thả hoặc Escape để hủy.',
          },
          announcements: {
            onDragStart: ({ active }) =>
              `Đang kéo bước ${steps.findIndex((step) => step.id === active.id) + 1}.`,
            onDragOver: ({ over }) =>
              over
                ? `Vị trí mới là bước ${steps.findIndex((step) => step.id === over.id) + 1}.`
                : 'Đang ở ngoài danh sách.',
            onDragEnd: () => 'Đã thả bước.',
            onDragCancel: () => 'Đã hủy đổi vị trí bước.',
          },
        }}
      >
        <SortableContext items={steps.map((step) => step.id)} strategy={verticalListSortingStrategy}>
          <ol
            ref={scrollRef}
            aria-label="Danh sách bước"
            className="max-h-[min(50dvh,28rem)] space-y-3 overflow-y-auto overscroll-contain pb-3 pr-2 [scrollbar-gutter:stable] xl:max-h-none xl:min-h-0 xl:flex-1"
          >
            {steps.length === 0 && (
              <li className="border border-dashed border-border p-8 text-center text-sm text-muted-foreground">
                Chưa có bước thực hiện nào. Hãy thêm ít nhất 1 bước để đủ điều kiện xuất bản.
              </li>
            )}
            {steps.map((step) => (
              <SortableStep
                key={step.id}
                step={step}
                count={steps.length}
                disabled={disabled}
                actionsDisabled={disabled || activeId !== null}
                onMove={onMove}
                onEdit={onEdit}
                onDelete={onDelete}
              />
            ))}
          </ol>
        </SortableContext>
        <DragOverlay
          modifiers={verticalOnly}
          adjustScale={false}
          dropAnimation={reducedMotion || !animateDrop ? null : undefined}
        >
          {activeStep && (
            <div
              data-step-drag-overlay
              aria-hidden="true"
              className="flex gap-3 border border-primary bg-background p-4 shadow-lg"
            >
              <GripVertical size={18} className="shrink-0 text-primary" />
              <span className="font-serif text-2xl text-primary">{activeStep.stepNumber}</span>
              <div className="min-w-0 wrap-anywhere">
                <strong className="text-sm">{activeStep.title}</strong>
                <p className="mt-1 line-clamp-3 whitespace-pre-wrap text-sm text-muted-foreground">
                  {activeStep.description}
                </p>
              </div>
            </div>
          )}
        </DragOverlay>
      </DndContext>
    </section>
  )
}
