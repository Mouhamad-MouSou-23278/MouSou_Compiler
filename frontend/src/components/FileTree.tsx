import React, { useState } from 'react';
import { FileCode, FilePlus, Star, Trash2, ChevronRight, ChevronDown, Folder } from 'lucide-react';
import { useAppStore } from '../store/useAppStore';
import { ProjectFile } from '../types';

export const FileTree: React.FC = () => {
  const { files, activeFileId, openTab, createNewFile, deleteFile, currentProject } = useAppStore();
  const [isCreating, setIsCreating] = useState(false);
  const [newFilePath, setNewFilePath] = useState('');

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newFilePath.trim()) return;
    await createNewFile(newFilePath.trim(), files.length === 0);
    setNewFilePath('');
    setIsCreating(false);
  };

  const getFileExtension = (path: string) => {
    const parts = path.split('.');
    return parts.length > 1 ? parts.pop()?.toLowerCase() : '';
  };

  return (
    <aside className="w-60 bg-[#1e1e24] border-r border-[#2d2d38] flex flex-col h-full select-none shrink-0">
      {/* Header */}
      <div className="h-10 px-3 border-b border-[#2d2d38] flex items-center justify-between text-xs font-semibold text-zinc-400 tracking-wider uppercase">
        <div className="flex items-center space-x-1.5 truncate">
          <Folder className="w-3.5 h-3.5 text-blue-400 shrink-0" />
          <span className="truncate">{currentProject?.name || 'Project Explorer'}</span>
        </div>
        <button
          onClick={() => setIsCreating(!isCreating)}
          title="New File"
          className="p-1 hover:bg-zinc-800 rounded text-zinc-400 hover:text-zinc-100 transition-colors"
        >
          <FilePlus className="w-4 h-4" />
        </button>
      </div>

      {/* New File Inline Input */}
      {isCreating && (
        <form onSubmit={handleCreate} className="p-2 border-b border-[#2d2d38] bg-zinc-900/50">
          <input
            type="text"
            placeholder="filename.py (or path/file.py)"
            value={newFilePath}
            onChange={(e) => setNewFilePath(e.target.value)}
            autoFocus
            className="w-full text-xs bg-zinc-950 text-zinc-200 border border-blue-500 rounded px-2 py-1 focus:outline-none font-mono"
            onKeyDown={(e) => {
              if (e.key === 'Escape') setIsCreating(false);
            }}
          />
        </form>
      )}

      {/* Files List */}
      <div className="flex-1 overflow-y-auto py-1">
        {files.length === 0 ? (
          <div className="p-4 text-center text-zinc-500 text-xs">No files in project</div>
        ) : (
          files.map((file) => {
            const isActive = file.id === activeFileId;
            return (
              <div
                key={file.id}
                onClick={() => openTab(file)}
                className={`group flex items-center justify-between px-3 py-1.5 text-xs cursor-pointer transition-colors ${
                  isActive
                    ? 'bg-blue-600/20 text-blue-300 font-medium border-l-2 border-blue-500'
                    : 'text-zinc-300 hover:bg-zinc-800/60 hover:text-zinc-100 border-l-2 border-transparent'
                }`}
              >
                <div className="flex items-center space-x-2 truncate">
                  <FileCode className={`w-4 h-4 shrink-0 ${isActive ? 'text-blue-400' : 'text-zinc-500'}`} />
                  <span className="truncate font-mono">{file.path}</span>
                </div>

                <div className="flex items-center space-x-1 opacity-0 group-hover:opacity-100 transition-opacity">
                  {file.isEntryPoint && (
                    <span title="Main Entry Point">
                      <Star className="w-3.5 h-3.5 text-amber-400 fill-amber-400/80" />
                    </span>
                  )}
                  {files.length > 1 && (
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        if (confirm(`Delete file "${file.path}"?`)) {
                          deleteFile(file.id);
                        }
                      }}
                      className="p-0.5 hover:text-red-400 text-zinc-500 rounded"
                      title="Delete File"
                    >
                      <Trash2 className="w-3.5 h-3.5" />
                    </button>
                  )}
                </div>
              </div>
            );
          })
        )}
      </div>
    </aside>
  );
};
