import { create } from 'zustand'
import { persist } from 'zustand/middleware'

type ViewMode = 'grid' | 'list'

interface UIState {
  sidebarOpen: boolean
  modal: string | null
  viewMode: ViewMode
  setSidebarOpen: (open: boolean) => void
  setModal: (modal: string | null) => void
  setViewMode: (mode: ViewMode) => void
}

export const useUIStore = create<UIState>()(
  persist(
    (set) => ({
      sidebarOpen: false,
      modal: null,
      viewMode: 'grid',
      setSidebarOpen: (sidebarOpen) => set({ sidebarOpen }),
      setModal: (modal) => set({ modal }),
      setViewMode: (viewMode) => set({ viewMode }),
    }),
    {
      name: 'culinary-ui',
      partialize: (state) => ({ viewMode: state.viewMode }),
      skipHydration: true,
    },
  ),
)
