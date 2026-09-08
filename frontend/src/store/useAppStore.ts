import { create } from 'zustand';
import { User, Project, ProjectFile, Execution, ExecutionLog, AIAnalyzeResponse } from '../types';
import { apiClient } from '../api/client';
import { signalRManager } from '../signalr/executionHub';

interface AppState {
  // Auth
  user: User | null;
  isAuthenticated: boolean;
  isAuthModalOpen: boolean;
  setAuthModalOpen: (open: boolean) => void;
  setAuth: (user: User | null, token: string | null) => void;
  logout: () => Promise<void>;

  // Projects & Files
  projects: Project[];
  currentProject: Project | null;
  files: ProjectFile[];
  openTabs: ProjectFile[];
  activeFileId: string | null;
  isProjectModalOpen: boolean;
  setProjectModalOpen: (open: boolean) => void;
  fetchProjects: () => Promise<void>;
  selectProject: (projectId: string) => Promise<void>;
  openTab: (file: ProjectFile) => void;
  closeTab: (fileId: string) => void;
  setActiveFileId: (fileId: string) => void;
  updateFileContent: (fileId: string, content: string) => void;
  saveActiveFile: () => Promise<void>;
  createNewFile: (path: string, isEntryPoint: boolean) => Promise<void>;
  deleteFile: (fileId: string) => Promise<void>;

  // Execution
  isRunning: boolean;
  currentExecution: Execution | null;
  logs: ExecutionLog[];
  runCode: (autoFix?: boolean) => Promise<void>;
  sendStdin: (input: string) => Promise<void>;
  clearLogs: () => void;
  addLog: (log: ExecutionLog) => void;

  // AI Assistant
  isAIPanelOpen: boolean;
  aiLoading: boolean;
  aiResponse: AIAnalyzeResponse | null;
  toggleAIPanel: () => void;
  requestAIAnalysis: (type: any, customPrompt?: string) => Promise<void>;
  requestAutoFix: () => Promise<void>;
}

