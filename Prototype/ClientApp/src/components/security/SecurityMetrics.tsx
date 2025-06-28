import React from 'react';
import { Users, AlertTriangle, AlertCircle, CheckCircle2 } from 'lucide-react';

interface LocalSecurityOverview {
  riskScore: number;
  riskLevel: string;
  activeSessions: number;
  unverifiedUsers: number;
  failedLoginsToday: number;
  successfulLoginsToday: number;
  applicationChanges: number;
}

interface SecurityMetricsProps {
  overview: LocalSecurityOverview;
}

export default function SecurityMetrics({ overview }: SecurityMetricsProps) {
  const metrics = [
    {
      icon: Users,
      color: 'info',
      value: overview.activeSessions,
      label: 'Active Sessions'
    },
    {
      icon: AlertTriangle,
      color: 'warning',
      value: overview.unverifiedUsers,
      label: 'Unverified Users'
    },
    {
      icon: AlertCircle,
      color: 'danger',
      value: overview.failedLoginsToday,
      label: 'Failed Logins (24h)'
    },
    {
      icon: CheckCircle2,
      color: 'success',
      value: overview.successfulLoginsToday,
      label: 'Successful Logins (24h)'
    }
  ];

  return (
    <div className="row g-3 h-100">
      {metrics.map((metric, index) => (
        <div key={index} className="col-md-3">
          <div className="card border-0 rounded-4 shadow-sm h-100">
            <div className="card-body p-3 text-center">
              <metric.icon className={`text-${metric.color} mb-2`} size={24} />
              <h4 className="fw-bold mb-1">{metric.value}</h4>
              <small className="text-muted">{metric.label}</small>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}