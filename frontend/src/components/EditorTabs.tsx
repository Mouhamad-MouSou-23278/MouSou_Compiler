import React from 'react';
import { X, Star, FileCode } from 'lucide-react';
import { useAppStore } from '../store/useAppStore';

export const EditorTabs: React.FC = () => {
  const { openTabs, activeFileId, setActiveFileId, closeTab } = useAppStore();

  if (openTabs.length === 0) return null;

  return (
    <div className="h-9 bg-[#1e1e24] border-b border-[#2d2d38] flex items-center overflow-x-auto select-none shrink-0 scrollbar-none">
      {openTabs.map((tab) => {
        const isActive = tab.id === activeFileId;
        return (
          <div
            key={tab.id}
            onClick={() => setActiveFileId(tab.id)}
            className={`h-full px-3 flex items-center space-x-2 text-xs border-r border-[#2d2d38] cursor-pointer transition-colors ${
              isActive
                ? 'bg-[#18181b] text-zinc-100 font-medium border-t-2 border-t-blue-500'
                : 'bg-[#1e1e24] text-zinc-400 hover:bg-zinc-800/50 hover:text-zinc-200 border-t-2 border-t-transparent'
            }`}
          >
            <FileCode className={`w-3.5 h-3.5 ${isActive ? 'text-blue-400' : 'text-zinc-500'}`} />
            <span className="font-mono">{tab.path}</span>
            {tab.isEntryPoint && (
              <span title="Entry Point">
                <Star className="w-3 h-3 text-amber-400 fill-amber-400/80" />
              </span>
            )}
            <button
              onClick={(e) => {
                e.stopPropagation();
                closeTab(tab.id);
              }}
              className="p-0.5 hover:bg-zinc-700 rounded text-zinc-400 hover:text-zinc-100"
            >
              <X className="w-3 h-3" />
            </button>
          </div>
        );
      })}
    </div>
  );
};
