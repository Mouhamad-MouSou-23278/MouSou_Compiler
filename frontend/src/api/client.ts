import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import {
  AuthResponse,
  User,
  Project,
  ProjectFile,
  Execution,
  AIAnalyzeResponse,
  AIAutoFixResponse,
  AIJobType,
} from '../types';

const api = axios.create({
  baseURL: '',
  withCredentials: true, // Send HttpOnly refresh cookie with requests
});

// Attach JWT access token to requests
api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem('access_token');
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Auto-refresh access token on 401 Unauthorized
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: unknown) => void;
  reject: (reason?: unknown) => void;
}> = [];

const processQueue = (error: Error | null, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status === 401 && !originalRequest._retry && !originalRequest.url?.includes('/api/auth/login')) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`;
            }
            return api(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const { data } = await axios.post<AuthResponse>('/api/auth/refresh', {}, { withCredentials: true });
        localStorage.setItem('access_token', data.accessToken);
        processQueue(null, data.accessToken);
        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
        }
        return api(originalRequest);
      } catch (refreshErr) {
        processQueue(refreshErr as Error, null);
        localStorage.removeItem('access_token');
        return Promise.reject(refreshErr);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

export const apiClient = {
  auth: {
    login: (data: { email: string; password: string }) => api.post<AuthResponse>('/api/auth/login', data),
    register: (data: { email: string; password: string; firstName?: string; lastName?: string }) =>
      api.post<AuthResponse>('/api/auth/register', data),
    logout: () => api.post('/api/auth/logout'),
    getProfile: () => api.get<User>('/api/auth/me'),
    updateProfile: (data: { firstName?: string; lastName?: string }) => api.put<User>('/api/auth/me', data),
  },
  projects: {
    list: () => api.get<Project[]>('/api/projects'),
    get: (id: string) => api.get<Project>(`/api/projects/${id}`),
    create: (data: { name: string; description?: string; language: string }) =>
      api.post<Project>('/api/projects', data),
    update: (id: string, data: { name: string; description?: string; language?: string }) =>
      api.put<Project>(`/api/projects/${id}`, data),
    delete: (id: string) => api.delete(`/api/projects/${id}`),
  },
  files: {
    list: (projectId: string) => api.get<ProjectFile[]>(`/api/projects/${projectId}/files`),
    get: (id: string) => api.get<ProjectFile>(`/api/files/${id}`),
    create: (projectId: string, data: { path: string; content: string; isEntryPoint: boolean }) =>
      api.post<ProjectFile>(`/api/projects/${projectId}/files`, data),
    update: (id: string, data: { path?: string; content?: string; isEntryPoint?: boolean }) =>
      api.put<ProjectFile>(`/api/files/${id}`, data),
    delete: (id: string) => api.delete(`/api/files/${id}`),
    setEntryPoint: (id: string) => api.post(`/api/files/${id}/entry-point`),
    batchSave: (projectId: string, files: Record<string, string>) =>
      api.post(`/api/projects/${projectId}/files/batch`, files),
  },
  executions: {
    run: (data: { projectId: string; stdinInput?: string; autoFixOnError?: boolean }) =>
      api.post<Execution>('/api/executions', data),
    get: (id: string) => api.get<Execution>(`/api/executions/${id}`),
    list: (projectId: string) => api.get<Execution[]>(`/api/projects/${projectId}/executions`),
    sendInput: (id: string, input: string) => api.post(`/api/executions/${id}/input`, { input }),
  },
  ai: {
    analyze: (data: { projectId: string; type: AIJobType; specificFilePath?: string; customPrompt?: string }) =>
      api.post<AIAnalyzeResponse>('/api/ai/analyze', data),
    autoFix: (data: { executionId: string }) => api.post<AIAutoFixResponse>('/api/ai/auto-fix', data),
  },
};
