import React, { useEffect } from 'react';
import { Navbar } from './components/Navbar';
import { FileTree } from './components/FileTree';
import { EditorTabs } from './components/EditorTabs';
import { MonacoCodeEditor } from './components/MonacoCodeEditor';
import { ConsoleOutput } from './components/ConsoleOutput';
import { AIPanel } from './components/AIPanel';
import { AuthModal } from './components/AuthModal';
import { ProjectModal } from './components/ProjectModal';
import { useAppStore } from './store/useAppStore';
import { apiClient } from './api/client';
import { signalRManager } from './signalr/executionHub';

export const App: React.FC = () => {
  const { setAuth, fetchProjects, addLog } = useAppStore();

  useEffect(() => {
    // Initial bootstrap: check active session
    const bootstrap = async () => {
      try {
        const profileRes = await apiClient.auth.getProfile();
        setAuth(profileRes.data, localStorage.getItem('access_token'));
      } catch {
        // Not logged in or expired
      }
      await fetchProjects();
    };

    bootstrap();

    // Attach SignalR log listener to append live streaming output into Zustand store
    const unsubscribe = signalRManager.onLog((log) => {
      addLog(log);
    });

    return () => {
      unsubscribe();
    };
  }, [setAuth, fetchProjects, addLog]);

  return (
    <div className="h-screen w-screen flex flex-col overflow-hidden bg-[#18181b] text-gray-100 font-sans select-none">
      {/* Top Navigation Bar */}
      <Navbar />

      {/* Main Studio Area */}
      <div className="flex-1 flex overflow-hidden">
        {/* Left Sidebar: File Tree Explorer */}
        <FileTree />

        {/* Center: Editor Tabs, Code Editor, and Console Output */}
        <main className="flex-1 flex flex-col min-w-0 bg-[#18181b] overflow-hidden">
          <EditorTabs />
          <div className="flex-1 flex flex-col min-h-0 relative">
            <MonacoCodeEditor />
          </div>
          <ConsoleOutput />
        </main>

        {/* Right Drawer: AI Copilot & Bug Doctor */}
        <AIPanel />
      </div>

      {/* Interactive Modals */}
      <AuthModal />
      <ProjectModal />
    </div>
  );
};

export default App;
