import React, { useState, useRef } from 'react';

const ACCEPTED_TYPES = '.pdf,.jpg,.jpeg,.png';
const REQUIRED_DOCUMENTS = ['Aadhaar', 'PAN', 'Resume'];

function inferDocumentType(fileName) {
  const baseName = fileName.replace(/\.[^.]+$/, '').toLowerCase();

  if (baseName.includes('aadhaar')) {
    return 'Aadhaar';
  }

  if (/(^|[^a-z])pan([^a-z]|$)/.test(baseName)) {
    return 'PAN';
  }

  if (baseName.includes('resume') || /(^|[^a-z])cv([^a-z]|$)/.test(baseName)) {
    return 'Resume';
  }

  return null;
}

function createFileEntry(file) {
  return {
    id: `${file.name}-${file.size}-${file.lastModified}`,
    file,
    documentType: inferDocumentType(file.name),
  };
}

function buildUploadFileName(entry, occurrenceByType) {
  const extensionMatch = entry.file.name.match(/\.[^.]+$/);
  const extension = extensionMatch ? extensionMatch[0] : '';

  if (!entry.documentType) {
    return entry.file.name;
  }

  const nextCount = (occurrenceByType[entry.documentType] || 0) + 1;
  occurrenceByType[entry.documentType] = nextCount;

  return nextCount === 1
    ? `${entry.documentType}${extension}`
    : `${entry.documentType}-${nextCount}${extension}`;
}

export default function VerificationForm({ onSubmit, loading }) {
  const [fields, setFields] = useState({
    candidateName: '',
    email: '',
    position: '',
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
    const selected = Array.from(e.target.files).map(createFileEntry);

    setFiles((prev) => {
      const existingIds = new Set(prev.map((entry) => entry.id));
      const nextEntries = selected.filter((entry) => !existingIds.has(entry.id));
      return [...prev, ...nextEntries];
    });

    if (validationErrors.files) {
      setValidationErrors((prev) => ({ ...prev, files: null }));
    }

    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const removeFile = (index) => {
    setFiles((prev) => {
      const updated = prev.filter((_, i) => i !== index);
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

    if (files.length === 0) {
      errors.files = 'Please upload at least one document.';
      return errors;
    }

    const providedDocumentTypes = new Set(
      files
        .map((entry) => entry.documentType)
        .filter(Boolean)
    );

    const missingDocuments = REQUIRED_DOCUMENTS.filter(
      (documentName) => !providedDocumentTypes.has(documentName)
    );

    if (missingDocuments.length > 0) {
      errors.files = `Missing mandatory documents: ${missingDocuments.join(', ')}.`;
    }

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

    const occurrenceByType = {};
    files.forEach((entry) => {
      const uploadFileName = buildUploadFileName(entry, occurrenceByType);
      formData.append('files', entry.file, uploadFileName);
    });

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
              <div className="form-text">
                Required document types: Aadhaar, PAN, and Resume. These are detected from the selected filenames.
              </div>
            </div>

            {/* Selected files list */}
            {files.length > 0 && (
              <div className="col-12">
                <ul className="list-group list-group-flush">
                  {files.map((entry, index) => (
                    <li
                      key={entry.id}
                      className="list-group-item d-flex justify-content-between align-items-center px-0 py-1"
                    >
                      <div className="text-truncate me-2" style={{ fontSize: '0.875rem' }}>
                        <span>
                          <i className="bi bi-file-earmark me-1 text-secondary"></i>
                          {entry.file.name}
                          <span className="text-muted ms-2">({formatBytes(entry.file.size)})</span>
                        </span>
                        <span className={`badge ms-2 ${entry.documentType ? 'bg-light text-dark border' : 'bg-warning-subtle text-warning border border-warning-subtle'}`}>
                          {entry.documentType || 'Unrecognized'}
                        </span>
                      </div>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-danger"
                        onClick={() => removeFile(index)}
                        disabled={loading}
                        aria-label={`Remove ${entry.file.name}`}
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
