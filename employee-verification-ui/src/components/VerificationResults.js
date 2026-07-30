import React, { useState } from 'react';

// ── Helpers ────────────────────────────────────────────────────────────────

function riskColor(level) {
  switch ((level || '').toLowerCase()) {
    case 'low':    return 'success';
    case 'medium': return 'warning';
    case 'high':   return 'danger';
    default:       return 'secondary';
  }
}

function statusColor(status) {
  switch ((status || '').toLowerCase()) {
    case 'verified':  return 'success';
    case 'failed':    return 'danger';
    case 'partial':   return 'warning';
    default:          return 'secondary';
  }
}

function severityColor(severity) {
  switch ((severity || '').toLowerCase()) {
    case 'critical': return 'danger';
    case 'high':     return 'warning';
    case 'medium':   return 'info';
    case 'low':      return 'secondary';
    default:         return 'secondary';
  }
}

function formatMs(ms) {
  if (!ms && ms !== 0) return '—';
  return ms >= 1000 ? `${(ms / 1000).toFixed(2)} s` : `${Math.round(ms)} ms`;
}

// ── Sub-components ─────────────────────────────────────────────────────────

function CandidateCard({ data }) {
  return (
    <div className="card mb-4">
      <div className="card-header bg-white py-3">
        <i className="bi bi-person-circle me-2 text-primary"></i>
        Candidate Details
      </div>
      <div className="card-body">
        <div className="row g-2">
          <div className="col-sm-4">
            <small className="text-muted d-block">Name</small>
            <strong>{data.candidateName || '—'}</strong>
          </div>
          <div className="col-sm-4">
            <small className="text-muted d-block">Email</small>
            <strong>{data.email || '—'}</strong>
          </div>
          <div className="col-sm-4">
            <small className="text-muted d-block">Position</small>
            <strong>{data.position || '—'}</strong>
          </div>
        </div>
        <div className="row g-2 mt-1">
          <div className="col-sm-4">
            <small className="text-muted d-block">Report ID</small>
            <code style={{ fontSize: '0.8rem' }}>{data.reportId || '—'}</code>
          </div>
          <div className="col-sm-4">
            <small className="text-muted d-block">Completed On</small>
            <span>{data.completedOn ? new Date(data.completedOn).toLocaleString() : '—'}</span>
          </div>
          <div className="col-sm-4">
            <small className="text-muted d-block">Total Duration</small>
            <span>{formatMs(data.totalDurationMs)}</span>
          </div>
        </div>
      </div>
    </div>
  );
}

