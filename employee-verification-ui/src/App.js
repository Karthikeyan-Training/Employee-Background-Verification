import React, { useState } from 'react';
import VerificationForm from './components/VerificationForm';
import VerificationResults from './components/VerificationResults';

const API_BASE = process.env.REACT_APP_API_BASE_URL || '';

export default function App() {
  const [result, setResult] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(false);

  const handleSubmit = async (formData) => {
    setLoading(true);
    setError(null);
    setResult(null);
    setSuccess(false);

    try {
      const response = await fetch(`${API_BASE}/api/demo/run`, {
        method: 'POST',
        body: formData,
      });

      if (!response.ok) {
        let errMsg = `Server error: ${response.status} ${response.statusText}`;
        try {
          const errBody = await response.json();
          if (errBody?.error) errMsg = errBody.error;
        } catch (_) {}
        throw new Error(errMsg);
      }

      const data = await response.json();
      setResult(data);
      setSuccess(true);
    } catch (err) {
      setError(err.message || 'An unexpected error occurred. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      {/* Navbar */}
      <nav className="navbar navbar-dark bg-primary px-3 py-2 mb-4">
        <span className="navbar-brand mb-0 h5">
          <i className="bi bi-shield-check me-2"></i>
          Employee Background Verification
        </span>
      </nav>

      <div className="container pb-5">
        <div className="row justify-content-center">
          <div className="col-lg-10 col-xl-9">

            {/* Page heading */}
            <div className="mb-4">
              <h4 className="fw-bold mb-1">Background Verification</h4>
              <p className="text-muted mb-0" style={{ fontSize: '0.9rem' }}>
                Submit candidate details and documents to run the full verification pipeline.
              </p>
            </div>

            {/* Global success alert */}
            {success && !loading && (
              <div className="alert alert-success d-flex align-items-center gap-2" role="alert">
                <i className="bi bi-check-circle-fill"></i>
                <span>Verification completed successfully.</span>
              </div>
            )}

            {/* Global error alert */}
            {error && (
              <div className="alert alert-danger d-flex align-items-center gap-2" role="alert">
                <i className="bi bi-exclamation-triangle-fill"></i>
                <span>{error}</span>
              </div>
            )}

            {/* Submission form */}
            <VerificationForm onSubmit={handleSubmit} loading={loading} />

            {/* Results panel (shown after a successful call) */}
            {result && !loading && (
              <div className="mt-4">
                <VerificationResults data={result} />
              </div>
            )}
          </div>
        </div>
      </div>
    </>
  );
}
