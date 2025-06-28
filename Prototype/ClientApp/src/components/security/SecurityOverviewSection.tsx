import React from 'react';
import { Shield, AlertCircle, AlertTriangle, Eye, CheckCircle2 } from 'lucide-react';

interface LocalSecurityOverview {
  riskScore: number;
  riskLevel: string;
  activeSessions: number;
  unverifiedUsers: number;
  failedLoginsToday: number;
  successfulLoginsToday: number;
  applicationChanges: number;
}

interface SecurityOverviewSectionProps {
  overview: LocalSecurityOverview;
}

export default function SecurityOverviewSection({ overview }: SecurityOverviewSectionProps) {
  const getRiskColor = (riskLevel: string) => {
    switch (riskLevel) {
      case 'HIGH': return 'danger';
      case 'MEDIUM': return 'warning';
      case 'LOW': return 'info';
      default: return 'success';
    }
  };

  const getRiskIcon = (riskLevel: string) => {
    switch (riskLevel) {
      case 'HIGH': return AlertCircle;
      case 'MEDIUM': return AlertTriangle;
      case 'LOW': return Eye;
      default: return CheckCircle2;
    }
  };

  const RiskIcon = getRiskIcon(overview.riskLevel);
  const riskColor = getRiskColor(overview.riskLevel);

  return (
    <div className="card border-0 rounded-4 shadow-sm h-100">
      <div className="card-body p-4 text-center">
        <RiskIcon className={`text-${riskColor} mb-3`} size={48} />
        <h3 className={`text-${riskColor} fw-bold`}>{overview.riskScore}/100</h3>
        <h6 className="fw-semibold mb-0">Risk Score</h6>
        <span className={`badge bg-${riskColor} bg-opacity-10 text-${riskColor} mt-2`}>
          {overview.riskLevel} RISK
        </span>
      </div>
    </div>
  );
}