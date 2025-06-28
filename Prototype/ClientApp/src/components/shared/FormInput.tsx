import React from 'react';
import { AlertCircle } from 'lucide-react';

interface FormInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  helpText?: string;
  containerClassName?: string;
  labelClassName?: string;
  inputClassName?: string;
  required?: boolean;
}

export default function FormInput({
  label,
  error,
  helpText,
  containerClassName = '',
  labelClassName = '',
  inputClassName = '',
  required = false,
  id,
  ...inputProps
}: FormInputProps) {
  const inputId = id || `input-${Math.random().toString(36).substr(2, 9)}`;
  
  return (
    <div className={`mb-3 ${containerClassName}`}>
      {label && (
        <label 
          htmlFor={inputId} 
          className={`form-label ${labelClassName} ${required ? 'required' : ''}`}
        >
          {label}
          {required && <span className="text-danger ms-1">*</span>}
        </label>
      )}
      <input
        id={inputId}
        className={`form-control ${error ? 'is-invalid' : ''} ${inputClassName}`}
        {...inputProps}
      />
      {helpText && !error && (
        <small className="form-text text-muted">{helpText}</small>
      )}
      {error && (
        <div className="invalid-feedback d-flex align-items-center">
          <AlertCircle size={14} className="me-1" />
          {error}
        </div>
      )}
    </div>
  );
}