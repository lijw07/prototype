import React from 'react';
import { MapPin, AlertTriangle, CheckCircle2 } from 'lucide-react';

interface SuspiciousIp {
  IpAddress: string;
  Count: number;
}

interface SuspiciousActivityProps {
  suspiciousIps: SuspiciousIp[];
}

export default function SuspiciousActivity({ suspiciousIps }: SuspiciousActivityProps) {
  return (
    <div className="card border-0 rounded-4 shadow-sm h-100">
      <div className="card-body p-4">
        <div className="d-flex align-items-center mb-4">
          <MapPin className="text-danger me-3" size={24} />
          <h5 className="card-title fw-bold mb-0">Suspicious IP Addresses</h5>
        </div>
        
        {suspiciousIps.length > 0 ? (
          <div className="space-y-3">
            {suspiciousIps.map((ip) => (
              <div key={ip.IpAddress} className="d-flex justify-content-between align-items-center py-2 px-3 bg-light rounded-3">
                <div className="d-flex align-items-center">
                  <AlertTriangle className="text-warning me-2" size={16} />
                  <code className="text-dark">{ip.IpAddress}</code>
                </div>
                <span className="badge bg-danger">{ip.Count} failed attempts</span>
              </div>
            ))}
          </div>
        ) : (
          <div className="text-center py-4 text-muted">
            <CheckCircle2 size={32} className="mb-2 opacity-50" />
            <div>No suspicious activity detected</div>
          </div>
        )}
      </div>
    </div>
  );
}