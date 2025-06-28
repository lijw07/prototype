import React from 'react';
import { Activity, Zap, Clock } from 'lucide-react';

interface SecurityEvent {
  UserActivityLogId: string;
  ActionType: string;
  Description: string;
  Timestamp: string;
  IpAddress: string;
  UserId: string;
}

interface RecentSecurityEventsProps {
  recentEvents: SecurityEvent[];
}

export default function RecentSecurityEvents({ recentEvents }: RecentSecurityEventsProps) {
  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    return date.toLocaleDateString();
  };

  const getActionTypeColor = (actionType: string) => {
    switch (actionType) {
      case 'FailedLogin': return 'danger';
      case 'Login': return 'success';
      case 'ChangePassword': return 'warning';
      case 'ApplicationRemoved': return 'info';
      case 'RoleDeleted': return 'warning';
      default: return 'secondary';
    }
  };

  return (
    <div className="card border-0 rounded-4 shadow-sm h-100">
      <div className="card-body p-4">
        <div className="d-flex align-items-center mb-4">
          <Activity className="text-primary me-3" size={24} />
          <h5 className="card-title fw-bold mb-0">Recent Security Events</h5>
        </div>
        
        <div className="timeline" style={{ maxHeight: '300px', overflowY: 'auto' }}>
          {recentEvents.map((event) => (
            <div key={event.UserActivityLogId} className="d-flex align-items-start mb-3">
              <div className={`rounded-circle bg-${getActionTypeColor(event.ActionType)} bg-opacity-10 p-2 me-3 flex-shrink-0`}>
                <Zap className={`text-${getActionTypeColor(event.ActionType)}`} size={14} />
              </div>
              <div className="flex-grow-1">
                <div className="fw-semibold small">{event.Description}</div>
                <div className="text-muted small">
                  <Clock size={12} className="me-1" />
                  {formatTimestamp(event.Timestamp)}
                  {event.IpAddress && (
                    <>
                      <span className="mx-2">•</span>
                      <code className="small">{event.IpAddress}</code>
                    </>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}