import React from 'react';

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  className?: string;
  text?: string;
}

const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({ 
  size = 'md', 
  className = '', 
  text 
}) => {
  const sizeClasses = {
    sm: 'h-4 w-4',
    md: 'h-8 w-8',
    lg: 'h-12 w-12'
  };

  return (
    <div className={`d-flex flex-column align-items-center justify-content-center ${className}`}>
      <div 
        className={`spinner-border text-primary ${sizeClasses[size]}`} 
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
};

export default LoadingSpinner;