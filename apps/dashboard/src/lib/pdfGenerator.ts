// PDF Generator for Prior Authorization Forms
import { PARequest } from './mockData';

// Generate PDF content as HTML for printing
export function generatePAPdf(request: PARequest): string {
  const today = new Date().toLocaleDateString('en-US', {
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  });

  return `
<!DOCTYPE html>
<html>
<head>
  <title>Prior Authorization Request - ${request.id}</title>
  <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    body { 
      font-family: 'Segoe UI', Arial, sans-serif; 
      font-size: 11pt;
      line-height: 1.4;
      color: #1a1a1a;
      padding: 0.5in;
    }
    .header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      border-bottom: 3px solid #0d9488;
      padding-bottom: 15px;
      margin-bottom: 20px;
    }
    .logo {
      display: flex;
      align-items: center;
      gap: 10px;
    }
    .logo-icon {
      width: 40px;
      height: 40px;
      background: #0d9488;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      font-weight: bold;
      font-size: 16pt;
    }
    .logo-text {
      font-size: 18pt;
      font-weight: bold;
      color: #0d9488;
    }
    .logo-subtitle {
      font-size: 9pt;
      color: #666;
    }
    .doc-info {
      text-align: right;
      font-size: 10pt;
      color: #666;
    }
    .doc-info strong {
      color: #1a1a1a;
      font-size: 12pt;
    }
    h1 {
      font-size: 16pt;
      color: #0d9488;
      margin-bottom: 20px;
      text-align: center;
      text-transform: uppercase;
      letter-spacing: 1px;
    }
    .section {
      margin-bottom: 20px;
    }
    .section-title {
      font-size: 11pt;
      font-weight: bold;
      color: #0d9488;
      border-bottom: 1px solid #e5e5e5;
      padding-bottom: 5px;
      margin-bottom: 10px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }
    .grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 10px 30px;
    }
    .field {
      margin-bottom: 8px;
    }
    .field-label {
      font-size: 9pt;
      color: #666;
      text-transform: uppercase;
      letter-spacing: 0.3px;
    }
    .field-value {
      font-weight: 500;
      color: #1a1a1a;
    }
    .clinical-summary {
      background: #f8f9fa;
      padding: 15px;
      border-radius: 6px;
      border-left: 4px solid #0d9488;
    }
    .criteria-list {
      list-style: none;
    }
    .criteria-item {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 8px 0;
      border-bottom: 1px solid #f0f0f0;
    }
    .criteria-item:last-child {
      border-bottom: none;
    }
    .criteria-status {
      width: 20px;
      height: 20px;
      border-radius: 4px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 12pt;
      flex-shrink: 0;
    }
    .criteria-met {
      background: #dcfce7;
      color: #16a34a;
    }
    .criteria-not-met {
      background: #fee2e2;
      color: #dc2626;
    }
    .criteria-pending {
      background: #fef3c7;
      color: #d97706;
    }
    .signature-section {
      margin-top: 40px;
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 60px;
    }
    .signature-box {
      border-top: 1px solid #1a1a1a;
      padding-top: 5px;
    }
    .signature-label {
      font-size: 9pt;
      color: #666;
    }
    .footer {
      margin-top: 40px;
      padding-top: 15px;
      border-top: 1px solid #e5e5e5;
      font-size: 8pt;
      color: #999;
      text-align: center;
    }
    .confidence-badge {
      display: inline-block;
      padding: 4px 12px;
      border-radius: 20px;
      font-weight: bold;
      font-size: 10pt;
    }
    .confidence-high {
      background: #dcfce7;
      color: #16a34a;
    }
    .confidence-medium {
      background: #fef3c7;
      color: #d97706;
    }
    .confidence-low {
      background: #fee2e2;
      color: #dc2626;
    }
    @media print {
      body { padding: 0; }
      .no-print { display: none; }
    }
  </style>
</head>
<body>
  <div class="header">
    <div class="logo">
      <div class="logo-icon">AS</div>
      <div>
        <div class="logo-text">AuthScript</div>
        <div class="logo-subtitle">Prior Authorization Request</div>
      </div>
    </div>
    <div class="doc-info">
      <strong>${request.id}</strong><br>
      Generated: ${today}<br>
      Status: ${request.status.toUpperCase()}
    </div>
  </div>

  <h1>Prior Authorization Request Form</h1>

  <div class="section">
    <div class="section-title">Patient Information</div>
    <div class="grid">
      <div class="field">
        <div class="field-label">Patient Name</div>
        <div class="field-value">${request.patient.name}</div>
      </div>
      <div class="field">
        <div class="field-label">Date of Birth</div>
        <div class="field-value">${request.patient.dob}</div>
      </div>
      <div class="field">
        <div class="field-label">Medical Record Number (MRN)</div>
        <div class="field-value">${request.patient.mrn}</div>
      </div>
      <div class="field">
        <div class="field-label">Member ID</div>
        <div class="field-value">${request.patient.memberId}</div>
      </div>
      <div class="field">
        <div class="field-label">Address</div>
        <div class="field-value">${request.patient.address}</div>
      </div>
      <div class="field">
        <div class="field-label">Phone</div>
        <div class="field-value">${request.patient.phone}</div>
      </div>
    </div>
  </div>

  <div class="section">
    <div class="section-title">Insurance Information</div>
    <div class="grid">
      <div class="field">
        <div class="field-label">Insurance Payer</div>
        <div class="field-value">${request.payer}</div>
      </div>
      <div class="field">
        <div class="field-label">Member ID</div>
        <div class="field-value">${request.patient.memberId}</div>
      </div>
    </div>
  </div>

  <div class="section">
    <div class="section-title">Requested Service</div>
    <div class="grid">
      <div class="field">
        <div class="field-label">Procedure/Service Code</div>
        <div class="field-value">${request.procedureCode}</div>
      </div>
      <div class="field">
        <div class="field-label">Procedure Description</div>
        <div class="field-value">${request.procedureName}</div>
      </div>
      <div class="field">
        <div class="field-label">Diagnosis Code (ICD-10)</div>
        <div class="field-value">${request.diagnosisCode}</div>
      </div>
      <div class="field">
        <div class="field-label">Diagnosis Description</div>
        <div class="field-value">${request.diagnosis}</div>
      </div>
      <div class="field">
        <div class="field-label">Requested Service Date</div>
        <div class="field-value">${request.serviceDate}</div>
      </div>
      <div class="field">
        <div class="field-label">Place of Service</div>
        <div class="field-value">${request.placeOfService}</div>
      </div>
    </div>
  </div>

  <div class="section">
    <div class="section-title">Ordering Provider</div>
    <div class="grid">
      <div class="field">
        <div class="field-label">Provider Name</div>
        <div class="field-value">${request.provider}</div>
      </div>
      <div class="field">
        <div class="field-label">NPI Number</div>
        <div class="field-value">${request.providerNpi}</div>
      </div>
    </div>
  </div>

  <div class="section">
    <div class="section-title">Clinical Summary / Medical Necessity</div>
    <div class="clinical-summary">
      ${request.clinicalSummary}
    </div>
  </div>

  <div class="section">
    <div class="section-title">
      Policy Criteria Assessment
      <span class="confidence-badge ${(request.confidence || 1) >= 80 ? 'confidence-high' : (request.confidence || 1) >= 60 ? 'confidence-medium' : 'confidence-low'}" style="float: right;">
        AI Confidence: ${Math.max(1, request.confidence || 0)}%
      </span>
    </div>
    <ul class="criteria-list">
      ${request.criteria.map(c => `
        <li class="criteria-item">
          <span class="criteria-status ${c.met === true ? 'criteria-met' : c.met === false ? 'criteria-not-met' : 'criteria-pending'}">
            ${c.met === true ? '✓' : c.met === false ? '✗' : '?'}
          </span>
          <span>${c.label}</span>
        </li>
      `).join('')}
    </ul>
  </div>

  <div class="signature-section">
    <div>
      <div style="height: 50px;"></div>
      <div class="signature-box">
        <div class="signature-label">Provider Signature</div>
      </div>
    </div>
    <div>
      <div style="height: 50px;"></div>
      <div class="signature-box">
        <div class="signature-label">Date</div>
      </div>
    </div>
  </div>

  <div class="footer">
    <p>This prior authorization request was generated by AuthScript - AI-Powered Prior Authorization for athenahealth</p>
    <p>Document ID: ${request.id} | Generated: ${new Date().toISOString()}</p>
  </div>
</body>
</html>
  `;
}

// Open PDF in new window for printing
export function openPAPdf(request: PARequest) {
  const html = generatePAPdf(request);
  const printWindow = window.open('', '_blank');
  
  if (printWindow) {
    printWindow.document.write(html);
    printWindow.document.close();
    
    // Wait for content to load then trigger print
    printWindow.onload = () => {
      setTimeout(() => {
        printWindow.print();
      }, 250);
    };
  }
}

// Download as PDF (using print to PDF)
export function downloadPAPdf(request: PARequest) {
  const html = generatePAPdf(request);
  const printWindow = window.open('', '_blank');
  
  if (printWindow) {
    printWindow.document.write(html);
    printWindow.document.close();
    printWindow.onload = () => {
      printWindow.print();
    };
  }
}
