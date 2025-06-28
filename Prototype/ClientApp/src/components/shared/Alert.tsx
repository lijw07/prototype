import React from 'react';
import { X, CheckCircle, XCircle, AlertTriangle, Info } from 'lucide-react';

interface AlertProps {
  type?: 'success' | 'danger' | 'warning' | 'info';
  message: string;
  title?: string;
  dismissible?: boolean;
  onDismiss?: () => void;
  className?: string;
  children?: React.ReactNode;
}

const iconMap = {
  success: CheckCircle,
  danger: XCircle,
  warning: AlertTriangle,
  info: Info
};

export default function Alert({
  type = 'info',
  message,
  title,
  dismissible = false,
  onDismiss,
  className = '',
  children
}: AlertProps) {
  const Icon = iconMap[type];

  return (
    <div 
      className={`alert alert-${type} d-flex align-items-start ${dismissible ? 'alert-dismissible' : ''} ${className}`} 
      role="alert"
    >
      <Icon size={20} className="me-2 flex-shrink-0 mt-1" />
      <div className="flex-grow-1">
        {title && <h6 className="alert-heading">{title}</h6>}
        <div>{message}</div>
        {children}
      </div>
      {dismissible && (
        <button 
          type="button" 
          className="btn-close" 
          aria-label="Close"
          onClick={onDismiss}
        />
      )}
    </div>
  );
}