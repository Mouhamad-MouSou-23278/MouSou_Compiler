import React from 'react';
import { Play, Sparkles, FolderPlus, LogIn, LogOut, Code2, Terminal, Loader2 } from 'lucide-react';
import { useAppStore } from '../store/useAppStore';

export const Navbar: React.FC = () => {
  const {
    projects,
    currentProject,
    selectProject,
    setProjectModalOpen,
    runCode,
    isRunning,
    isAIPanelOpen,
    toggleAIPanel,
    isAuthenticated,
    user,
    setAuthModalOpen,
    logout,
  } = useAppStore();

  return (
    <header className="h-14 bg-[#1e1e24] border-b border-[#2d2d38] flex items-center justify-between px-4 select-none shrink-0 z-20">
      {/* Left: Branding & Project Selector */}
      <div className="flex items-center space-x-4">
        <div className="flex items-center space-x-2 text-blue-400 font-bold text-lg tracking-tight">
          <div className="bg-blue-600/20 p-1.5 rounded-lg border border-blue-500/30">
            <Terminal className="w-5 h-5 text-blue-400" />
          </div>
          <span className="hidden sm:inline bg-gradient-to-r from-blue-400 to-indigo-300 bg-clip-text text-transparent">
            MouSou Compiler <span className="text-xs px-1.5 py-0.5 rounded bg-blue-500/20 border border-blue-500/30 text-blue-300 font-mono">v2.0</span>
          </span>
        </div>

        <div className="h-5 w-px bg-zinc-700 hidden sm:block" />

        {/* Project Selector */}
        {isAuthenticated && (
          <div className="flex items-center space-x-2">
            <select
              value={currentProject?.id || ''}
              onChange={(e) => selectProject(e.target.value)}
              className="bg-zinc-800 text-sm text-zinc-200 border border-zinc-700 rounded-md px-2.5 py-1 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer max-w-[160px] sm:max-w-[220px] truncate"
            >
              {projects.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name} ({p.language})
                </option>
              ))}
            </select>

            <button
              onClick={() => setProjectModalOpen(true)}
              title="Create New Project"
              className="p-1.5 text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800 rounded-md transition-colors"
            >
              <FolderPlus className="w-4 h-4" />
            </button>
          </div>
        )}
      </div>

      {/* Center: Language Badge */}
      {currentProject && (
        <div className="hidden md:flex items-center space-x-2 text-xs font-mono px-2.5 py-1 bg-zinc-900 border border-zinc-800 rounded-full text-zinc-300">
          <Code2 className="w-3.5 h-3.5 text-blue-400" />
          <span className="uppercase font-semibold text-blue-300">{currentProject.language}</span>
          <span className="text-zinc-500">|</span>
          <span className="text-zinc-400">Sandbox: 256MB / 1 CPU</span>
        </div>
      )}

      {/* Right: Actions (Run, AI, Auth) */}
      <div className="flex items-center space-x-2.5">
        {/* Run Code Button */}
        <button
          onClick={() => runCode(false)}
          disabled={isRunning || !currentProject}
          title="Run Code (Ctrl + Enter)"
          className={`flex items-center space-x-2 px-3.5 py-1.5 rounded-md font-medium text-sm transition-all shadow-sm ${
            isRunning
              ? 'bg-amber-600/80 text-amber-100 cursor-not-allowed'
              : 'bg-emerald-600 hover:bg-emerald-500 text-white active:scale-95 shadow-emerald-950'
          }`}
        >
          {isRunning ? (
            <>
              <Loader2 className="w-4 h-4 animate-spin" />
              <span>Running...</span>
            </>
          ) : (
            <>
              <Play className="w-4 h-4 fill-white" />
              <span>Run Code</span>
              <kbd className="hidden lg:inline-block ml-1 px-1 text-[10px] bg-emerald-700/50 rounded text-emerald-200">
                Ctrl+↵
              </kbd>
            </>
          )}
        </button>

        {/* AI Assistant Button */}
        <button
          onClick={toggleAIPanel}
          title="Toggle AI Copilot"
          className={`flex items-center space-x-1.5 px-3 py-1.5 rounded-md text-sm font-medium border transition-colors ${
            isAIPanelOpen
              ? 'bg-purple-600/20 text-purple-300 border-purple-500/50 shadow-sm shadow-purple-950'
              : 'bg-zinc-800 hover:bg-zinc-700/70 text-zinc-300 border-zinc-700'
          }`}
        >
          <Sparkles className="w-4 h-4 text-purple-400" />
          <span className="hidden sm:inline">AI Copilot</span>
        </button>

        {/* User Auth */}
        {isAuthenticated ? (
          <div className="flex items-center space-x-2 pl-2 border-l border-zinc-700">
            <div
              className="w-8 h-8 rounded-full bg-blue-600/30 border border-blue-500/40 text-blue-300 flex items-center justify-center font-semibold text-xs"
              title={user?.email || 'Logged In'}
            >
              {(user?.firstName?.[0] || user?.email?.[0] || 'U').toUpperCase()}
            </div>
            <button
              onClick={logout}
              title="Sign Out"
              className="p-1.5 text-zinc-400 hover:text-red-400 hover:bg-zinc-800 rounded-md transition-colors"
            >
              <LogOut className="w-4 h-4" />
            </button>
          </div>
        ) : (
          <button
            onClick={() => setAuthModalOpen(true)}
            className="flex items-center space-x-1.5 px-3 py-1.5 rounded-md text-sm bg-blue-600 hover:bg-blue-500 text-white font-medium transition-colors"
          >
            <LogIn className="w-4 h-4" />
            <span>Sign In</span>
          </button>
        )}
      </div>
    </header>
  );
};
