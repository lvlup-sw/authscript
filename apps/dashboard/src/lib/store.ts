// Simple store using localStorage for PA request management
import { 
  PARequest, 
  PATIENTS, 
  PROVIDERS, 
  generateMockPAData,
  Patient,
  Procedure,
  Medication
} from './mockData';

// Re-export PARequest type for use by other modules
export type { PARequest };

const STORAGE_KEY = 'authscript_pa_requests';
const CURRENT_PA_KEY = 'authscript_current_pa';

// Cookie helpers
export function setCookie(name: string, value: string, days: number = 7) {
  const expires = new Date(Date.now() + days * 864e5).toUTCString();
  document.cookie = `${name}=${encodeURIComponent(value)}; expires=${expires}; path=/; SameSite=Strict`;
}

export function getCookie(name: string): string | null {
  const value = document.cookie
    .split('; ')
    .find(row => row.startsWith(name + '='));
  return value ? decodeURIComponent(value.split('=')[1]) : null;
}

export function deleteCookie(name: string) {
  document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/`;
}

// Initialize with some mock data
function getInitialRequests(): PARequest[] {
  const patient1 = PATIENTS[0];
  const patient2 = PATIENTS[1];
  const patient3 = PATIENTS[2];
  const provider = PROVIDERS[0];

  return [
    {
      id: 'PA-001',
      ...generateMockPAData(
        patient1,
        { code: '72148', name: 'MRI Lumbar Spine w/o Contrast', category: 'imaging', requiresPA: true },
        { code: 'M54.5', name: 'Low Back Pain' },
        provider
      ),
      status: 'ready',
      createdAt: new Date(Date.now() - 300000).toISOString(),
      updatedAt: new Date(Date.now() - 300000).toISOString(),
    } as PARequest,
    {
      id: 'PA-002',
      ...generateMockPAData(
        patient2,
        { code: '27447', name: 'Total Knee Replacement', category: 'surgery', requiresPA: true },
        { code: 'M17.11', name: 'Primary Osteoarthritis, Right Knee' },
        provider
      ),
      status: 'ready',
      createdAt: new Date(Date.now() - 720000).toISOString(),
      updatedAt: new Date(Date.now() - 720000).toISOString(),
    } as PARequest,
    {
      id: 'PA-003',
      ...generateMockPAData(
        patient3,
        { code: '70553', name: 'MRI Brain w/ & w/o Contrast', category: 'imaging', requiresPA: true },
        { code: 'G43.909', name: 'Migraine, Unspecified' },
        provider
      ),
      status: 'ready',
      confidence: 58,
      createdAt: new Date(Date.now() - 2700000).toISOString(),
      updatedAt: new Date(Date.now() - 2700000).toISOString(),
    } as PARequest,
  ];
}

// Get all PA requests
export function getPARequests(): PARequest[] {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) {
      return JSON.parse(stored);
    }
  } catch (e) {
    console.error('Error loading PA requests:', e);
  }
  
  // Initialize with mock data
  const initialData = getInitialRequests();
  savePARequests(initialData);
  return initialData;
}

// Save all PA requests
export function savePARequests(requests: PARequest[]) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(requests));
  } catch (e) {
    console.error('Error saving PA requests:', e);
  }
}

// Get a single PA request by ID
export function getPARequest(id: string): PARequest | null {
  const requests = getPARequests();
  return requests.find(r => r.id === id) || null;
}

// Create a new PA request
export function createPARequest(
  patient: Patient,
  procedure: Procedure | Medication,
  diagnosis: { code: string; name: string },
  providerId: string = 'DR001'
): PARequest {
  const provider = PROVIDERS.find(p => p.id === providerId) || PROVIDERS[0];
  const requests = getPARequests();
  
  const newId = `PA-${String(requests.length + 1).padStart(3, '0')}`;
  const mockData = generateMockPAData(patient, procedure, diagnosis, provider);
  
  const newRequest: PARequest = {
    id: newId,
    ...mockData,
    status: 'draft',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  } as PARequest;
  
  requests.unshift(newRequest);
  savePARequests(requests);
  
  // Store current PA in cookie for the flow
  setCookie(CURRENT_PA_KEY, newId);
  
  return newRequest;
}

// Update a PA request
export function updatePARequest(id: string, updates: Partial<PARequest>): PARequest | null {
  const requests = getPARequests();
  const index = requests.findIndex(r => r.id === id);
  
  if (index === -1) return null;
  
  requests[index] = {
    ...requests[index],
    ...updates,
    updatedAt: new Date().toISOString(),
  };
  
  savePARequests(requests);
  return requests[index];
}

// Process a PA request (simulate AI processing)
export function processPARequest(id: string): Promise<PARequest | null> {
  return new Promise((resolve) => {
    // Update to processing status (use 1 so UI never shows 0%)
    updatePARequest(id, { status: 'processing', confidence: 1 });
    
    // Simulate AI processing time
    setTimeout(() => {
      const request = getPARequest(id);
      if (!request) {
        resolve(null);
        return;
      }
      
      // Generate confidence and update to ready
      const confidence = Math.floor(Math.random() * 30) + 70; // 70-100
      const updated = updatePARequest(id, { 
        status: 'ready', 
        confidence,
        criteria: request.criteria.map(c => ({
          ...c,
          met: c.met === null ? Math.random() > 0.3 : c.met
        }))
      });
      
      resolve(updated);
    }, 2000);
  });
}

// Submit a PA request
export function submitPARequest(id: string): PARequest | null {
  return updatePARequest(id, { status: 'submitted' });
}

// Delete a PA request
export function deletePARequest(id: string): boolean {
  const requests = getPARequests();
  const filtered = requests.filter(r => r.id !== id);
  
  if (filtered.length === requests.length) return false;
  
  savePARequests(filtered);
  return true;
}

// Get current PA from cookie
export function getCurrentPAId(): string | null {
  return getCookie(CURRENT_PA_KEY);
}

// Clear current PA cookie
export function clearCurrentPA() {
  deleteCookie(CURRENT_PA_KEY);
}

// Low confidence threshold: same as dashboard/review indicators
const LOW_CONFIDENCE_THRESHOLD = 70;

// Get stats
export function getPAStats() {
  const requests = getPARequests();
  
  return {
    ready: requests.filter(r => r.status === 'ready').length,
    processing: requests.filter(r => r.status === 'processing').length,
    submitted: requests.filter(r => r.status === 'submitted' || r.status === 'approved').length,
    attention: requests.filter(r => r.status === 'ready' && r.confidence < LOW_CONFIDENCE_THRESHOLD).length,
    total: requests.length,
  };
}
