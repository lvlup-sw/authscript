import { createFileRoute, useNavigate } from '@tanstack/react-router';
import { useEffect, useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { initiateSmartLaunch, type SmartLaunchContext } from '@/lib/smartAuth';

export const Route = createFileRoute('/smart-launch')({
  component: SmartLaunchPage,
  validateSearch: (search: Record<string, unknown>) => ({
    iss: (search.iss as string) || '',
    launch: (search.launch as string) || '',
  }),
});

function SmartLaunchPage() {
  const { iss, launch } = Route.useSearch();
  const navigate = useNavigate();
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [launchContext, setLaunchContext] = useState<SmartLaunchContext | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!iss || !launch) {
      setStatus('error');
      setError('Missing required SMART launch parameters (iss, launch)');
      return;
    }

    async function performLaunch() {
      try {
        const context = await initiateSmartLaunch(iss, launch);
        setLaunchContext(context);
        setStatus('ready');
      } catch (err) {
        setStatus('error');
        setError(err instanceof Error ? err.message : 'Unknown error during SMART launch');
      }
    }

    performLaunch();
  }, [iss, launch]);

  if (status === 'loading') {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center space-y-4">
          <div className="animate-spin h-8 w-8 border-4 border-primary border-t-transparent rounded-full mx-auto" />
          <p className="text-muted-foreground">Authenticating with Epic...</p>
        </div>
      </div>
    );
  }

  if (status === 'error') {
    return (
      <div className="max-w-md mx-auto mt-12">
        <Card className="border-destructive">
          <CardHeader>
            <CardTitle className="text-destructive">Launch Error</CardTitle>
            <CardDescription>
              Unable to complete SMART on FHIR launch
            </CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground mb-4">{error}</p>
            <Button variant="outline" onClick={() => navigate({ to: '/' })}>
              Return to Dashboard
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">AuthScript SMART App</h1>
        <p className="text-muted-foreground mt-2">
          Manual prior authorization workflow
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Patient Context</CardTitle>
          <CardDescription>
            SMART launch context from Epic
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-muted-foreground">Patient ID:</span>
              <p className="font-mono">{launchContext?.patientId || 'N/A'}</p>
            </div>
            <div>
              <span className="text-muted-foreground">Encounter:</span>
              <p className="font-mono">{launchContext?.encounterId || 'N/A'}</p>
            </div>
          </div>

          <div className="flex gap-4 pt-4">
            <Button>Run Analysis</Button>
            <Button variant="outline">View Patient Data</Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
