import React, { useEffect, useRef } from 'react';
import Editor, { OnMount } from '@monaco-editor/react';
import { useAppStore } from '../store/useAppStore';

export const MonacoCodeEditor: React.FC = () => {
  const { files, activeFileId, updateFileContent, runCode, saveActiveFile } = useAppStore();
  const editorRef = useRef<any>(null);

  const activeFile = files.find((f) => f.id === activeFileId);

  const getLanguage = (path?: string) => {
    if (!path) return 'plaintext';
    const ext = path.split('.').pop()?.toLowerCase();
    switch (ext) {
      case 'py':
        return 'python';
      case 'js':
      case 'jsx':
        return 'javascript';
      case 'ts':
      case 'tsx':
        return 'typescript';
      case 'cs':
        return 'csharp';
      case 'go':
        return 'go';
      case 'cpp':
      case 'cc':
      case 'h':
      case 'hpp':
        return 'cpp';
      case 'rs':
        return 'rust';
      case 'json':
        return 'json';
      case 'md':
        return 'markdown';
      case 'sh':
        return 'shell';
      default:
        return 'plaintext';
    }
  };

  const handleEditorDidMount: OnMount = (editor, monaco) => {
    editorRef.current = editor;

    // Ctrl+Enter: Run Code
    editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.Enter, () => {
      runCode(false);
    });

    // Ctrl+S: Save File
    editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, () => {
      saveActiveFile();
    });
  };

  useEffect(() => {
    const handleGlobalKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
        e.preventDefault();
        runCode(false);
      } else if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault();
        saveActiveFile();
      }
    };

    window.addEventListener('keydown', handleGlobalKeyDown);
    return () => window.removeEventListener('keydown', handleGlobalKeyDown);
  }, [runCode, saveActiveFile]);

  if (!activeFile) {
    return (
      <div className="flex-1 flex items-center justify-center bg-[#18181b] text-zinc-500 text-sm">
        Select or create a file to start editing
      </div>
    );
  }

  return (
    <div className="flex-1 w-full h-full relative overflow-hidden bg-[#18181b]">
      <Editor
        height="100%"
        language={getLanguage(activeFile.path)}
        theme="vs-dark"
        value={activeFile.content}
        onChange={(val) => {
          if (val !== undefined && activeFile) {
            updateFileContent(activeFile.id, val);
          }
        }}
        onMount={handleEditorDidMount}
        options={{
          fontSize: 14,
          fontFamily: "'Fira Code', monospace",
          fontLigatures: true,
          minimap: { enabled: true, scale: 0.75 },
          scrollBeyondLastLine: false,
          automaticLayout: true,
          tabSize: 4,
          wordWrap: 'on',
          cursorBlinking: 'smooth',
          cursorSmoothCaretAnimation: 'on',
          smoothScrolling: true,
          renderLineHighlight: 'all',
          lineNumbersMinChars: 3,
        }}
      />
    </div>
  );
};
