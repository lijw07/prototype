import React from 'react';
import { X, CheckCircle, XCircle, AlertTriangle, Info } from 'lucide-react';
import { Notification, NotificationType } from '../../hooks/shared/useNotifications';

interface NotificationContainerProps {
  notifications: Notification[];
  onRemove: (id: string) => void;
  position?: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left' | 'top-center';
  maxNotifications?: number;
}

const iconMap: Record<NotificationType, React.ComponentType<any>> = {
  success: CheckCircle,
  error: XCircle,
  warning: AlertTriangle,
  info: Info
};

const alertClassMap: Record<NotificationType, string> = {
  success: 'alert-success',
  error: 'alert-danger',
  warning: 'alert-warning',
  info: 'alert-info'
};

export default function NotificationContainer({
  notifications,
  onRemove,
  position = 'top-right',
  maxNotifications = 5
}: NotificationContainerProps) {
  const positionClasses = {
    'top-right': 'top-0 end-0',
    'top-left': 'top-0 start-0',
    'bottom-right': 'bottom-0 end-0',
    'bottom-left': 'bottom-0 start-0',
    'top-center': 'top-0 start-50 translate-middle-x'
  };

  const visibleNotifications = notifications.slice(0, maxNotifications);

  if (visibleNotifications.length === 0) {
    return null;
  }

  return (
    <div 
      className={`position-fixed ${positionClasses[position]} p-3`}
      style={{ zIndex: 1055 }}
    >
      <div className="d-flex flex-column gap-2">
        {visibleNotifications.map((notification) => (
          <NotificationItem
            key={notification.id}
            notification={notification}
            onRemove={onRemove}
          />
        ))}
      </div>
    </div>
  );
}

interface NotificationItemProps {
  notification: Notification;
  onRemove: (id: string) => void;
}

function NotificationItem({ notification, onRemove }: NotificationItemProps) {
  const Icon = iconMap[notification.type];
  const alertClass = alertClassMap[notification.type];

  return (
    <div 
      className={`alert ${alertClass} alert-dismissible d-flex align-items-start shadow-sm`}
      style={{ 
        minWidth: '300px',
        maxWidth: '400px',
        animation: 'slideInRight 0.3s ease-out'
      }}
      role="alert"
    >
      <Icon size={20} className="me-2 flex-shrink-0 mt-1" />
      
      <div className="flex-grow-1">
        {notification.title && (
          <h6 className="alert-heading mb-1">{notification.title}</h6>
        )}
        <div className="mb-0" style={{ whiteSpace: 'pre-line' }}>
          {notification.message}
        </div>
        
        {notification.actions && notification.actions.length > 0 && (
          <div className="mt-2 d-flex gap-2">
            {notification.actions.map((action, index) => (
              <button
                key={index}
                className={`btn btn-sm ${action.style === 'primary' ? `btn-${notification.type}` : 'btn-outline-secondary'}`}
                onClick={() => {
                  action.action();
                  onRemove(notification.id);
                }}
              >
                {action.label}
              </button>
            ))}
          </div>
        )}
      </div>
      
      <button
        type="button"
        className="btn-close"
        aria-label="Close"
        onClick={() => onRemove(notification.id)}
      />
    </div>
  );
}

// CSS for animations
export const notificationStyles = `
  @keyframes slideInRight {
    from {
      transform: translateX(100%);
      opacity: 0;
    }
    to {
      transform: translateX(0);
      opacity: 1;
    }
  }

  @keyframes slideOutRight {
    from {
      transform: translateX(0);
      opacity: 1;
    }
    to {
      transform: translateX(100%);
      opacity: 0;
    }
  }

  .notification-exit {
    animation: slideOutRight 0.3s ease-in forwards;
  }
`;