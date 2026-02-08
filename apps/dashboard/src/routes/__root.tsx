import { createRootRouteWithContext, Outlet, Link } from '@tanstack/react-router';
import type { QueryClient } from '@tanstack/react-query';
import { Bell, Settings, HelpCircle, Search, LogOut } from 'lucide-react';
import { exitToEhr } from '@/lib/ehrExit';

interface RouterContext {
  queryClient: QueryClient;
}

export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootLayout,
});

function RootLayout() {
  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <header className="h-14 bg-slate-800 sticky top-0 z-50">
        <div className="h-full max-w-[1400px] mx-auto px-6 flex items-center justify-between">
          {/* Logo */}
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-teal flex items-center justify-center">
              <span className="text-white font-bold">AS</span>
            </div>
            <div>
              <h1 className="font-semibold text-white">AuthScript</h1>
              <p className="text-xs text-slate-400">Prior Authorization</p>
            </div>
          </div>

          {/* Search Bar */}
          <div className="hidden md:flex items-center gap-2 px-4 py-2 rounded-xl bg-slate-700/50 border border-slate-600 w-80">
            <Search className="w-4 h-4 text-slate-400" />
            <input 
              type="text" 
              placeholder="Search patients, PA requests..." 
              className="bg-transparent text-sm outline-none flex-1 text-white placeholder:text-slate-400"
            />
            <kbd className="px-1.5 py-0.5 rounded bg-slate-600 border border-slate-500 text-[10px] text-slate-300 font-mono">⌘K</kbd>
          </div>

          {/* Right Actions */}
          <div className="flex items-center gap-1">
            {/* Status */}
            <div className="flex items-center gap-2 px-3 py-1.5 rounded-full bg-teal/20 text-teal text-xs font-medium mr-2">
              <span className="relative flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-teal opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-teal"></span>
              </span>
              Live
            </div>
            
            <button className="p-2 rounded-lg hover:bg-slate-700 transition-colors relative click-effect">
              <Bell className="w-5 h-5 text-slate-400" />
              <span className="absolute top-1.5 right-1.5 w-2 h-2 bg-teal rounded-full" />
            </button>
            
            <Link to="/help" className="p-2 rounded-lg hover:bg-slate-700 transition-colors click-effect">
              <HelpCircle className="w-5 h-5 text-slate-400" />
            </Link>
            
            <button className="p-2 rounded-lg hover:bg-slate-700 transition-colors click-effect">
              <Settings className="w-5 h-5 text-slate-400" />
            </button>
            
            <div className="w-px h-8 bg-slate-600 mx-2" />

            {/* Exit to EHR */}
            <button
              type="button"
              onClick={exitToEhr}
              className="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-slate-700 transition-colors text-slate-300 text-sm font-medium click-effect"
              title="Exit AuthScript and return to athenahealth"
            >
              <LogOut className="w-4 h-4" />
              <span className="hidden sm:inline">Exit to EHR</span>
            </button>
            
            {/* User Avatar */}
            <div className="w-8 h-8 rounded-lg bg-teal flex items-center justify-center text-white font-semibold text-sm">
              FC
            </div>
          </div>
        </div>
      </header>

      <main className="max-w-[1400px] mx-auto">
        <Outlet />
      </main>
    </div>
  );
}
