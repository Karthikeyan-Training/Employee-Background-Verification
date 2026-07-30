import React, { useState, useRef } from 'react';

const ACCEPTED_TYPES = '.pdf,.jpg,.jpeg,.png';

export default function VerificationForm({ onSubmit, loading }) {
  const [fields, setFields] = useState({
    candidateName: '',
    email: '',
    position: '',
    criminalRecordCheck: false,
  });
  const [files, setFiles] = useState([]);
  const [validationErrors, setValidationErrors] = useState({});
  const fileInputRef = useRef(null);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFields((prev) => ({ ...prev, [name]: type === 'checkbox' ? checked : value }));
    if (validationErrors[name]) {
      setValidationErrors((prev) => ({ ...prev, [name]: null }));
    }
  };

  const handleFileChange = (e) => {
    const selected = Array.from(e.target.files);
    setFiles(selected);
    if (validationErrors.files) {
      setValidationErrors((prev) => ({ ...prev, files: null }));
    }
  };

  const removeFile = (index) => {
    setFiles((prev) => {
      const updated = prev.filter((_, i) => i !== index);
      // keep the input in sync
      if (updated.length === 0 && fileInputRef.current) {
        fileInputRef.current.value = '';
      }
      return updated;
    });
  };

  const validate = () => {
    const errors = {};
    if (!fields.candidateName.trim()) errors.candidateName = 'Candidate name is required.';
    if (fields.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(fields.email)) {
      errors.email = 'Enter a valid email address.';
    }
    if (files.length === 0) errors.files = 'Please upload at least one document.';
    return errors;
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    const errors = validate();
    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);
      return;
    }

    const formData = new FormData();
    formData.append('candidateName', fields.candidateName.trim());
    formData.append('email', fields.email.trim());
    formData.append('position', fields.position.trim());
    formData.append('criminalRecordCheck', fields.criminalRecordCheck);
    files.forEach((file) => formData.append('files', file));

    onSubmit(formData);
  };

  const formatBytes = (bytes) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  return (
    <div className="card">
      <div className="card-header bg-white py-3">
        <i className="bi bi-person-vcard me-2 text-primary"></i>
        Candidate Details
      </div>
      <div className="card-body p-4">
        <form onSubmit={handleSubmit} noValidate>
          <div className="row g-3">

            {/* Candidate Name */}
            <div className="col-md-6">
              <label htmlFor="candidateName" className="form-label">
                Candidate Name <span className="text-danger">*</span>
              </label>
              <input
                type="text"
                id="candidateName"
                name="candidateName"
                className={`form-control ${validationErrors.candidateName ? 'is-invalid' : ''}`}
                placeholder="e.g. John Doe"
                value={fields.candidateName}
                onChange={handleChange}
                disabled={loading}
              />
              {validationErrors.candidateName && (
                <div className="invalid-feedback">{validationErrors.candidateName}</div>
              )}
            </div>

            {/* Email */}
            <div className="col-md-6">
              <label htmlFor="email" className="form-label">Email</label>
              <input
                type="email"
                id="email"
                name="email"
                className={`form-control ${validationErrors.email ? 'is-invalid' : ''}`}
                placeholder="e.g. john@example.com"
                value={fields.email}
                onChange={handleChange}
                disabled={loading}
              />
              {validationErrors.email && (
                <div className="invalid-feedback">{validationErrors.email}</div>
              )}
            </div>

            {/* Position */}
            <div className="col-md-6">
              <label htmlFor="position" className="form-label">Position</label>
              <input
                type="text"
                id="position"
                name="position"
                className="form-control"
                placeholder="e.g. Software Engineer"
                value={fields.position}
                onChange={handleChange}
                disabled={loading}
              />
            </div>

            {/* Criminal Record Check */}
            <div className="col-md-6 d-flex align-items-end">
              <div className="form-check mb-2">
                <input
                  type="checkbox"
                  id="criminalRecordCheck"
                  name="criminalRecordCheck"
                  className="form-check-input"
                  checked={fields.criminalRecordCheck}
                  onChange={handleChange}
                  disabled={loading}
                />
                <label htmlFor="criminalRecordCheck" className="form-check-label">
                  Include Criminal Record Check
                </label>
              </div>
            </div>

            {/* File Upload */}
            <div className="col-12">
              <label htmlFor="files" className="form-label">
                Documents <span className="text-danger">*</span>
                <span className="text-muted ms-1" style={{ fontSize: '0.8rem' }}>
                  (PDF, JPG, PNG — max 10 MB each)
                </span>
              </label>
              <input
                type="file"
                id="files"
                name="files"
                ref={fileInputRef}
                className={`form-control ${validationErrors.files ? 'is-invalid' : ''}`}
                accept={ACCEPTED_TYPES}
                multiple
                onChange={handleFileChange}
                disabled={loading}
              />
              {validationErrors.files && (
                <div className="invalid-feedback">{validationErrors.files}</div>
              )}
            </div>

            {/* Selected files list */}
            {files.length > 0 && (
              <div className="col-12">
                <ul className="list-group list-group-flush">
                  {files.map((file, index) => (
                    <li
                      key={index}
                      className="list-group-item d-flex justify-content-between align-items-center px-0 py-1"
                    >
                      <span className="text-truncate me-2" style={{ fontSize: '0.875rem' }}>
                        <i className="bi bi-file-earmark me-1 text-secondary"></i>
                        {file.name}
                        <span className="text-muted ms-2">({formatBytes(file.size)})</span>
                      </span>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-danger"
                        onClick={() => removeFile(index)}
                        disabled={loading}
                        aria-label={`Remove ${file.name}`}
                      >
                        <i className="bi bi-x"></i>
                      </button>
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </div>

          {/* Submit */}
          <div className="mt-4 d-flex align-items-center gap-3">
            <button
              type="submit"
              className="btn btn-primary px-4"
              disabled={loading}
            >
              {loading ? (
                <>
                  <span
                    className="spinner-border spinner-border-sm me-2"
                    role="status"
                    aria-hidden="true"
                  ></span>
                  Running Verification…
                </>
              ) : (
                <>
                  <i className="bi bi-play-circle me-2"></i>
                  Run Verification
                </>
              )}
            </button>
            {loading && (
              <span className="text-muted" style={{ fontSize: '0.875rem' }}>
                Processing all pipeline stages — this may take a moment.
              </span>
            )}
          </div>
        </form>
      </div>
    </div>
  );
}
