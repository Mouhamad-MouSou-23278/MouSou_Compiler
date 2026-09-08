import React, { useState } from 'react';
import {
  Sparkles,
  X,
  Wrench,
  HelpCircle,
  ShieldAlert,
  Zap,
  CheckCircle2,
  FileCheck,
  Loader2,
  RefreshCw,
} from 'lucide-react';
import { useAppStore } from '../store/useAppStore';
import { AIJobType } from '../types';

export const AIPanel: React.FC = () => {
  const {
    isAIPanelOpen,
    toggleAIPanel,
    aiLoading,
    aiResponse,
    requestAIAnalysis,
    requestAutoFix,
    currentExecution,
  } = useAppStore();

  const [customPrompt, setCustomPrompt] = useState('');

  if (!isAIPanelOpen) return null;

  const handleAction = (type: AIJobType) => {
    requestAIAnalysis(type, customPrompt);
    setCustomPrompt('');
  };

  const hasFailedExecution = currentExecution?.status === 'Failed' || currentExecution?.error;

  return (
    <div className="w-80 sm:w-96 bg-[#1a1a20] border-l border-[#2d2d38] flex flex-col h-full z-10 shrink-0 select-none shadow-xl">
      {/* Header */}
      <div className="h-10 px-4 border-b border-[#2d2d38] flex items-center justify-between bg-[#202028]">
        <div className="flex items-center space-x-2 text-purple-400 font-semibold text-xs">
          <Sparkles className="w-4 h-4" />
          <span>AI Copilot & Code Doctor</span>
        </div>
        <button
          onClick={toggleAIPanel}
          className="p-1 hover:bg-zinc-800 rounded text-zinc-400 hover:text-zinc-100"
        >
          <X className="w-4 h-4" />
        </button>
      </div>

      {/* Quick Action Grid */}
      <div className="p-3 border-b border-[#2d2d38] bg-zinc-900/40 grid grid-cols-2 gap-2">
        <button
          onClick={() => handleAction('Explain')}
          disabled={aiLoading}
          className="flex items-center space-x-1.5 p-2 bg-zinc-800/80 hover:bg-zinc-700/80 text-zinc-200 text-xs rounded border border-zinc-700/70 transition-colors"
        >
          <HelpCircle className="w-3.5 h-3.5 text-blue-400 shrink-0" />
          <span className="truncate">Explain Code</span>
        </button>

        <button
          onClick={() => handleAction('BugFix')}
          disabled={aiLoading}
          className="flex items-center space-x-1.5 p-2 bg-zinc-800/80 hover:bg-zinc-700/80 text-zinc-200 text-xs rounded border border-zinc-700/70 transition-colors"
        >
          <Wrench className="w-3.5 h-3.5 text-amber-400 shrink-0" />
          <span className="truncate">Find Bugs</span>
        </button>

        <button
          onClick={() => handleAction('TestGeneration')}
          disabled={aiLoading}
          className="flex items-center space-x-1.5 p-2 bg-zinc-800/80 hover:bg-zinc-700/80 text-zinc-200 text-xs rounded border border-zinc-700/70 transition-colors"
        >
          <FileCheck className="w-3.5 h-3.5 text-emerald-400 shrink-0" />
          <span className="truncate">Generate Tests</span>
        </button>

        <button
          onClick={() => handleAction('PerformanceAnalysis')}
          disabled={aiLoading}
          className="flex items-center space-x-1.5 p-2 bg-zinc-800/80 hover:bg-zinc-700/80 text-zinc-200 text-xs rounded border border-zinc-700/70 transition-colors"
        >
          <Zap className="w-3.5 h-3.5 text-yellow-400 shrink-0" />
          <span className="truncate">Optimize</span>
        </button>
      </div>

      {/* Auto-Fix Banner for Failed Executions */}
      {hasFailedExecution && (
        <div className="p-3 bg-amber-950/40 border-b border-amber-900/50 flex items-center justify-between">
          <div className="flex items-center space-x-2 text-xs text-amber-300">
            <ShieldAlert className="w-4 h-4 text-amber-400 shrink-0" />
            <span>Execution failed with errors</span>
          </div>
          <button
            onClick={requestAutoFix}
            disabled={aiLoading}
            className="flex items-center space-x-1 px-2.5 py-1 bg-amber-600 hover:bg-amber-500 text-white rounded text-xs font-semibold shadow-sm transition-colors"
          >
            <RefreshCw className={`w-3 h-3 ${aiLoading ? 'animate-spin' : ''}`} />
            <span>Auto-Fix & Rerun</span>
          </button>
        </div>
      )}

      {/* Content / Suggestions Area */}
      <div className="flex-1 overflow-y-auto p-4 select-text font-sans text-xs leading-relaxed space-y-4">
        {aiLoading ? (
          <div className="h-48 flex flex-col items-center justify-center space-y-2 text-purple-400">
            <Loader2 className="w-6 h-6 animate-spin" />
            <span className="text-zinc-400 text-xs">Analyzing code with OpenRouter AI...</span>
          </div>
        ) : aiResponse ? (
          <div className="space-y-3">
            <div className="flex items-center space-x-1.5 text-purple-400 font-semibold text-xs border-b border-zinc-800 pb-1.5">
              <CheckCircle2 className="w-4 h-4 text-purple-400" />
              <span>AI Recommendation ({aiResponse.type})</span>
            </div>

            <div className="text-zinc-200 whitespace-pre-wrap font-sans bg-zinc-900/60 p-3 rounded-lg border border-zinc-800">
              {aiResponse.analysis}
            </div>

            {aiResponse.diffPatch && (
              <div className="space-y-1">
                <div className="text-[11px] font-mono text-zinc-400">Structured Patch:</div>
                <pre className="p-3 bg-[#141416] border border-zinc-800 rounded font-mono text-[11px] text-zinc-300 overflow-x-auto whitespace-pre">
                  {aiResponse.diffPatch}
                </pre>
              </div>
            )}
          </div>
        ) : (
          <div className="text-center py-12 text-zinc-500 text-xs">
            Ask the AI Copilot to explain your algorithm, debug runtime exceptions, or generate test cases!
          </div>
        )}
      </div>

      {/* Custom Prompt Input */}
      <div className="p-3 border-t border-[#2d2d38] bg-[#202028]">
        <div className="flex space-x-2">
          <input
            type="text"
            placeholder="Ask AI Copilot anything..."
            value={customPrompt}
            onChange={(e) => setCustomPrompt(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' && customPrompt.trim()) {
                handleAction('Explain');
              }
            }}
            disabled={aiLoading}
            className="flex-1 bg-zinc-900 border border-zinc-700 rounded px-3 py-1.5 text-xs text-zinc-200 focus:outline-none focus:border-purple-500 font-sans"
          />
          <button
            onClick={() => handleAction('Explain')}
            disabled={aiLoading || !customPrompt.trim()}
            className="px-3 py-1.5 bg-purple-600 hover:bg-purple-500 disabled:bg-zinc-800 text-white text-xs font-semibold rounded transition-colors"
          >
            Ask
          </button>
        </div>
      </div>
    </div>
  );
};
