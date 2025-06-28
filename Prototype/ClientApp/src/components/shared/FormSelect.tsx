import React from 'react';
import { AlertCircle } from 'lucide-react';

interface SelectOption {
  value: string | number;
  label: string;
  disabled?: boolean;
}

interface FormSelectProps extends Omit<React.SelectHTMLAttributes<HTMLSelectElement>, 'children'> {
  label?: string;
  error?: string;
  helpText?: string;
  options: SelectOption[];
  placeholder?: string;
  containerClassName?: string;
  labelClassName?: string;
  selectClassName?: string;
  required?: boolean;
}

export default function FormSelect({
  label,
  error,
  helpText,
  options,
  placeholder = 'Select an option...',
  containerClassName = '',
  labelClassName = '',
  selectClassName = '',
  required = false,
  id,
  ...selectProps
}: FormSelectProps) {
  const selectId = id || `select-${Math.random().toString(36).substr(2, 9)}`;
  
  return (
    <div className={`mb-3 ${containerClassName}`}>
      {label && (
        <label 
          htmlFor={selectId} 
          className={`form-label ${labelClassName} ${required ? 'required' : ''}`}
        >
          {label}
          {required && <span className="text-danger ms-1">*</span>}
        </label>
      )}
      <select
        id={selectId}
        className={`form-select ${error ? 'is-invalid' : ''} ${selectClassName}`}
        {...selectProps}
      >
        {placeholder && <option value="">{placeholder}</option>}
        {options.map((option) => (
          <option 
            key={option.value} 
            value={option.value}
            disabled={option.disabled}
          >
            {option.label}
          </option>
        ))}
      </select>
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