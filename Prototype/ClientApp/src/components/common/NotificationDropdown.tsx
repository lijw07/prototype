import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Bell, RefreshCw, CheckCircle, AlertCircle, Upload, X, Clock } from 'lucide-react';
import { useMigration } from '../../contexts/MigrationContext';

interface Notification {
  id: string;
  type: 'migration' | 'system' | 'user';
  status: 'processing' | 'completed' | 'error' | 'info';
  title: string;
  message: string;
  progress?: number;
  timestamp: Date;
  data?: any;
  isRead: boolean;
}

interface NotificationDropdownProps {
  isOpen: boolean;
  onToggle: () => void;
}

const NOTIFICATIONS_STORAGE_KEY = 'cams_notifications';
const DISMISSED_NOTIFICATIONS_KEY = 'cams_dismissed_notifications';

export const NotificationDropdown: React.FC<NotificationDropdownProps> = ({ 
  isOpen, 
  onToggle 
}) => {
  const navigate = useNavigate();
  const { migrationState, setShouldNavigateToBulkTab } = useMigration();
  
  // Track dismissed notification IDs
  const [dismissedNotifications, setDismissedNotifications] = useState<Set<string>>(() => {
    try {
      const stored = localStorage.getItem(DISMISSED_NOTIFICATIONS_KEY);
      return stored ? new Set(JSON.parse(stored)) : new Set();
    } catch (error) {
      console.error('Error loading dismissed notifications from localStorage:', error);
      return new Set();
    }
  });

  const [notifications, setNotifications] = useState<Notification[]>(() => {
    // Load notifications from localStorage on initialization
    try {
      const stored = localStorage.getItem(NOTIFICATIONS_STORAGE_KEY);
      if (stored) {
        const parsed = JSON.parse(stored);
        // Convert timestamp strings back to Date objects
        return parsed.map((n: any) => ({
          ...n,
          timestamp: new Date(n.timestamp)
        }));
      }
      return [];
    } catch (error) {
      console.error('Error loading notifications from localStorage:', error);
      return [];
    }
  });

  // Save notifications to localStorage whenever they change
  useEffect(() => {
    try {
      localStorage.setItem(NOTIFICATIONS_STORAGE_KEY, JSON.stringify(notifications));
    } catch (error) {
      console.error('Error saving notifications to localStorage:', error);
    }
  }, [notifications]);

  // Save dismissed notifications to localStorage whenever they change
  useEffect(() => {
    try {
      localStorage.setItem(DISMISSED_NOTIFICATIONS_KEY, JSON.stringify([...dismissedNotifications]));
    } catch (error) {
      console.error('Error saving dismissed notifications to localStorage:', error);
    }
  }, [dismissedNotifications]);

  // Clean up old notifications and dismissed IDs periodically
  useEffect(() => {
    const cleanup = () => {
      const now = new Date();
      const sevenDaysAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
      
      setNotifications(prev => {
        const cleaned = prev
          .filter(n => n.timestamp > sevenDaysAgo || n.status === 'processing')
          .slice(0, 10); // Keep only 10 most recent
        
        return cleaned.length !== prev.length ? cleaned : prev;
      });

      // Also clean up old dismissed notification IDs (older than 7 days)
      setDismissedNotifications(prev => {
        const activeNotificationIds = new Set(notifications.map(n => n.id));
        const recentDismissedIds = new Set([...prev].filter(id => {
          // Keep IDs that still have active notifications or might be migration-related
          return activeNotificationIds.has(id) || id.startsWith('migration-');
        }));
        return recentDismissedIds.size !== prev.size ? recentDismissedIds : prev;
      });
    };

    // Clean up on mount and then every hour
    cleanup();
    const interval = setInterval(cleanup, 60 * 60 * 1000);
    
    return () => clearInterval(interval);
  }, [notifications]);

  // Generate notifications based on migration state
  useEffect(() => {
    if (!migrationState || migrationState.status === 'idle') {
      // Clear migration notifications when idle
      setNotifications(prev => prev.filter(n => n.type !== 'migration'));
      return;
    }

    const migrationNotificationId = `migration-${migrationState.jobId || 'unknown'}`;
    
    // Don't create notification if it was previously dismissed and migration is completed/failed
    if (dismissedNotifications.has(migrationNotificationId) && 
        (migrationState.status === 'completed' || migrationState.status === 'error')) {
      return;
    }

    const migrationNotification: Notification = {
      id: migrationNotificationId,
      type: 'migration',
      status: migrationState.status,
      title: 'Bulk Migration',
      message: getStatusMessage(migrationState.status, migrationState.progress || 0),
      progress: migrationState.progress,
      timestamp: migrationState.startTime ? new Date(migrationState.startTime) : new Date(),
      data: migrationState.results,
      isRead: false
    };

    // Update or add migration notification
    setNotifications(prev => {
      const filtered = prev.filter(n => n.type !== 'migration');
      const existingMigration = prev.find(n => n.type === 'migration');
      
      // Preserve read status if notification already exists
      if (existingMigration) {
        migrationNotification.isRead = existingMigration.isRead;
      }
      
      return [migrationNotification, ...filtered];
    });
  }, [migrationState, dismissedNotifications]);

  const getStatusMessage = (status: string, progress: number): string => {
    switch (status) {
      case 'processing':
        return `Migration in progress... ${Math.round(progress)}%`;
      case 'completed':
        return 'Migration completed successfully';
      case 'error':
        return 'Migration failed with errors';
      default:
        return 'Migration status unknown';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'processing':
        return <RefreshCw className="rotating text-warning" size={16} />;
      case 'completed':
        return <CheckCircle className="text-success" size={16} />;
      case 'error':
        return <AlertCircle className="text-danger" size={16} />;
      case 'info':
        return <Clock className="text-info" size={16} />;
      default:
        return <Bell className="text-secondary" size={16} />;
    }
  };

  const getStatusColor = (status: string): string => {
    switch (status) {
      case 'processing':
        return 'warning';
      case 'completed':
        return 'success';
      case 'error':
        return 'danger';
      case 'info':
        return 'info';
      default:
        return 'secondary';
    }
  };

  const removeNotification = (id: string) => {
    // Track that this notification was dismissed
    setDismissedNotifications(prev => new Set([...prev, id]));
    // Remove from current notifications
    setNotifications(prev => prev.filter(n => n.id !== id));
  };

  const markAsRead = (id: string) => {
    setNotifications(prev => 
      prev.map(n => n.id === id ? { ...n, isRead: true } : n)
    );
  };

  const markAllAsRead = () => {
    setNotifications(prev => 
      prev.map(n => ({ ...n, isRead: true }))
    );
  };

  const clearAllNotifications = () => {
    // Track all cleared notifications as dismissed (except processing ones)
    setNotifications(prev => {
      const toRemove = prev.filter(n => !(n.type === 'migration' && n.status === 'processing'));
      const removedIds = toRemove.map(n => n.id);
      
      // Add removed IDs to dismissed set
      setDismissedNotifications(dismissed => new Set([...dismissed, ...removedIds]));
      
      // Keep only processing migration notifications
      return prev.filter(n => n.type === 'migration' && n.status === 'processing');
    });
  };

  const navigateToMigration = () => {
    // Set flag to navigate to bulk tab
    setShouldNavigateToBulkTab(true);
    // Navigate to user provisioning page
    navigate('/user-provisioning');
    // Close the dropdown
    onToggle();
  };

  const unreadCount = notifications.filter(n => !n.isRead).length;
  const hasActiveNotifications = notifications.some(n => n.status === 'processing');

  return (
    <div className="position-relative">
      {/* Notification Bell Button */}
      <button
        className="btn btn-link p-2 border-0 d-flex align-items-center text-decoration-none position-relative"
        onClick={onToggle}
        style={{ background: 'none' }}
        title="Notifications"
      >
        <Bell 
          size={20} 
          className={`${hasActiveNotifications ? 'text-warning' : 'text-muted'}`} 
        />
        {unreadCount > 0 && (
          <span 
            className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger"
            style={{ fontSize: '10px', padding: '2px 6px' }}
          >
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {/* Dropdown content */}
      {isOpen && (
        <div 
          className="position-absolute end-0 mt-2 bg-white border rounded shadow-lg"
          style={{ minWidth: '360px', maxWidth: '400px', zIndex: 1001, maxHeight: '500px' }}
        >
          {/* Header */}
          <div className="d-flex align-items-center justify-content-between p-3 border-bottom">
            <h6 className="fw-bold mb-0">Notifications</h6>
            {notifications.length > 0 && (
              <div className="d-flex gap-1">
                {unreadCount > 0 && (
                  <button 
                    onClick={markAllAsRead}
                    className="btn btn-sm btn-outline-primary"
                    style={{ fontSize: '12px', padding: '2px 8px' }}
                    title="Mark all as read"
                  >
                    Mark Read
                  </button>
                )}
                <button 
                  onClick={clearAllNotifications}
                  className="btn btn-sm btn-outline-secondary"
                  style={{ fontSize: '12px', padding: '2px 8px' }}
                  title="Remove all notifications"
                >
                  Clear All
                </button>
              </div>
            )}
          </div>

          {/* Notifications List */}
          <div className="overflow-auto" style={{ maxHeight: '400px' }}>
            {notifications.length === 0 ? (
              <div className="text-center p-4 text-muted">
                <Bell size={32} className="text-muted mb-2" />
                <p className="mb-0">No notifications</p>
                <small>You're all caught up!</small>
              </div>
            ) : (
              <div>
                {notifications.map((notification) => (
                  <div 
                    key={notification.id}
                    className="border-bottom p-3 position-relative notification-item"
                    style={{ cursor: notification.type === 'migration' ? 'pointer' : 'default' }}
                    onMouseEnter={() => {
                      if (!notification.isRead) {
                        markAsRead(notification.id);
                      }
                    }}
                    onClick={() => {
                      if (notification.type === 'migration') {
                        markAsRead(notification.id);
                        navigateToMigration();
                      }
                    }}
                  >
                    {/* Unread indicator dot */}
                    {!notification.isRead && (
                      <div 
                        className="position-absolute bg-primary rounded-circle"
                        style={{ 
                          width: '8px', 
                          height: '8px', 
                          top: '12px', 
                          left: '12px',
                          zIndex: 1
                        }}
                      />
                    )}
                    <div className="d-flex align-items-start">
                      <div className="me-2 mt-1">
                        {getStatusIcon(notification.status)}
                      </div>
                      <div className="flex-grow-1 me-2">
                        <div className="d-flex align-items-center justify-content-between mb-1">
                          <h6 className={`mb-0 small ${!notification.isRead ? 'fw-bold' : 'fw-semibold'}`}>
                            {notification.title}
                          </h6>
                          <small className="text-muted">
                            {notification.timestamp.toLocaleTimeString([], { 
                              hour: '2-digit', 
                              minute: '2-digit' 
                            })}
                          </small>
                        </div>
                        <p className="mb-0 small text-muted">{notification.message}</p>

                        {/* Progress bar for processing notifications */}
                        {notification.status === 'processing' && notification.progress !== undefined && (
                          <div className="mt-2">
                            <div className="progress" style={{ height: '4px' }}>
                              <div
                                className="progress-bar progress-bar-striped progress-bar-animated bg-warning"
                                style={{ width: `${notification.progress}%` }}
                              />
                            </div>
                            <div className="d-flex justify-content-between mt-1">
                              <small className="text-muted">Progress</small>
                              <small className="text-muted fw-semibold">
                                {Math.round(notification.progress)}%
                              </small>
                            </div>
                          </div>
                        )}

                        {/* Results summary for completed migrations */}
                        {notification.status === 'completed' && notification.data && (
                          <div className="mt-2">
                            <div className="row g-1">
                              <div className="col-4">
                                <div className="text-center p-1 bg-success bg-opacity-10 rounded">
                                  <div className="fw-bold text-success" style={{ fontSize: '12px' }}>
                                    {notification.data.successful}
                                  </div>
                                  <div style={{ fontSize: '10px' }} className="text-success">Success</div>
                                </div>
                              </div>
                              <div className="col-4">
                                <div className="text-center p-1 bg-danger bg-opacity-10 rounded">
                                  <div className="fw-bold text-danger" style={{ fontSize: '12px' }}>
                                    {notification.data.failed}
                                  </div>
                                  <div style={{ fontSize: '10px' }} className="text-danger">Failed</div>
                                </div>
                              </div>
                              <div className="col-4">
                                <div className="text-center p-1 bg-info bg-opacity-10 rounded">
                                  <div className="fw-bold text-info" style={{ fontSize: '12px' }}>
                                    {notification.data.processedFiles}/{notification.data.totalFiles}
                                  </div>
                                  <div style={{ fontSize: '10px' }} className="text-info">Files</div>
                                </div>
                              </div>
                            </div>
                          </div>
                        )}
                      </div>

                      {/* Remove button - only show for non-processing notifications */}
                      {notification.status !== 'processing' && (
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            removeNotification(notification.id);
                          }}
                          className="btn btn-sm p-1 opacity-50"
                          style={{ width: '20px', height: '20px' }}
                          title="Remove notification"
                          onMouseEnter={(e) => {
                            e.currentTarget.classList.remove('opacity-50');
                            e.currentTarget.classList.add('opacity-100');
                          }}
                          onMouseLeave={(e) => {
                            e.currentTarget.classList.remove('opacity-100');
                            e.currentTarget.classList.add('opacity-50');
                          }}
                        >
                          <X size={12} className="text-muted" />
                        </button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Footer - Show action button for migration notifications */}
          {notifications.some(n => n.type === 'migration') && (
            <div className="p-3 border-top text-center">
              <button 
                className="btn btn-primary btn-sm"
                onClick={navigateToMigration}
              >
                <Upload size={14} className="me-1" />
                View Migration Details
              </button>
            </div>
          )}
        </div>
      )}

      <style>{`
        .rotating {
          animation: spin 1s linear infinite;
        }
        
        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }

        .notification-item:hover {
          background-color: rgba(0, 0, 0, 0.02);
        }

        .notification-item[style*="cursor: pointer"]:hover {
          background-color: rgba(13, 110, 253, 0.05);
        }
      `}</style>
    </div>
  );
};