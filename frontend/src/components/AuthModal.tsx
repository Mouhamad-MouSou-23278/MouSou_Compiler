import React, { useState } from 'react';
import { X, Lock, Mail, User as UserIcon, Loader2 } from 'lucide-react';
import { useAppStore } from '../store/useAppStore';
import { apiClient } from '../api/client';

export const AuthModal: React.FC = () => {
  const { isAuthModalOpen, setAuthModalOpen, setAuth, fetchProjects } = useAppStore();
  const [isRegister, setIsRegister] = useState(false);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  if (!isAuthModalOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      if (isRegister) {
        const res = await apiClient.auth.register({ email, password, firstName, lastName });
        setAuth(
          {
            id: res.data.userId,
            email: res.data.email,
            firstName: res.data.firstName,
            lastName: res.data.lastName,
            role: res.data.role,
            emailConfirmed: true,
            createdAt: new Date().toISOString(),
          },
          res.data.accessToken
        );
      } else {
        const res = await apiClient.auth.login({ email, password });
        setAuth(
          {
            id: res.data.userId,
            email: res.data.email,
            firstName: res.data.firstName,
            lastName: res.data.lastName,
            role: res.data.role,
            emailConfirmed: true,
            createdAt: new Date().toISOString(),
          },
          res.data.accessToken
        );
      }

      await fetchProjects();
    } catch (err: any) {
      setError(err.response?.data?.detail || err.response?.data?.message || 'Authentication failed. Please verify credentials.');
    } finally {
      setLoading(false);
    }
  };

  const fillDemoAdmin = () => {
    setEmail('admin@mousou.dev');
    setPassword('Admin123!');
    setIsRegister(false);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
      <div className="bg-[#202028] border border-zinc-700 rounded-xl max-w-md w-full p-6 shadow-2xl relative">
        <button
          onClick={() => setAuthModalOpen(false)}
          className="absolute top-4 right-4 text-zinc-400 hover:text-zinc-200"
        >
          <X className="w-5 h-5" />
        </button>

        <h2 className="text-xl font-bold text-zinc-100 mb-1">
          {isRegister ? 'Create Your Account' : 'Welcome to MouSou Compiler'}
        </h2>
        <p className="text-xs text-zinc-400 mb-6">
          {isRegister
            ? 'Start writing, compiling, and running sandboxed code in seconds.'
            : 'Sign in to access your saved multi-file projects.'}
        </p>

        {error && (
          <div className="p-3 mb-4 bg-rose-950/40 border border-rose-800 text-rose-300 text-xs rounded-lg">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          {isRegister && (
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-zinc-300 mb-1">First Name</label>
                <div className="relative">
                  <UserIcon className="w-4 h-4 text-zinc-500 absolute left-3 top-2.5" />
                  <input
                    type="text"
                    required
                    value={firstName}
                    onChange={(e) => setFirstName(e.target.value)}
                    placeholder="John"
                    className="w-full bg-zinc-900 border border-zinc-700 rounded-lg pl-9 pr-3 py-2 text-xs text-zinc-200 focus:outline-none focus:border-blue-500"
                  />
                </div>
              </div>
              <div>
                <label className="block text-xs font-medium text-zinc-300 mb-1">Last Name</label>
                <input
                  type="text"
                  required
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  placeholder="Doe"
                  className="w-full bg-zinc-900 border border-zinc-700 rounded-lg px-3 py-2 text-xs text-zinc-200 focus:outline-none focus:border-blue-500"
                />
              </div>
            </div>
          )}

          <div>
            <label className="block text-xs font-medium text-zinc-300 mb-1">Email Address</label>
            <div className="relative">
              <Mail className="w-4 h-4 text-zinc-500 absolute left-3 top-2.5" />
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="name@company.com"
                className="w-full bg-zinc-900 border border-zinc-700 rounded-lg pl-9 pr-3 py-2 text-xs text-zinc-200 focus:outline-none focus:border-blue-500"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-medium text-zinc-300 mb-1">Password</label>
            <div className="relative">
              <Lock className="w-4 h-4 text-zinc-500 absolute left-3 top-2.5" />
              <input
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full bg-zinc-900 border border-zinc-700 rounded-lg pl-9 pr-3 py-2 text-xs text-zinc-200 focus:outline-none focus:border-blue-500"
              />
            </div>
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-blue-600 hover:bg-blue-500 text-white font-medium py-2 rounded-lg text-xs transition-colors flex items-center justify-center space-x-2 shadow-sm shadow-blue-950"
          >
            {loading && <Loader2 className="w-4 h-4 animate-spin" />}
            <span>{isRegister ? 'Create Account' : 'Sign In'}</span>
          </button>
        </form>

        <div className="mt-4 pt-4 border-t border-zinc-700/60 flex items-center justify-between text-xs">
          <button
            onClick={() => setIsRegister(!isRegister)}
            className="text-blue-400 hover:underline"
          >
            {isRegister ? 'Already have an account? Sign In' : 'Need an account? Register'}
          </button>

          <button
            onClick={fillDemoAdmin}
            className="text-amber-400/90 hover:text-amber-300 underline font-mono text-[11px]"
          >
            Fill Demo Admin
          </button>
        </div>
      </div>
    </div>
  );
};
