import React, { useState } from 'react';
import { X, FolderPlus, Loader2 } from 'lucide-react';
import { useAppStore } from '../store/useAppStore';
import { apiClient } from '../api/client';

export const ProjectModal: React.FC = () => {
  const { isProjectModalOpen, setProjectModalOpen, fetchProjects, selectProject } = useAppStore();
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [language, setLanguage] = useState('python');
  const [loading, setLoading] = useState(false);

  if (!isProjectModalOpen) return null;

  const languages = [
    { id: 'python', name: 'Python 3.11', desc: 'Standard scripting, data science & algorithms' },
    { id: 'javascript', name: 'Node.js 20', desc: 'Modern JavaScript runtime with async/await' },
    { id: 'csharp', name: 'C# 12 (.NET 8)', desc: 'Enterprise object-oriented computing' },
    { id: 'go', name: 'Go 1.22', desc: 'High concurrency systems language' },
    { id: 'cpp', name: 'C++ 20 (GCC)', desc: 'High-performance native code' },
    { id: 'rust', name: 'Rust (rustc)', desc: 'Memory safe systems language' },
  ];

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;
    setLoading(true);

    try {
      const res = await apiClient.projects.create({
        name: name.trim(),
        description: description.trim() || undefined,
        language,
      });

      await fetchProjects();
      await selectProject(res.data.id);
      setProjectModalOpen(false);
      setName('');
      setDescription('');
    } catch (err) {
      console.error('Failed to create project:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
      <div className="bg-[#202028] border border-zinc-700 rounded-xl max-w-lg w-full p-6 shadow-2xl relative">
        <button
          onClick={() => setProjectModalOpen(false)}
          className="absolute top-4 right-4 text-zinc-400 hover:text-zinc-200"
        >
          <X className="w-5 h-5" />
        </button>

        <div className="flex items-center space-x-2 text-blue-400 mb-1">
          <FolderPlus className="w-5 h-5" />
          <h2 className="text-lg font-bold text-zinc-100">Create New Project</h2>
        </div>
        <p className="text-xs text-zinc-400 mb-6">
          Set up a multi-file sandboxed workspace with custom runtime flags and dependencies.
        </p>

        <form onSubmit={handleCreate} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-zinc-300 mb-1">Project Name</label>
            <input
              type="text"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Microservices Simulation or Graph Algorithm"
              className="w-full bg-zinc-900 border border-zinc-700 rounded-lg px-3 py-2 text-xs text-zinc-200 focus:outline-none focus:border-blue-500"
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-zinc-300 mb-1">Description (Optional)</label>
            <textarea
              rows={2}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="What does this project do?"
              className="w-full bg-zinc-900 border border-zinc-700 rounded-lg px-3 py-2 text-xs text-zinc-200 focus:outline-none focus:border-blue-500 resize-none"
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-zinc-300 mb-2">Target Language & Sandbox</label>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
              {languages.map((lang) => (
                <div
                  key={lang.id}
                  onClick={() => setLanguage(lang.id)}
                  className={`p-3 rounded-lg border cursor-pointer transition-all ${
                    language === lang.id
                      ? 'bg-blue-600/15 border-blue-500 text-blue-300'
                      : 'bg-zinc-900/60 border-zinc-800 text-zinc-300 hover:bg-zinc-800/40'
                  }`}
                >
                  <div className="font-semibold text-xs">{lang.name}</div>
                  <div className="text-[10px] text-zinc-500 mt-0.5">{lang.desc}</div>
                </div>
              ))}
            </div>
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-blue-600 hover:bg-blue-500 text-white font-medium py-2 rounded-lg text-xs transition-colors flex items-center justify-center space-x-2 shadow-sm shadow-blue-950 mt-4"
          >
            {loading && <Loader2 className="w-4 h-4 animate-spin" />}
            <span>Initialize Project Workspace</span>
          </button>
        </form>
      </div>
    </div>
  );
};
