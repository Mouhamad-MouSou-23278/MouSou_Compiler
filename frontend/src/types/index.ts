export type UserRole = 'User' | 'Admin';

export interface User {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  role: UserRole;
  emailConfirmed: boolean;
  createdAt: string;
}

export interface AuthResponse {
  accessToken: string;
  userId: string;
  email: string;
  firstName?: string;
  lastName?: string;
  role: UserRole;
}

export interface ProjectFile {
  id: string;
  projectId: string;
  path: string;
  content: string;
  isEntryPoint: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface Project {
  id: string;
  userId: string;
  name: string;
  description?: string;
  language: string;
  isArchived: boolean;
  fileCount?: number;
  createdAt: string;
  updatedAt: string;
  files?: ProjectFile[];
}

export type ExecutionStatus = 'Queued' | 'Running' | 'Completed' | 'Failed' | 'Timeout' | 'MemoryExceeded';

export interface Execution {
  id: string;
  projectId: string;
  language: string;
  status: ExecutionStatus;
  startedAt: string;
  finishedAt?: string;
  exitCode?: number;
  output?: string;
  error?: string;
  stdinInput?: string;
  metricsJson?: string;
  trigger: string;
}

export interface ExecutionLog {
  id: string;
  stream: 'stdout' | 'stderr' | 'system';
  text: string;
  timestamp: string;
}

export type AIJobType = 'Explain' | 'BugFix' | 'Refactor' | 'TestGeneration' | 'SecurityAnalysis' | 'PerformanceAnalysis' | 'Documentation';

export interface AIAnalyzeResponse {
  jobId: string;
  type: AIJobType;
  analysis: string;
  suggestedCode?: string;
  diffPatch?: string;
}

export interface AIAutoFixResponse {
  jobId: string;
  errorDiagnosis: string;
  targetFilePath?: string;
  fixedCode?: string;
  diffPatch?: string;
  successfullyPatched: boolean;
  newExecutionId?: string;
}