function VerificationCard({ verification }) {
  const v = verification || {};
  const color = statusColor(v.status);

  return (
    <div className="card mb-4">
      <div className="card-header bg-white py-3">
        <i className="bi bi-clipboard-check me-2 text-primary"></i>
        Verification Status
      </div>
      <div className="card-body">
        <div className="mb-3">
          <span className={`badge bg-${color} fs-6 px-3 py-2`}>
            {v.status || 'Unknown'}
          </span>
        </div>

        {v.matchedFields?.length > 0 && (
          <div className="mb-3">
            <p className="section-title">Matched Fields</p>
            <div className="d-flex flex-wrap gap-2">
              {v.matchedFields.map((f, i) => (
                <span key={i} className="badge bg-success-subtle text-success border border-success-subtle">
                  <i className="bi bi-check-lg me-1"></i>{f}
                </span>
              ))}
            </div>
          </div>
        )}

        {v.mismatchedFields?.length > 0 && (
          <div className="mb-3">
            <p className="section-title">Mismatched Fields</p>
            <table className="table table-sm table-bordered mb-0">
              <thead className="table-light">
                <tr>
                  <th>Field</th>
                  <th>Values by Source</th>
                </tr>
              </thead>
              <tbody>
                {v.mismatchedFields.map((m, i) => (
                  <tr key={i}>
                    <td><strong>{m.fieldName}</strong></td>
                    <td>
                      {Object.entries(m.valuesBySource || {}).map(([src, val]) => (
                        <div key={src} style={{ fontSize: '0.85rem' }}>
                          <span className="text-muted">{src}:</span> {val}
                        </div>
                      ))}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {v.missingFields?.length > 0 && (
          <div className="mb-3">
            <p className="section-title">Missing Fields</p>
            <div className="d-flex flex-wrap gap-2">
              {v.missingFields.map((f, i) => (
                <span key={i} className="badge bg-warning-subtle text-warning border border-warning-subtle">
                  <i className="bi bi-question-circle me-1"></i>{f}
                </span>
              ))}
            </div>
          </div>
        )}

        {v.warnings?.length > 0 && (
          <div className="alert alert-warning py-2 mb-0">
            <p className="fw-semibold mb-1"><i className="bi bi-exclamation-triangle me-1"></i>Warnings</p>
            <ul className="mb-0 ps-3" style={{ fontSize: '0.875rem' }}>
              {v.warnings.map((w, i) => <li key={i}>{w}</li>)}
            </ul>
          </div>
        )}
      </div>
    </div>
  );
}

function RiskCard({ risk }) {
  const r = risk || {};
  const color = riskColor(r.level);

  return (
    <div className="card mb-4">
      <div className="card-header bg-white py-3">
        <i className="bi bi-graph-up-arrow me-2 text-primary"></i>
        Risk Score
      </div>
      <div className="card-body">
        <div className="d-flex align-items-center gap-3 mb-3">
          <span className={`badge bg-${color} risk-badge`}>
            {r.level || 'Unknown'}
          </span>
          <div>
            <div className="fw-bold" style={{ fontSize: '1.5rem' }}>{r.score ?? '—'}<small className="text-muted fs-6"> / 100</small></div>
          </div>
        </div>

        {/* Progress bar */}
        <div className="progress mb-3" style={{ height: '10px' }}>
          <div
            className={`progress-bar bg-${color}`}
            role="progressbar"
            style={{ width: `${r.score ?? 0}%` }}
            aria-valuenow={r.score ?? 0}
            aria-valuemin="0"
            aria-valuemax="100"
          ></div>
        </div>

        {r.recommendation && (
          <p className="text-muted mb-0" style={{ fontSize: '0.9rem' }}>
            <i className="bi bi-lightbulb me-1"></i>
            {r.recommendation}
          </p>
        )}
      </div>
    </div>
  );
}

function FraudCard({ fraud }) {
  const f = fraud || {};

  return (
    <div className="card mb-4">
      <div className="card-header bg-white py-3">
        <i className="bi bi-shield-exclamation me-2 text-primary"></i>
        Fraud Indicators
      </div>
      <div className="card-body">
        <div className="d-flex align-items-center gap-3 mb-3">
          <span className={`badge ${f.isFraudulent ? 'bg-danger' : 'bg-success'} fs-6 px-3 py-2`}>
            <i className={`bi ${f.isFraudulent ? 'bi-x-circle' : 'bi-check-circle'} me-1`}></i>
            {f.isFraudulent ? 'Fraud Detected' : 'No Fraud Detected'}
          </span>
          {f.maxSeverity && (
            <span className={`badge bg-${severityColor(f.maxSeverity)}-subtle text-${severityColor(f.maxSeverity)} border border-${severityColor(f.maxSeverity)}-subtle`}>
              Max Severity: {f.maxSeverity}
            </span>
          )}
        </div>

        {f.indicators?.length > 0 && (
          <div className="mb-3">
            <p className="section-title">Indicators</p>
            <div className="list-group list-group-flush">
              {f.indicators.map((ind, i) => (
                <div key={i} className="list-group-item px-0 py-2">
                  <div className="d-flex justify-content-between align-items-start">
                    <div>
                      <strong style={{ fontSize: '0.9rem' }}>{ind.indicatorName}</strong>
                      <p className="mb-0 text-muted" style={{ fontSize: '0.85rem' }}>{ind.description}</p>
                    </div>
                    <span className={`badge bg-${severityColor(ind.severity)} ms-2`}>
                      {ind.severity}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {f.manualReviewRecommendations?.length > 0 && (
          <div className="alert alert-info py-2 mb-0">
            <p className="fw-semibold mb-1"><i className="bi bi-info-circle me-1"></i>Manual Review Recommendations</p>
            <ul className="mb-0 ps-3" style={{ fontSize: '0.875rem' }}>
              {f.manualReviewRecommendations.map((r, i) => <li key={i}>{r}</li>)}
            </ul>
          </div>
        )}
      </div>
    </div>
  );
}

function ReportCard({ reportContent }) {
  const [expanded, setExpanded] = useState(false);

  if (!reportContent) return null;

  return (
    <div className="card mb-4">
      <div className="card-header bg-white py-3 d-flex justify-content-between align-items-center">
        <span>
          <i className="bi bi-file-text me-2 text-primary"></i>
          AI Generated Report
        </span>
        <button
          className="btn btn-sm btn-outline-secondary"
          onClick={() => setExpanded((p) => !p)}
        >
          {expanded ? (
            <><i className="bi bi-chevron-up me-1"></i>Collapse</>
          ) : (
            <><i className="bi bi-chevron-down me-1"></i>Expand</>
          )}
        </button>
      </div>
      {expanded && (
        <div className="card-body">
          <div className="report-box">{reportContent}</div>
        </div>
      )}
      {!expanded && (
        <div className="card-body py-2">
          <p className="text-muted mb-0" style={{ fontSize: '0.875rem' }}>
            Click <strong>Expand</strong> to view the full AI-generated report.
          </p>
        </div>
      )}
    </div>
  );
}

function PipelineStepsCard({ steps }) {
  if (!steps?.length) return null;

  return (
    <div className="card mb-4">
      <div className="card-header bg-white py-3">
        <i className="bi bi-diagram-3 me-2 text-primary"></i>
        Pipeline Steps
      </div>
      <div className="card-body p-0">
        <table className="table table-sm table-hover mb-0">
          <thead className="table-light">
            <tr>
              <th className="ps-3">Step</th>
              <th>Status</th>
              <th>Duration</th>
              <th className="pe-3">Detail</th>
            </tr>
          </thead>
          <tbody>
            {steps.map((s, i) => (
              <tr key={i} className="pipeline-step">
                <td className="ps-3">{s.step}</td>
                <td>
                  <span
                    className={`badge bg-${
                      s.status === 'Completed' ? 'success' :
                      s.status === 'Failed' ? 'danger' : 'secondary'
                    }`}
                  >
                    {s.status}
                  </span>
                </td>
                <td>{formatMs(s.durationMs)}</td>
                <td className="pe-3 text-muted">{s.detail || '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ── Main export ────────────────────────────────────────────────────────────

export default function VerificationResults({ data }) {
  if (!data) return null;

  return (
    <div>
      <h5 className="fw-bold mb-3">
        <i className="bi bi-check2-all me-2 text-success"></i>
        Verification Results
      </h5>

      <CandidateCard data={data} />

      <div className="row g-4">
        <div className="col-md-6">
          <VerificationCard verification={data.verification} />
        </div>
        <div className="col-md-6">
          <RiskCard risk={data.risk} />
        </div>
      </div>

      <FraudCard fraud={data.fraud} />
      <ReportCard reportContent={data.reportContent} />
      <PipelineStepsCard steps={data.pipelineSteps} />
    </div>
  );
}
