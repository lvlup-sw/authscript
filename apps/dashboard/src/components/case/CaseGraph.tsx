import { ReactFlow, ReactFlowProvider, Background, Controls } from '@xyflow/react';
import type { Node, Edge } from '@xyflow/react';
import { PatientNode } from './PatientNode';
import { EvidenceNode } from './EvidenceNode';
import { CriteriaNode } from './CriteriaNode';
import { DecisionNode } from './DecisionNode';
import { AnimatedEdge } from './AnimatedEdge';
import { DEMO_PA_RESULT_SOURCES } from '@/lib/demoData';
import type { PARequest } from '@/api/graphqlService';

// Register custom node and edge types
const nodeTypes = {
  patient: PatientNode,
  evidence: EvidenceNode,
  criteria: CriteriaNode,
  decision: DecisionNode,
};

const edgeTypes = {
  animated: AnimatedEdge,
};

/**
 * Maps a criterion's met value (boolean | null) to a node status string.
 */
function toStatus(met: boolean | null): 'met' | 'not_met' | 'indeterminate' {
  if (met === true) return 'met';
  if (met === false) return 'not_met';
  return 'indeterminate';
}

/**
 * Builds graph nodes and edges from a PARequest.
 * Exported for testability.
 */
export function buildCaseGraphData(paRequest: PARequest): {
  nodes: Node[];
  edges: Edge[];
} {
  const nodes: Node[] = [];
  const edges: Edge[] = [];

  // 1. Patient node (top center)
  const patientNodeId = 'patient-1';
  nodes.push({
    id: patientNodeId,
    type: 'patient',
    position: { x: 400, y: 0 },
    data: {
      name: paRequest.patient.name,
      dob: paRequest.patient.dob,
      mrn: paRequest.patient.mrn,
      insurance: paRequest.payer,
    },
  });

  // 2. Evidence nodes (left column, stacked)
  const evidenceEntries = paRequest.criteria.map((c) => {
    const sourceInfo = DEMO_PA_RESULT_SOURCES[c.label];
    return {
      text: sourceInfo?.evidence ?? c.reason ?? c.label,
      source: sourceInfo?.source ?? 'Clinical',
      criterionLabel: c.label,
    };
  });

  const evidenceYStart = 120;
  const evidenceSpacing = 100;

  evidenceEntries.forEach((entry, i) => {
    const nodeId = `evidence-${i}`;
    nodes.push({
      id: nodeId,
      type: 'evidence',
      position: { x: 0, y: evidenceYStart + i * evidenceSpacing },
      data: {
        text: entry.text,
        source: entry.source,
      },
    });

    // Edge: patient -> evidence (subtle)
    edges.push({
      id: `edge-patient-evidence-${i}`,
      source: patientNodeId,
      target: nodeId,
      style: { stroke: '#cbd5e1', strokeWidth: 1 },
      animated: false,
    });
  });

  // 3. Criteria nodes (center column, stacked)
  const criteriaYStart = 120;
  const criteriaSpacing = 100;

  paRequest.criteria.forEach((c, i) => {
    const nodeId = `criteria-${i}`;
    nodes.push({
      id: nodeId,
      type: 'criteria',
      position: { x: 350, y: criteriaYStart + i * criteriaSpacing },
      data: {
        label: c.label,
        status: toStatus(c.met),
        reasoning: c.reason,
      },
    });

    // Edge: evidence -> criteria (animated dashed)
    edges.push({
      id: `edge-evidence-criteria-${i}`,
      source: `evidence-${i}`,
      target: nodeId,
      type: 'animated',
      data: { animationDelay: i * 0.3 },
    });
  });

  // 4. Decision node (right)
  const decisionNodeId = 'decision-1';
  const decisionY =
    criteriaYStart +
    ((paRequest.criteria.length - 1) * criteriaSpacing) / 2;

  nodes.push({
    id: decisionNodeId,
    type: 'decision',
    position: { x: 700, y: decisionY },
    data: {
      payer: paRequest.payer,
      policyId: 'LCD L34220',
      confidence: paRequest.confidence,
      status: paRequest.status,
    },
  });

  // Edges: criteria -> decision (solid colored)
  paRequest.criteria.forEach((_c, i) => {
    const statusColor =
      _c.met === true
        ? '#22c55e'
        : _c.met === false
          ? '#ef4444'
          : '#f59e0b';
    edges.push({
      id: `edge-criteria-decision-${i}`,
      source: `criteria-${i}`,
      target: decisionNodeId,
      style: { stroke: statusColor, strokeWidth: 2 },
    });
  });

  return { nodes, edges };
}

interface CaseGraphProps {
  paRequest: PARequest;
}

/**
 * Full case graph visualization for a PA request.
 * Shows patient -> evidence -> criteria -> decision flow.
 */
export function CaseGraph({ paRequest }: CaseGraphProps) {
  const { nodes, edges } = buildCaseGraphData(paRequest);

  return (
    <ReactFlowProvider>
      <ReactFlow
        nodes={nodes}
        edges={edges}
        nodeTypes={nodeTypes}
        edgeTypes={edgeTypes}
        fitView
        nodesDraggable={false}
        nodesConnectable={false}
        elementsSelectable={false}
        panOnDrag={false}
        zoomOnScroll={false}
        zoomOnDoubleClick={false}
        proOptions={{ hideAttribution: true }}
        className="bg-slate-50/50 rounded-xl"
      >
        <Background gap={24} size={1} color="#e2e8f0" />
        <Controls showInteractive={false} />
      </ReactFlow>
    </ReactFlowProvider>
  );
}
