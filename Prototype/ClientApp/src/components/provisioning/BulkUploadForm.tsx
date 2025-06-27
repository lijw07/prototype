// Bulk Upload Form Component
// Follows SRP: Only responsible for file upload interface and validation

import React, { useState, useCallback, useRef } from 'react';
import { 
  Upload,
  FileText,
  X,
  Check,
  AlertTriangle,
  Download,
  Settings,
  Play,
  Pause,
  RotateCcw,
  FileSpreadsheet,
  Database,
  Zap
} from 'lucide-react';
import { useBulkUpload, type UploadOptions } from './hooks/useBulkUpload';
import { useSignalRProgress } from './hooks/useSignalRProgress';
import type { BulkUploadProps } from './types/provisioning.types';

const BulkUploadForm: React.FC<BulkUploadProps> = ({
  className = '',
  allowedFileTypes = ['.csv', '.xlsx', '.xls', '.json', '.xml'],
  maxFileSize = 50, // MB
  maxFiles = 10,
  onError,
  onSuccess,
  onUploadComplete,
  onProgressUpdate
}) => {
  const {
    files,
    isUploading,
    progress,
    status,
    currentJobId,
    results,
    errors,
    loading,
    error,
    addFiles,
    removeFile,
    clearFiles,
    startUpload,
    cancelUpload,
    setProgressCallbacks,
    clearError,
    resetUpload
  } = useBulkUpload();

  const {
    isConnected,
    connectionError,
    subscribeToJob,
    refreshConnection
  } = useSignalRProgress();

  const fileInputRef = useRef<HTMLInputElement>(null);
  const [dragActive, setDragActive] = useState(false);
  const [uploadOptions, setUploadOptions] = useState<UploadOptions>({
    strategy: 'progress',
    detectTableTypes: true,
    validateOnly: false,
    autoProvision: false
  });
  const [showAdvancedOptions, setShowAdvancedOptions] = useState(false);

  // Setup progress callbacks
  React.useEffect(() => {
    setProgressCallbacks({
      onProgress: (progressData) => {
        if (onProgressUpdate) {
          onProgressUpdate(progressData.progressPercentage);
        }
      },
      onComplete: (result) => {
        if (onUploadComplete) {
          onUploadComplete(result);
        }
        if (onSuccess) {
          onSuccess('Upload completed successfully');
        }
      },
      onError: (jobError) => {
        if (onError) {
          onError(jobError.error);
        }
      }
    });
  }, [setProgressCallbacks, onProgressUpdate, onUploadComplete, onSuccess, onError]);

  // Handle drag and drop events
  const handleDrag = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);

    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      const droppedFiles = Array.from(e.dataTransfer.files);
      if (files.length + droppedFiles.length > maxFiles) {
        if (onError) {
          onError(`Maximum ${maxFiles} files allowed`);
        }
        return;
      }
      addFiles(droppedFiles);
    }
  }, [addFiles, files.length, maxFiles, onError]);

  // Handle file selection
  const handleFileSelect = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      const selectedFiles = Array.from(e.target.files);
      if (files.length + selectedFiles.length > maxFiles) {
        if (onError) {
          onError(`Maximum ${maxFiles} files allowed`);
        }
        return;
      }
      addFiles(selectedFiles);
    }
  }, [addFiles, files.length, maxFiles, onError]);

  // Start upload with current options
  const handleStartUpload = useCallback(async () => {
    clearError();
    
    if (!isConnected && uploadOptions.strategy !== 'core') {
      if (onError) {
        onError('SignalR connection required for progress tracking. Please refresh the connection or use core strategy.');
      }
      return;
    }

    const result = await startUpload(uploadOptions);
    
    if (result.success && result.jobId && uploadOptions.strategy !== 'core') {
      // Subscribe to job progress for real-time updates
      await subscribeToJob({
        jobId: result.jobId,
        onProgress: (progressData) => {
          if (onProgressUpdate) {
            onProgressUpdate(progressData.progressPercentage);
          }
        },
        onComplete: (completionResult) => {
          if (onUploadComplete) {
            onUploadComplete(completionResult);
          }
          if (onSuccess) {
            onSuccess('Upload completed successfully');
          }
        },
        onError: (jobError) => {
          if (onError) {
            onError(jobError.error);
          }
        }
      });
    }
  }, [startUpload, uploadOptions, isConnected, subscribeToJob, onProgressUpdate, onUploadComplete, onSuccess, onError, clearError]);

  // Get file type icon
  const getFileIcon = (fileType: string) => {
    switch (fileType) {
      case '.csv':
        return <FileText className="text-success" size={20} />;
      case '.xlsx':
      case '.xls':
        return <FileSpreadsheet className="text-success" size={20} />;
      case '.json':
        return <Database className="text-info" size={20} />;
      case '.xml':
        return <FileText className="text-warning" size={20} />;
      default:
        return <FileText className="text-muted" size={20} />;
    }
  };

  // Get strategy display name
  const getStrategyDisplayName = (strategy: string) => {
    switch (strategy) {
      case 'core':
        return 'Core Upload';
      case 'multiple':
        return 'Multiple Files';
      case 'progress':
        return 'Progress Tracking';
      case 'queue':
        return 'Queue Processing';
      default:
        return strategy;
    }
  };

  return (
    <div className={`card border-0 rounded-4 shadow-sm ${className}`}>
      {/* Header */}
      <div className="card-header border-0 bg-transparent pt-4 px-4 pb-0">
        <div className="d-flex align-items-center justify-content-between mb-3">
          <div className="d-flex align-items-center">
            <div className="rounded-circle bg-primary bg-opacity-10 p-3 me-3">
              <Upload className="text-primary" size={24} />
            </div>
            <div>
              <h5 className="card-title mb-0 fw-bold text-dark">Bulk Upload</h5>
              <p className="text-muted small mb-0">
                Upload and process multiple user provisioning files
              </p>
            </div>
          </div>
          
          {/* Connection Status */}
          <div className="d-flex align-items-center gap-3">
            {uploadOptions.strategy !== 'core' && (
              <div className="d-flex align-items-center">
                <div className={`rounded-circle p-1 me-2 ${isConnected ? 'bg-success' : 'bg-danger'}`}>
                  <div className="rounded-circle bg-white" style={{width: '6px', height: '6px'}}></div>
                </div>
                <span className={`small ${isConnected ? 'text-success' : 'text-danger'}`}>
                  {isConnected ? 'Connected' : 'Disconnected'}
                </span>
                {!isConnected && (
                  <button
                    className="btn btn-link btn-sm p-0 ms-2"
                    onClick={refreshConnection}
                    title="Refresh Connection"
                  >
                    <RotateCcw size={14} />
                  </button>
                )}
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="card-body p-4">
        {/* Upload Options */}
        <div className="row mb-4">
          <div className="col-md-6">
            <label className="form-label small fw-semibold">Upload Strategy</label>
            <select
              className="form-select rounded-3"
              value={uploadOptions.strategy}
              onChange={(e) => setUploadOptions((prev: UploadOptions) => ({
                ...prev,
                strategy: e.target.value as any
              }))}
              disabled={isUploading}
            >
              <option value="core">Core Upload (Basic)</option>
              <option value="progress">Progress Tracking (Recommended)</option>
              <option value="multiple">Multiple Files</option>
              <option value="queue">Queue Processing</option>
            </select>
            <small className="text-muted">
              {getStrategyDisplayName(uploadOptions.strategy)} - 
              {uploadOptions.strategy === 'core' && ' Simple upload without real-time progress'}
              {uploadOptions.strategy === 'progress' && ' Real-time progress with SignalR'}
              {uploadOptions.strategy === 'multiple' && ' Optimized for multiple files'}
              {uploadOptions.strategy === 'queue' && ' Background processing with queue'}
            </small>
          </div>
          
          <div className="col-md-6">
            <div className="d-flex align-items-center justify-content-between">
              <label className="form-label small fw-semibold mb-0">Advanced Options</label>
              <button
                className="btn btn-outline-secondary btn-sm rounded-3"
                onClick={() => setShowAdvancedOptions(!showAdvancedOptions)}
              >
                <Settings size={14} className="me-1" />
                {showAdvancedOptions ? 'Hide' : 'Show'}
              </button>
            </div>
            
            {showAdvancedOptions && (
              <div className="mt-3 p-3 bg-light rounded-3">
                <div className="row g-3">
                  <div className="col-12">
                    <div className="form-check">
                      <input
                        className="form-check-input"
                        type="checkbox"
                        id="detectTableTypes"
                        checked={uploadOptions.detectTableTypes}
                        onChange={(e) => setUploadOptions((prev: UploadOptions) => ({
                          ...prev,
                          detectTableTypes: e.target.checked
                        }))}
                        disabled={isUploading}
                      />
                      <label className="form-check-label small" htmlFor="detectTableTypes">
                        Auto-detect table types
                      </label>
                    </div>
                  </div>
                  <div className="col-12">
                    <div className="form-check">
                      <input
                        className="form-check-input"
                        type="checkbox"
                        id="validateOnly"
                        checked={uploadOptions.validateOnly}
                        onChange={(e) => setUploadOptions((prev: UploadOptions) => ({
                          ...prev,
                          validateOnly: e.target.checked
                        }))}
                        disabled={isUploading}
                      />
                      <label className="form-check-label small" htmlFor="validateOnly">
                        Validation only (dry run)
                      </label>
                    </div>
                  </div>
                  <div className="col-12">
                    <div className="form-check">
                      <input
                        className="form-check-input"
                        type="checkbox"
                        id="autoProvision"
                        checked={uploadOptions.autoProvision}
                        onChange={(e) => setUploadOptions((prev: UploadOptions) => ({
                          ...prev,
                          autoProvision: e.target.checked
                        }))}
                        disabled={isUploading || uploadOptions.validateOnly}
                      />
                      <label className="form-check-label small" htmlFor="autoProvision">
                        Auto-provision after upload
                      </label>
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Drop Zone */}
        <div
          className={`border-2 border-dashed rounded-4 p-5 text-center position-relative ${
            dragActive ? 'border-primary bg-primary bg-opacity-10' : 'border-secondary'
          } ${isUploading ? 'opacity-50' : ''}`}
          onDragEnter={handleDrag}
          onDragLeave={handleDrag}
          onDragOver={handleDrag}
          onDrop={handleDrop}
        >
          <input
            ref={fileInputRef}
            type="file"
            multiple
            accept={allowedFileTypes.join(',')}
            onChange={handleFileSelect}
            className="d-none"
            disabled={isUploading}
          />
          
          {isUploading ? (
            <div>
              <div className="spinner-border text-primary mb-3" role="status">
                <span className="visually-hidden">Processing...</span>
              </div>
              <h6 className="fw-semibold text-dark mb-2">{status}</h6>
              <div className="progress mx-auto mb-2" style={{width: '300px', height: '8px'}}>
                <div
                  className="progress-bar bg-primary"
                  style={{width: `${progress}%`}}
                ></div>
              </div>
              <p className="small text-muted mb-3">{progress.toFixed(1)}% Complete</p>
              <button
                className="btn btn-outline-danger btn-sm rounded-3"
                onClick={cancelUpload}
              >
                <Pause size={14} className="me-1" />
                Cancel Upload
              </button>
            </div>
          ) : (
            <div>
              <Upload size={48} className={`mb-3 ${dragActive ? 'text-primary' : 'text-muted'}`} />
              <h6 className="fw-semibold text-dark mb-2">
                {dragActive ? 'Drop files here' : 'Drag & drop files or click to browse'}
              </h6>
              <p className="text-muted small mb-3">
                Supported formats: {allowedFileTypes.join(', ')} • Max {maxFileSize}MB per file • Max {maxFiles} files
              </p>
              <button
                className="btn btn-primary rounded-3"
                onClick={() => fileInputRef.current?.click()}
                disabled={files.length >= maxFiles}
              >
                <Upload size={16} className="me-2" />
                Choose Files
              </button>
            </div>
          )}
        </div>

        {/* Selected Files */}
        {files.length > 0 && (
          <div className="mt-4">
            <div className="d-flex align-items-center justify-content-between mb-3">
              <h6 className="fw-semibold text-dark mb-0">
                Selected Files ({files.length}/{maxFiles})
              </h6>
              <button
                className="btn btn-outline-secondary btn-sm rounded-3"
                onClick={clearFiles}
                disabled={isUploading}
              >
                <X size={14} className="me-1" />
                Clear All
              </button>
            </div>

            <div className="row g-3">
              {files.map((file) => (
                <div key={file.id} className="col-12">
                  <div className={`border rounded-3 p-3 ${file.isValid ? 'border-success bg-success bg-opacity-5' : 'border-danger bg-danger bg-opacity-5'}`}>
                    <div className="d-flex align-items-center justify-content-between">
                      <div className="d-flex align-items-center flex-grow-1">
                        {getFileIcon(file.type)}
                        <div className="ms-3 flex-grow-1">
                          <div className="fw-semibold text-dark">{file.name}</div>
                          <div className="small text-muted">{file.size} • {file.type}</div>
                        </div>
                        {file.isValid ? (
                          <Check className="text-success" size={20} />
                        ) : (
                          <AlertTriangle className="text-danger" size={20} />
                        )}
                      </div>
                      
                      <button
                        className="btn btn-outline-secondary btn-sm rounded-3 ms-3"
                        onClick={() => removeFile(file.id)}
                        disabled={isUploading}
                      >
                        <X size={14} />
                      </button>
                    </div>
                    
                    {!file.isValid && file.errors.length > 0 && (
                      <div className="mt-2">
                        {file.errors.map((error, index) => (
                          <div key={index} className="small text-danger">
                            • {error}
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>

            {/* Upload Controls */}
            <div className="d-flex align-items-center justify-content-between mt-4">
              <div className="d-flex align-items-center gap-3">
                <span className="small text-muted">
                  {files.filter(f => f.isValid).length} valid files ready for upload
                </span>
              </div>
              
              <div className="d-flex gap-2">
                <button
                  className="btn btn-outline-secondary rounded-3"
                  onClick={resetUpload}
                  disabled={isUploading}
                >
                  <RotateCcw size={16} className="me-1" />
                  Reset
                </button>
                <button
                  className="btn btn-primary rounded-3"
                  onClick={handleStartUpload}
                  disabled={isUploading || files.filter(f => f.isValid).length === 0 || loading}
                >
                  {loading ? (
                    <>
                      <div className="spinner-border spinner-border-sm me-2" role="status">
                        <span className="visually-hidden">Loading...</span>
                      </div>
                      Starting...
                    </>
                  ) : (
                    <>
                      <Play size={16} className="me-1" />
                      Start Upload
                    </>
                  )}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Error Display */}
        {(error || errors.length > 0 || connectionError) && (
          <div className="alert alert-danger mt-4" role="alert">
            <div className="d-flex align-items-start">
              <AlertTriangle size={20} className="text-danger me-2 flex-shrink-0 mt-1" />
              <div className="flex-grow-1">
                <h6 className="alert-heading fw-semibold mb-2">Upload Issues</h6>
                {error && <div className="mb-1">• {error}</div>}
                {connectionError && <div className="mb-1">• Connection: {connectionError}</div>}
                {errors.map((err, index) => (
                  <div key={index} className="mb-1">• {err}</div>
                ))}
              </div>
              <button
                className="btn btn-link btn-sm p-0"
                onClick={clearError}
              >
                <X size={16} />
              </button>
            </div>
          </div>
        )}

        {/* Results Display */}
        {results.length > 0 && (
          <div className="alert alert-success mt-4" role="alert">
            <div className="d-flex align-items-start">
              <Check size={20} className="text-success me-2 flex-shrink-0 mt-1" />
              <div className="flex-grow-1">
                <h6 className="alert-heading fw-semibold mb-2">Upload Results</h6>
                <div className="small">
                  {results.length} file(s) processed successfully
                </div>
              </div>
              <button
                className="btn btn-outline-success btn-sm rounded-3"
                onClick={() => {
                  // Handle download results
                  console.log('Download results:', results);
                }}
              >
                <Download size={14} className="me-1" />
                Download
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default BulkUploadForm;