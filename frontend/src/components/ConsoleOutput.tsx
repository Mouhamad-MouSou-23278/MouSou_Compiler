import React, { useState, useRef, useEffect } from 'react';
import { Terminal, Send, Trash2, Gauge, Clock, ShieldCheck, AlertCircle } from 'lucide-react';
import { useAppStore } from '../store/useAppStore';

export const ConsoleOutput: React.FC = () => {
  const { logs, clearLogs, sendStdin, currentExecution, isRunning } = useAppStore();
  const [stdinText, setStdinText] = useState('');
  const [activeTab, setActiveTab] = useState<'console' | 'stdin' | 'metrics'>('console');
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [logs]);

  const handleSendStdin = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!stdinText.trim()) return;
    await sendStdin(stdinText);
    setStdinText('');
  };

  const getMetrics = () => {
    if (!currentExecution?.metricsJson) return null;
    try {
      return JSON.parse(currentExecution.metricsJson);
    } catch {
      return null;
    }
  };

  const metrics = getMetrics();

  return (
    <div className="h-64 bg-[#141416] border-t border-[#2d2d38] flex flex-col shrink-0 select-none">
      {/* Console Header */}
      <div className="h-9 px-3 bg-[#1e1e24] border-b border-[#2d2d38] flex items-center justify-between">
        <div className="flex items-center space-x-2">
          <button
            onClick={() => setActiveTab('console')}
            className={`flex items-center space-x-1.5 px-2.5 py-1 rounded text-xs font-medium transition-colors ${
              activeTab === 'console' ? 'bg-[#141416] text-blue-400' : 'text-zinc-400 hover:text-zinc-200'
            }`}
          >
            <Terminal className="w-3.5 h-3.5" />
            <span>Execution Terminal</span>
            {logs.length > 0 && (
              <span className="ml-1 px-1.5 py-0.2 text-[10px] bg-zinc-800 rounded-full text-zinc-300">
                {logs.length}
              </span>
            )}
          </button>

          <button
            onClick={() => setActiveTab('stdin')}
            className={`flex items-center space-x-1.5 px-2.5 py-1 rounded text-xs font-medium transition-colors ${
              activeTab === 'stdin' ? 'bg-[#141416] text-blue-400' : 'text-zinc-400 hover:text-zinc-200'
            }`}
          >
            <Send className="w-3.5 h-3.5" />
            <span>Interactive Stdin</span>
          </button>

          <button
            onClick={() => setActiveTab('metrics')}
            className={`flex items-center space-x-1.5 px-2.5 py-1 rounded text-xs font-medium transition-colors ${
              activeTab === 'metrics' ? 'bg-[#141416] text-blue-400' : 'text-zinc-400 hover:text-zinc-200'
            }`}
          >
            <Gauge className="w-3.5 h-3.5" />
            <span>Metrics & Sandbox</span>
          </button>
        </div>

        <div className="flex items-center space-x-3">
          {currentExecution && (
            <div className="flex items-center space-x-1.5 text-xs font-mono">
              <span className="text-zinc-500">Status:</span>
              <span
                className={`font-semibold ${
                  currentExecution.status === 'Completed'
                    ? 'text-emerald-400'
                    : currentExecution.status === 'Running' || currentExecution.status === 'Queued'
                    ? 'text-amber-400 animate-pulse'
                    : 'text-rose-400'
                }`}
              >
                {currentExecution.status}
              </span>
            </div>
          )}

          <button
            onClick={clearLogs}
            title="Clear Console"
            className="p-1 hover:bg-zinc-800 rounded text-zinc-400 hover:text-zinc-100 transition-colors"
          >
            <Trash2 className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>

      {/* Tab Content */}
      <div className="flex-1 overflow-hidden relative">
        {activeTab === 'console' && (
          <div ref={scrollRef} className="h-full overflow-y-auto p-3 font-mono text-xs select-text leading-relaxed">
            {logs.length === 0 ? (
              <div className="h-full flex items-center justify-center text-zinc-600">
                Click &quot;Run Code&quot; or press Ctrl+Enter to execute program in sandbox
              </div>
            ) : (
              logs.map((log) => (
                <div
                  key={log.id}
                  className={`whitespace-pre-wrap ${
                    log.stream === 'stderr'
                      ? 'text-rose-400 font-medium'
                      : log.stream === 'system'
                      ? 'text-cyan-400/90'
                      : 'text-zinc-200'
                  }`}
                >
                  {log.text}
                </div>
              ))
            )}
          </div>
        )}

        {activeTab === 'stdin' && (
          <div className="p-4 flex flex-col h-full space-y-3">
            <p className="text-xs text-zinc-400">
              Send interactive standard input (stdin) directly to your running sandboxed process.
            </p>
            <form onSubmit={handleSendStdin} className="flex space-x-2">
              <input
                type="text"
                value={stdinText}
                onChange={(e) => setStdinText(e.target.value)}
                placeholder="Type input line here..."
                disabled={!isRunning}
                className="flex-1 bg-zinc-900 border border-zinc-700 rounded px-3 py-1.5 text-xs text-zinc-200 font-mono focus:outline-none focus:border-blue-500 disabled:opacity-50"
              />
              <button
                type="submit"
                disabled={!isRunning || !stdinText.trim()}
                className="px-4 py-1.5 bg-blue-600 hover:bg-blue-500 disabled:bg-zinc-800 text-white text-xs font-medium rounded flex items-center space-x-1.5 transition-colors"
              >
                <Send className="w-3.5 h-3.5" />
                <span>Send</span>
              </button>
            </form>
          </div>
        )}

        {activeTab === 'metrics' && (
          <div className="p-4 grid grid-cols-1 sm:grid-cols-3 gap-4 text-xs font-mono">
            <div className="p-3 bg-zinc-900/70 border border-zinc-800 rounded-lg flex items-center space-x-3">
              <Clock className="w-5 h-5 text-blue-400" />
              <div>
                <div className="text-zinc-500 text-[10px] uppercase">Execution Duration</div>
                <div className="text-sm font-semibold text-zinc-200">
                  {metrics?.executionTimeMs ? `${metrics.executionTimeMs.toFixed(1)} ms` : '--'}
                </div>
              </div>
            </div>

            <div className="p-3 bg-zinc-900/70 border border-zinc-800 rounded-lg flex items-center space-x-3">
              <ShieldCheck className="w-5 h-5 text-emerald-400" />
              <div>
                <div className="text-zinc-500 text-[10px] uppercase">Sandbox Isolation</div>
                <div className="text-sm font-semibold text-emerald-300">Network: None | Non-Root</div>
              </div>
            </div>

            <div className="p-3 bg-zinc-900/70 border border-zinc-800 rounded-lg flex items-center space-x-3">
              <AlertCircle className="w-5 h-5 text-amber-400" />
              <div>
                <div className="text-zinc-500 text-[10px] uppercase">Exit Code</div>
                <div className="text-sm font-semibold text-zinc-200">
                  {currentExecution?.exitCode !== undefined && currentExecution.exitCode !== null
                    ? currentExecution.exitCode
                    : '--'}
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
