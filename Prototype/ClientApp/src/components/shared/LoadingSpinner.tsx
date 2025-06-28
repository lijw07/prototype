import React from 'react';

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  className?: string;
  text?: string;
  fullScreen?: boolean;
  variant?: 'primary' | 'secondary' | 'success' | 'danger' | 'warning' | 'info' | 'light' | 'dark';
}

const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({ 
  size = 'md', 
  className = '', 
  text,
  fullScreen = false,
  variant = 'primary'
}) => {
  const sizeClasses = {
    sm: 'spinner-border-sm',
    md: '',
    lg: 'spinner-border-lg'
  };

  const spinner = (
    <div className={`d-flex flex-column align-items-center justify-content-center ${className}`}>
      <div 
        className={`spinner-border text-${variant} ${sizeClasses[size]}`} 
        role="status"
        aria-hidden="true"
      />
      {text && (
        <div className="mt-2 text-muted small">
          {text}
        </div>
      )}
      <span className="visually-hidden">Loading...</span>
    </div>
  );

  if (fullScreen) {
    return (
      <div className="min-vh-100 bg-light d-flex justify-content-center align-items-center">
        {spinner}
      </div>
    );
  }

  return spinner;
};

export default LoadingSpinner;