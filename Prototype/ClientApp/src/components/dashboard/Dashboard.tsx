import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { 
  BarChart3, 
  Database, 
  Users, 
  Activity, 
  Shield, 
  Clock, 
  TrendingUp, 
  Server,
  AlertTriangle,
  CheckCircle2,
  FileText,
  Key
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';

interface DashboardStats {
  totalApplications: number;
  activeConnections: number;
  totalUsers: number;
  recentActivity: number;
  systemHealth: 'healthy' | 'warning' | 'error';
  uptime: string;
}

export default function Dashboard() {
  const [seconds, setSeconds] = useState(0);
  const [currentTime, setCurrentTime] = useState(new Date());
  const { user } = useAuth();
  const navigate = useNavigate();
  
  // Mock dashboard stats - in real app, this would come from API
  const [stats] = useState<DashboardStats>({
    totalApplications: 12,
    activeConnections: 8,
    totalUsers: 24,
    recentActivity: 156,
    systemHealth: 'healthy',
    uptime: '99.9%'
  });

  useEffect(() => {
    const intervalId = setInterval(() => {
      setSeconds((prevSeconds) => prevSeconds + 1);
      setCurrentTime(new Date());
    }, 1000);

    return () => clearInterval(intervalId);
  }, []);

  const formatUptime = (seconds: number) => {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;
    return `${hours}h ${minutes}m ${secs}s`;
  };

  const getGreeting = () => {
    const hour = currentTime.getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 18) return 'Good afternoon';
    return 'Good evening';
  };

  const dashboardCards = [
    {
      title: 'Applications',
      value: stats.totalApplications,
      icon: Database,
      color: 'primary',
      description: 'Connected databases'
    },
    {
      title: 'Active Connections',
      value: stats.activeConnections,
      icon: Server,
      color: 'success',
      description: 'Currently active'
    },
    {
      title: 'Total Users',
      value: stats.totalUsers,
      icon: Users,
      color: 'info',
      description: 'Registered users'
    },
    {
      title: 'Recent Activity',
      value: stats.recentActivity,
      icon: Activity,
      color: 'warning',
      description: 'Last 24 hours'
    }
  ];

  return (
    <div className="min-vh-100 bg-light">
      <div className="container-fluid py-4">
        {/* Header Section */}
        <div className="row mb-4">
          <div className="col-12">
            <div className="card border-0 rounded-4 shadow-sm bg-white">
              <div className="card-body p-4">
                <div className="row align-items-center">
                  <div className="col-md-8">
                    <h1 className="display-6 fw-bold mb-2 text-dark">
                      {getGreeting()}, {user?.username || 'User'}! 👋
                    </h1>
                    <p className="lead mb-0 text-muted">
                      Welcome to the Central Access Management System (CAMS)
                    </p>
                    <p className="small mb-0 text-muted">
                      System uptime: {formatUptime(seconds)} • {currentTime.toLocaleDateString()} {currentTime.toLocaleTimeString()}
                    </p>
                  </div>
                  <div className="col-md-4 text-end">
                    <div className="d-flex align-items-center justify-content-end">
                      {stats.systemHealth === 'healthy' ? (
                        <CheckCircle2 size={48} className="text-success" />
                      ) : (
                        <AlertTriangle size={48} className="text-warning" />
                      )}
                      <div className="ms-3">
                        <div className="fw-bold text-dark">System Status</div>
                        <div className="small text-muted">
                          {stats.systemHealth === 'healthy' ? 'All Systems Operational' : 'Minor Issues'}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Stats Cards */}
        <div className="row g-4 mb-4">
          {dashboardCards.map((card, index) => {
            const IconComponent = card.icon;
            return (
              <div key={index} className="col-lg-3 col-md-6">
                <div className="card border-0 rounded-4 shadow-sm h-100 dashboard-card animate-fade-in" 
                     style={{ animationDelay: `${index * 0.1}s` }}>
                  <div className="card-body p-4">
                    <div className="d-flex align-items-center justify-content-between mb-3">
                      <div className={`rounded-3 p-3 bg-${card.color} bg-opacity-10`}>
                        <IconComponent className={`text-${card.color}`} size={24} />
                      </div>
                      <TrendingUp className="text-success" size={20} />
                    </div>
                    <h3 className="display-6 fw-bold text-dark mb-1">{card.value}</h3>
                    <h6 className="fw-semibold text-muted mb-1">{card.title}</h6>
                    <p className="small text-muted mb-0">{card.description}</p>
                  </div>
                </div>
              </div>
            );
          })}
        </div>

        {/* Content Grid */}
        <div className="row g-4">
          {/* Recent Activity */}
          <div className="col-lg-8">
            <div className="card border-0 rounded-4 shadow-sm h-100">
              <div className="card-body p-4">
                <div className="d-flex align-items-center mb-4">
                  <Activity className="text-primary me-3" size={24} />
                  <h5 className="card-title fw-bold mb-0">Recent Activity</h5>
                </div>
                <div className="list-group list-group-flush">
                  <div className="list-group-item border-0 px-0 py-3">
                    <div className="d-flex align-items-start">
                      <div className="rounded-circle bg-success bg-opacity-10 p-2 me-3">
                        <CheckCircle2 className="text-success" size={16} />
                      </div>
                      <div className="flex-grow-1">
                        <div className="fw-semibold text-dark">Database connection established</div>
                        <div className="small text-muted">Production SQL Server • 2 minutes ago</div>
                      </div>
                    </div>
                  </div>
                  <div className="list-group-item border-0 px-0 py-3">
                    <div className="d-flex align-items-start">
                      <div className="rounded-circle bg-primary bg-opacity-10 p-2 me-3">
                        <Users className="text-primary" size={16} />
                      </div>
                      <div className="flex-grow-1">
                        <div className="fw-semibold text-dark">New user registered</div>
                        <div className="small text-muted">john.doe@company.com • 15 minutes ago</div>
                      </div>
                    </div>
                  </div>
                  <div className="list-group-item border-0 px-0 py-3">
                    <div className="d-flex align-items-start">
                      <div className="rounded-circle bg-warning bg-opacity-10 p-2 me-3">
                        <Shield className="text-warning" size={16} />
                      </div>
                      <div className="flex-grow-1">
                        <div className="fw-semibold text-dark">Security audit completed</div>
                        <div className="small text-muted">System security scan • 1 hour ago</div>
                      </div>
                    </div>
                  </div>
                  <div className="list-group-item border-0 px-0 py-3">
                    <div className="d-flex align-items-start">
                      <div className="rounded-circle bg-info bg-opacity-10 p-2 me-3">
                        <Database className="text-info" size={16} />
                      </div>
                      <div className="flex-grow-1">
                        <div className="fw-semibold text-dark">Application configuration updated</div>
                        <div className="small text-muted">Development environment • 3 hours ago</div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Quick Actions */}
          <div className="col-lg-4">
            <div className="card border-0 rounded-4 shadow-sm h-100">
              <div className="card-body p-4">
                <div className="d-flex align-items-center mb-4">
                  <BarChart3 className="text-primary me-3" size={24} />
                  <h5 className="card-title fw-bold mb-0">Quick Actions</h5>
                </div>
                <div className="d-grid gap-3">
                  <button 
                    className="btn btn-primary rounded-3 fw-semibold text-start"
                    onClick={() => navigate('/applications')}
                  >
                    <Database className="me-2" size={18} />
                    Add New Application
                  </button>
                  <button className="btn btn-outline-primary rounded-3 fw-semibold text-start">
                    <Users className="me-2" size={18} />
                    Manage Users
                  </button>
                  <button 
                    className="btn btn-outline-primary rounded-3 fw-semibold text-start"
                    onClick={() => navigate('/audit-logs')}
                  >
                    <Shield className="me-2" size={18} />
                    View Audit Logs
                  </button>
                  <button 
                    className="btn btn-outline-primary rounded-3 fw-semibold text-start"
                    onClick={() => navigate('/activity-logs')}
                  >
                    <Activity className="me-2" size={18} />
                    Activity Monitor
                  </button>
                  <button 
                    className="btn btn-outline-primary rounded-3 fw-semibold text-start"
                    onClick={() => navigate('/application-logs')}
                  >
                    <FileText className="me-2" size={18} />
                    Application Logs
                  </button>
                  <button 
                    className="btn btn-outline-primary rounded-3 fw-semibold text-start"
                    onClick={() => navigate('/roles')}
                  >
                    <Key className="me-2" size={18} />
                    Manage Roles
                  </button>
                </div>
                
                {/* System Info */}
                <div className="mt-4 pt-3 border-top">
                  <h6 className="fw-bold text-muted mb-3">System Information</h6>
                  <div className="row g-2 text-center">
                    <div className="col-6">
                      <div className="bg-light rounded-3 p-2">
                        <Clock className="text-primary" size={20} />
                        <div className="small fw-semibold mt-1">Uptime</div>
                        <div className="small text-muted">{stats.uptime}</div>
                      </div>
                    </div>
                    <div className="col-6">
                      <div className="bg-light rounded-3 p-2">
                        <TrendingUp className="text-success" size={20} />
                        <div className="small fw-semibold mt-1">Performance</div>
                        <div className="small text-muted">Excellent</div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}