export const useAppStore = create<AppState>((set, get) => ({
  // Auth Initial State
  user: null,
  isAuthenticated: !!localStorage.getItem('access_token'),
  isAuthModalOpen: false,
  setAuthModalOpen: (open) => set({ isAuthModalOpen: open }),

  setAuth: (user, token) => {
    if (token) {
      localStorage.setItem('access_token', token);
      set({ user, isAuthenticated: true, isAuthModalOpen: false });
    } else {
      localStorage.removeItem('access_token');
      set({ user: null, isAuthenticated: false });
    }
  },

  logout: async () => {
    try {
      await apiClient.auth.logout();
    } catch (e) {
      console.warn('Logout API error:', e);
    }
    localStorage.removeItem('access_token');
    set({ user: null, isAuthenticated: false, currentProject: null, files: [], openTabs: [] });
  },

  // Projects & Files State
  projects: [],
  currentProject: null,
  files: [],
  openTabs: [],
  activeFileId: null,
  isProjectModalOpen: false,
  setProjectModalOpen: (open) => set({ isProjectModalOpen: open }),

  fetchProjects: async () => {
    try {
      const res = await apiClient.projects.list();
      set({ projects: res.data });
      if (res.data.length > 0 && !get().currentProject) {
        await get().selectProject(res.data[0].id);
      }
    } catch (err) {
      console.error('Failed to load projects:', err);
    }
  },

  selectProject: async (projectId: string) => {
    try {
      const res = await apiClient.projects.get(projectId);
      const project = res.data;
      const files = project.files || [];
      const entryFile = files.find((f) => f.isEntryPoint) || files[0];

      set({
        currentProject: project,
        files: files,
        openTabs: entryFile ? [entryFile] : [],
        activeFileId: entryFile ? entryFile.id : null,
      });
    } catch (err) {
      console.error('Failed to select project:', err);
    }
  },

  openTab: (file: ProjectFile) => {
    const { openTabs } = get();
    if (!openTabs.some((t) => t.id === file.id)) {
      set({ openTabs: [...openTabs, file], activeFileId: file.id });
    } else {
      set({ activeFileId: file.id });
    }
  },

  closeTab: (fileId: string) => {
    const { openTabs, activeFileId } = get();
    const newTabs = openTabs.filter((t) => t.id !== fileId);
    let newActiveId = activeFileId;
    if (activeFileId === fileId) {
      newActiveId = newTabs.length > 0 ? newTabs[newTabs.length - 1].id : null;
    }
    set({ openTabs: newTabs, activeFileId: newActiveId });
  },

  setActiveFileId: (fileId: string) => set({ activeFileId: fileId }),

  updateFileContent: (fileId: string, content: string) => {
    set((state) => ({
      files: state.files.map((f) => (f.id === fileId ? { ...f, content } : f)),
      openTabs: state.openTabs.map((f) => (f.id === fileId ? { ...f, content } : f)),
    }));
  },

  saveActiveFile: async () => {
    const { activeFileId, files } = get();
    if (!activeFileId) return;
    const file = files.find((f) => f.id === activeFileId);
    if (!file) return;

    try {
      await apiClient.files.update(file.id, { content: file.content });
      console.log(`Saved file ${file.path}`);
    } catch (err) {
      console.error('Failed to save file:', err);
    }
  },

  createNewFile: async (path: string, isEntryPoint: boolean) => {
    const { currentProject } = get();
    if (!currentProject) return;

    try {
      const res = await apiClient.files.create(currentProject.id, {
        path,
        content: '',
        isEntryPoint,
      });
      const newFile = res.data;
      set((state) => ({
        files: [...state.files, newFile],
        openTabs: [...state.openTabs, newFile],
        activeFileId: newFile.id,
      }));
    } catch (err) {
      console.error('Failed to create file:', err);
    }
  },

  deleteFile: async (fileId: string) => {
    try {
      await apiClient.files.delete(fileId);
      get().closeTab(fileId);
      set((state) => ({
        files: state.files.filter((f) => f.id !== fileId),
      }));
    } catch (err) {
      console.error('Failed to delete file:', err);
    }
  },

  // Execution State
  isRunning: false,
  currentExecution: null,
  logs: [],

  runCode: async (autoFix = false) => {
    const { currentProject, files, saveActiveFile } = get();
    if (!currentProject) return;

    await saveActiveFile();
    set({ isRunning: true, logs: [] });

    try {
      await signalRManager.connect();

      // Batch save all file changes
      const fileMap: Record<string, string> = {};
      files.forEach((f) => {
        fileMap[f.path] = f.content;
      });
      await apiClient.files.batchSave(currentProject.id, fileMap);

      const res = await apiClient.executions.run({
        projectId: currentProject.id,
        autoFixOnError: autoFix,
      });

      const execution = res.data;
      set({ currentExecution: execution });

      await signalRManager.joinExecution(execution.id);

      get().addLog({
        id: 'start',
        stream: 'system',
        text: `[System] Job queued for execution (Language: ${execution.language})...\n`,
        timestamp: new Date().toISOString(),
      });

      // Poll until execution completes
      const pollInterval = setInterval(async () => {
        try {
          const detailRes = await apiClient.executions.get(execution.id);
          const current = detailRes.data;
          set({ currentExecution: current });

          if (current.status !== 'Queued' && current.status !== 'Running') {
            clearInterval(pollInterval);
            set({ isRunning: false });

            if (current.output) {
              get().addLog({
                id: 'out',
                stream: 'stdout',
                text: current.output,
                timestamp: new Date().toISOString(),
              });
            }
            if (current.error) {
              get().addLog({
                id: 'err',
                stream: 'stderr',
                text: current.error,
                timestamp: new Date().toISOString(),
              });
            }

            get().addLog({
              id: 'end',
              stream: 'system',
              text: `\n[System] Execution finished with status: ${current.status} (Exit Code: ${current.exitCode ?? 0})\n`,
              timestamp: new Date().toISOString(),
            });
          }
        } catch (e) {
          clearInterval(pollInterval);
          set({ isRunning: false });
        }
      }, 1000);
    } catch (err) {
      console.error('Failed to run code:', err);
      set({ isRunning: false });
      get().addLog({
        id: 'error',
        stream: 'stderr',
        text: `Failed to initiate execution: ${(err as Error).message}\n`,
        timestamp: new Date().toISOString(),
      });
    }
  },

  sendStdin: async (input: string) => {
    const { currentExecution } = get();
    if (!currentExecution) return;

    try {
      await apiClient.executions.sendInput(currentExecution.id, input);
      get().addLog({
        id: Math.random().toString(),
        stream: 'system',
        text: `> ${input}\n`,
        timestamp: new Date().toISOString(),
      });
    } catch (err) {
      console.error('Failed to send stdin:', err);
    }
  },

  clearLogs: () => set({ logs: [] }),

  addLog: (log: ExecutionLog) => {
    set((state) => ({ logs: [...state.logs, log] }));
  },

  // AI Assistant State
  isAIPanelOpen: false,
  aiLoading: false,
  aiResponse: null,

  toggleAIPanel: () => set((state) => ({ isAIPanelOpen: !state.isAIPanelOpen })),

  requestAIAnalysis: async (type, customPrompt) => {
    const { currentProject, activeFileId, files } = get();
    if (!currentProject) return;

    const activeFile = files.find((f) => f.id === activeFileId);
    set({ aiLoading: true, isAIPanelOpen: true });

    try {
      const res = await apiClient.ai.analyze({
        projectId: currentProject.id,
        type,
        specificFilePath: activeFile?.path,
        customPrompt,
      });
      set({ aiResponse: res.data, aiLoading: false });
    } catch (err) {
      console.error('AI analysis error:', err);
      set({ aiLoading: false });
    }
  },

  requestAutoFix: async () => {
    const { currentExecution, currentProject, selectProject, runCode } = get();
    if (!currentExecution || !currentProject) return;

    set({ aiLoading: true, isAIPanelOpen: true });
    try {
      const res = await apiClient.ai.autoFix({ executionId: currentExecution.id });
      set({ aiLoading: false });

      if (res.data.successfullyPatched) {
        // Reload project files
        await selectProject(currentProject.id);
        get().addLog({
          id: Math.random().toString(),
          stream: 'system',
          text: `[AI Auto-Fix] Applied patch: ${res.data.errorDiagnosis}. Rerunning...\n`,
          timestamp: new Date().toISOString(),
        });
        await runCode(false);
      }
    } catch (err) {
      console.error('Auto fix error:', err);
      set({ aiLoading: false });
    }
  },
}));
