import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { 
  Shield, 
  Database, 
  Users, 
  Zap, 
  ChevronRight, 
  Star,
  Globe,
  Lock,
  TrendingUp,
  CheckCircle,
  ArrowRight,
  Sparkles,
  Layers,
  Settings,
  BarChart3,
  Eye,
  Clock,
  Building2,
  Network,
  Workflow,
  FileCheck,
  Award,
  Headphones,
  Code,
  Cloud
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';

export default function Home() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [animatedCounters, setAnimatedCounters] = useState({
    enterprises: 0,
    connections: 0,
    users: 0,
    uptime: 0
  });

  // Animated counters effect
  useEffect(() => {
    const targets = { enterprises: 500, connections: 50000, users: 100000, uptime: 99.99 };
    const duration = 2500;
    const steps = 60;
    const interval = duration / steps;

    let step = 0;
    const timer = setInterval(() => {
      step++;
      const progress = step / steps;
      const easeOut = 1 - Math.pow(1 - progress, 3);

      setAnimatedCounters({
        enterprises: Math.floor(targets.enterprises * easeOut),
        connections: Math.floor(targets.connections * easeOut),
        users: Math.floor(targets.users * easeOut),
        uptime: Math.min(targets.uptime, (targets.uptime * easeOut))
      });

      if (step >= steps) {
        clearInterval(timer);
        setAnimatedCounters(targets);
      }
    }, interval);

    return () => clearInterval(timer);
  }, []);

  return (
    <div className="min-vh-100">
      {/* Hero Section */}
      <section className="position-relative overflow-hidden hero-section" style={{
        background: 'linear-gradient(135deg, #0f172a 0%, #1e293b 25%, #334155 50%, #475569 75%, #64748b 100%)',
        minHeight: '100vh',
        paddingTop: '100px'
      }}>
        {/* Enhanced Animated Background */}
        <div className="position-absolute w-100 h-100 hero-bg-pattern"></div>
        <div className="position-absolute w-100 h-100 hero-particles"></div>

        <div className="container position-relative" style={{ zIndex: 10 }}>
          <div className="row align-items-center min-vh-100 py-5">
            <div className="col-lg-6 pe-lg-5">
              <div className="mb-4">
                <div className="d-inline-flex align-items-center bg-white bg-opacity-20 backdrop-blur rounded-pill px-4 py-2 mb-4">
                  <Award className="text-warning me-2" size={18} />
                  <span className="text-white fw-semibold small">Enterprise-Grade Middleware Platform</span>
                </div>
              </div>
              
              <h1 className="display-2 fw-bold text-white mb-4 lh-1 hero-title">
                The Future of
                <span className="d-block text-gradient-gold fw-bolder">Employee Access Management</span>
              </h1>
              
              <p className="lead text-white mb-5 opacity-90 fs-4">
                CAMS is the intelligent middleware hub that automates employee provisioning across all your systems. 
                Connect to any database, API, or cloud service while maintaining enterprise security and compliance.
              </p>
              
              <div className="d-flex flex-column flex-sm-row gap-4 mb-5">
                {user ? (
                  <button 
                    className="btn btn-gradient-gold btn-lg rounded-4 fw-bold px-5 py-3 shadow-lg d-flex align-items-center cta-button"
                    onClick={() => navigate('/dashboard')}
                    style={{ fontSize: '1.1rem' }}
                  >
                    <BarChart3 className="me-2" size={22} />
                    Access Dashboard
                    <ArrowRight className="ms-2" size={22} />
                  </button>
                ) : (
                  <button 
                    className="btn btn-gradient-gold btn-lg rounded-4 fw-bold px-5 py-3 shadow-lg d-flex align-items-center cta-button"
                    onClick={() => navigate('/login')}
                    style={{ fontSize: '1.1rem' }}
                  >
                    <Zap className="me-2" size={22} />
                    Start Free Trial
                    <ArrowRight className="ms-2" size={22} />
                  </button>
                )}
                
                <button className="btn btn-outline-light btn-lg rounded-4 fw-semibold px-5 py-3 d-flex align-items-center secondary-button"
                        style={{ fontSize: '1.1rem' }}>
                  <Eye className="me-2" size={22} />
                  Watch Demo
                </button>
              </div>

              {/* Enhanced Enterprise Stats */}
              <div className="row g-4 stats-container">
                <div className="col-6 col-md-3">
                  <div className="text-center stat-item">
                    <div className="h1 fw-bold text-gradient-gold mb-1 counter-number">
                      {animatedCounters.enterprises.toLocaleString()}+
                    </div>
                    <div className="text-white opacity-90 fw-medium">Enterprises</div>
                  </div>
                </div>
                <div className="col-6 col-md-3">
                  <div className="text-center stat-item">
                    <div className="h1 fw-bold text-gradient-gold mb-1 counter-number">
                      {(animatedCounters.connections / 1000).toFixed(0)}K+
                    </div>
                    <div className="text-white opacity-90 fw-medium">Connections</div>
                  </div>
                </div>
                <div className="col-6 col-md-3">
                  <div className="text-center stat-item">
                    <div className="h1 fw-bold text-gradient-gold mb-1 counter-number">
                      {(animatedCounters.users / 1000).toFixed(0)}K+
                    </div>
                    <div className="text-white opacity-90 fw-medium">Users</div>
                  </div>
                </div>
                <div className="col-6 col-md-3">
                  <div className="text-center stat-item">
                    <div className="h1 fw-bold text-gradient-gold mb-1 counter-number">
                      {animatedCounters.uptime.toFixed(2)}%
                    </div>
                    <div className="text-white opacity-90 fw-medium">Uptime</div>
                  </div>
                </div>
              </div>
            </div>
            
            <div className="col-lg-6">
              <div className="position-relative dashboard-preview">
                {/* Enhanced Enterprise Dashboard Mockup */}
                <div className="card border-0 shadow-2xl rounded-4 overflow-hidden bg-white dashboard-card">
                  <div className="card-header bg-gradient-dark text-white p-4">
                    <div className="d-flex align-items-center justify-content-between">
                      <div className="d-flex align-items-center">
                        <Building2 className="me-2" size={20} />
                        <span className="fw-bold">Enterprise Control Center</span>
                      </div>
                      <div className="d-flex align-items-center">
                        <div className="bg-success rounded-circle me-2 live-indicator" style={{ width: '8px', height: '8px' }}></div>
                        <small className="text-success fw-medium">Live</small>
                      </div>
                    </div>
                  </div>
                  <div className="card-body p-4">
                    <div className="row g-3 mb-4">
                      <div className="col-6">
                        <div className="bg-primary bg-opacity-10 rounded-3 p-3 text-center">
                          <Database className="text-primary mb-2" size={28} />
                          <div className="fw-bold h5 mb-1 text-primary">247</div>
                          <div className="small text-muted fw-medium">Active Connections</div>
                        </div>
                      </div>
                      <div className="col-6">
                        <div className="bg-success bg-opacity-10 rounded-3 p-3 text-center">
                          <Users className="text-success mb-2" size={28} />
                          <div className="fw-bold h5 mb-1 text-success">15,249</div>
                          <div className="small text-muted fw-medium">Provisioned Users</div>
                        </div>
                      </div>
                    </div>
                    
                    <div className="mb-3">
                      <div className="d-flex justify-content-between align-items-center mb-2">
                        <span className="fw-medium text-dark">System Integration</span>
                        <span className="badge bg-success">98.7% Complete</span>
                      </div>
                      <div className="progress" style={{ height: '8px' }}>
                        <div className="progress-bar bg-gradient-success" style={{ width: '98.7%' }}></div>
                      </div>
                    </div>

                    <div className="row g-2">
                      <div className="col-4">
                        <div className="small text-center p-2 bg-light rounded">
                          <div className="fw-bold text-primary">ADP</div>
                          <CheckCircle className="text-success" size={16} />
                        </div>
                      </div>
                      <div className="col-4">
                        <div className="small text-center p-2 bg-light rounded">
                          <div className="fw-bold text-primary">Salesforce</div>
                          <CheckCircle className="text-success" size={16} />
                        </div>
                      </div>
                      <div className="col-4">
                        <div className="small text-center p-2 bg-light rounded">
                          <div className="fw-bold text-primary">Office 365</div>
                          <CheckCircle className="text-success" size={16} />
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Floating Security Badge */}
                <div className="position-absolute top-0 end-0 translate-middle">
                  <div className="bg-warning rounded-circle p-3 shadow-lg" style={{ animation: 'pulse 2s infinite' }}>
                    <Shield className="text-dark" size={24} />
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Value Proposition Section */}
      <section className="py-6 bg-white">
        <div className="container py-5">
          <div className="row justify-content-center text-center mb-5">
            <div className="col-lg-8">
              <h2 className="display-4 fw-bold text-dark mb-4">
                One Platform, Infinite Possibilities
              </h2>
              <p className="lead text-muted fs-4">
                CAMS eliminates the complexity of managing employee access across disparate systems. 
                Connect once, automate everything, and scale with confidence.
              </p>
            </div>
          </div>

          <div className="row g-5 align-items-center">
            <div className="col-lg-4">
              <div className="text-center">
                <div className="bg-primary bg-opacity-10 rounded-circle p-4 d-inline-flex mb-4">
                  <Network className="text-primary" size={48} />
                </div>
                <h4 className="fw-bold text-dark mb-3">Universal Connectivity</h4>
                <p className="text-muted">
                  Connect to any database, API, or cloud service through our intelligent middleware layer. 
                  Support for SQL Server, Oracle, PostgreSQL, MongoDB, REST APIs, and more.
                </p>
              </div>
            </div>
            <div className="col-lg-4">
              <div className="text-center">
                <div className="bg-success bg-opacity-10 rounded-circle p-4 d-inline-flex mb-4">
                  <Workflow className="text-success" size={48} />
                </div>
                <h4 className="fw-bold text-dark mb-3">Intelligent Automation</h4>
                <p className="text-muted">
                  Automate employee onboarding, access provisioning, and role management. 
                  Integrate with HR systems like ADP, Workday, and BambooHR for seamless workflows.
                </p>
              </div>
            </div>
            <div className="col-lg-4">
              <div className="text-center">
                <div className="bg-warning bg-opacity-10 rounded-circle p-4 d-inline-flex mb-4">
                  <FileCheck className="text-warning" size={48} />
                </div>
                <h4 className="fw-bold text-dark mb-3">Enterprise Compliance</h4>
                <p className="text-muted">
                  Built-in compliance features for SOC 2, GDPR, HIPAA, and more. 
                  Comprehensive audit trails, role-based permissions, and automated reporting.
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Features Grid */}
      <section className="py-6 bg-light">
        <div className="container py-5">
          <div className="text-center mb-5">
            <h2 className="display-4 fw-bold text-dark mb-4">
              Enterprise-Grade Features
            </h2>
            <p className="lead text-muted">
              Everything your organization needs to manage access at scale
            </p>
          </div>

          <div className="row g-4">
            <div className="col-lg-4 col-md-6">
              <div className="card border-0 rounded-4 shadow-sm h-100 p-4 feature-card">
                <div className="d-flex align-items-center mb-3">
                  <div className="bg-primary bg-opacity-10 rounded-3 p-2 me-3">
                    <Database className="text-primary" size={24} />
                  </div>
                  <h5 className="fw-bold mb-0">Multi-Database Support</h5>
                </div>
                <p className="text-muted mb-0">
                  Native connectors for all major databases and cloud platforms. 
                  No vendor lock-in, maximum flexibility.
                </p>
              </div>
            </div>
            
            <div className="col-lg-4 col-md-6">
              <div className="card border-0 rounded-4 shadow-sm h-100 p-4 feature-card">
                <div className="d-flex align-items-center mb-3">
                  <div className="bg-success bg-opacity-10 rounded-3 p-2 me-3">
                    <Shield className="text-success" size={24} />
                  </div>
                  <h5 className="fw-bold mb-0">Zero-Trust Security</h5>
                </div>
                <p className="text-muted mb-0">
                  Advanced encryption, multi-factor authentication, and 
                  granular access controls protect your sensitive data.
                </p>
              </div>
            </div>
            
            <div className="col-lg-4 col-md-6">
              <div className="card border-0 rounded-4 shadow-sm h-100 p-4 feature-card">
                <div className="d-flex align-items-center mb-3">
                  <div className="bg-info bg-opacity-10 rounded-3 p-2 me-3">
                    <Users className="text-info" size={24} />
                  </div>
                  <h5 className="fw-bold mb-0">Role-Based Access</h5>
                </div>
                <p className="text-muted mb-0">
                  Sophisticated RBAC system with inheritance, delegation, 
                  and time-based access controls.
                </p>
              </div>
            </div>
            
            <div className="col-lg-4 col-md-6">
              <div className="card border-0 rounded-4 shadow-sm h-100 p-4 feature-card">
                <div className="d-flex align-items-center mb-3">
                  <div className="bg-warning bg-opacity-10 rounded-3 p-2 me-3">
                    <BarChart3 className="text-warning" size={24} />
                  </div>
                  <h5 className="fw-bold mb-0">Real-Time Analytics</h5>
                </div>
                <p className="text-muted mb-0">
                  Comprehensive dashboards with usage metrics, 
                  security insights, and performance monitoring.
                </p>
              </div>
            </div>
            
            <div className="col-lg-4 col-md-6">
              <div className="card border-0 rounded-4 shadow-sm h-100 p-4 feature-card">
                <div className="d-flex align-items-center mb-3">
                  <div className="bg-danger bg-opacity-10 rounded-3 p-2 me-3">
                    <Code className="text-danger" size={24} />
                  </div>
                  <h5 className="fw-bold mb-0">API-First Design</h5>
                </div>
                <p className="text-muted mb-0">
                  RESTful APIs and SDKs for seamless integration 
                  with your existing tools and workflows.
                </p>
              </div>
            </div>
            
            <div className="col-lg-4 col-md-6">
              <div className="card border-0 rounded-4 shadow-sm h-100 p-4 feature-card">
                <div className="d-flex align-items-center mb-3">
                  <div className="bg-secondary bg-opacity-10 rounded-3 p-2 me-3">
                    <Cloud className="text-secondary" size={24} />
                  </div>
                  <h5 className="fw-bold mb-0">Cloud-Native</h5>
                </div>
                <p className="text-muted mb-0">
                  Built for the cloud with auto-scaling, high availability, 
                  and multi-region deployment options.
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Testimonials */}
      <section className="py-6 bg-dark">
        <div className="container py-5">
          <div className="text-center mb-5">
            <h2 className="display-4 fw-bold text-white mb-4">
              Trusted by Global Enterprises
            </h2>
            <p className="lead text-white opacity-75">
              See how industry leaders are transforming their access management with CAMS
            </p>
          </div>

          <div className="row g-4">
            <div className="col-lg-4">
              <div className="card border-0 rounded-4 shadow-lg h-100 testimonial-card">
                <div className="card-body p-5">
                  <div className="mb-4">
                    {[...Array(5)].map((_, i) => (
                      <Star key={i} className="text-warning me-1" size={18} fill="currentColor" />
                    ))}
                  </div>
                  <blockquote className="mb-4">
                    <p className="text-muted fst-italic">
                      "CAMS reduced our employee onboarding time from 3 days to 30 minutes. 
                      The ROI was immediate and substantial."
                    </p>
                  </blockquote>
                  <div className="d-flex align-items-center">
                    <div className="bg-primary rounded-circle p-2 me-3">
                      <Building2 className="text-white" size={20} />
                    </div>
                    <div>
                      <div className="fw-bold text-dark">Sarah Mitchell</div>
                      <div className="small text-muted">CTO, Fortune 500 Financial Services</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            
            <div className="col-lg-4">
              <div className="card border-0 rounded-4 shadow-lg h-100 testimonial-card">
                <div className="card-body p-5">
                  <div className="mb-4">
                    {[...Array(5)].map((_, i) => (
                      <Star key={i} className="text-warning me-1" size={18} fill="currentColor" />
                    ))}
                  </div>
                  <blockquote className="mb-4">
                    <p className="text-muted fst-italic">
                      "The middleware approach is brilliant. One integration gives us access 
                      to all our systems with enterprise-grade security."
                    </p>
                  </blockquote>
                  <div className="d-flex align-items-center">
                    <div className="bg-success rounded-circle p-2 me-3">
                      <Building2 className="text-white" size={20} />
                    </div>
                    <div>
                      <div className="fw-bold text-dark">Michael Chen</div>
                      <div className="small text-muted">VP Engineering, Global Healthcare</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            
            <div className="col-lg-4">
              <div className="card border-0 rounded-4 shadow-lg h-100 testimonial-card">
                <div className="card-body p-5">
                  <div className="mb-4">
                    {[...Array(5)].map((_, i) => (
                      <Star key={i} className="text-warning me-1" size={18} fill="currentColor" />
                    ))}
                  </div>
                  <blockquote className="mb-4">
                    <p className="text-muted fst-italic">
                      "CAMS enabled us to achieve SOC 2 compliance 6 months ahead of schedule. 
                      The audit trail capabilities are exceptional."
                    </p>
                  </blockquote>
                  <div className="d-flex align-items-center">
                    <div className="bg-warning rounded-circle p-2 me-3">
                      <Building2 className="text-white" size={20} />
                    </div>
                    <div>
                      <div className="fw-bold text-dark">Dr. Jennifer Park</div>
                      <div className="small text-muted">Chief Security Officer, Tech Unicorn</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Enterprise CTA */}
      <section className="py-6" style={{
        background: 'linear-gradient(135deg, #f59e0b 0%, #d97706 50%, #b45309 100%)'
      }}>
        <div className="container py-5 text-center">
          <div className="row justify-content-center">
            <div className="col-lg-8">
              <h2 className="display-4 fw-bold text-white mb-4">
                Ready to Transform Your Enterprise?
              </h2>
              <p className="lead text-white opacity-90 mb-5 fs-4">
                Join hundreds of global enterprises already using CAMS to automate access management, 
                ensure compliance, and scale securely.
              </p>
              
              <div className="d-flex flex-column flex-sm-row gap-4 justify-content-center mb-5">
                {user ? (
                  <button 
                    className="btn btn-dark btn-lg rounded-4 fw-bold px-5 py-3 shadow-lg d-flex align-items-center cta-button"
                    onClick={() => navigate('/applications')}
                    style={{ fontSize: '1.1rem' }}
                  >
                    <Settings className="me-2" size={22} />
                    Manage Applications
                    <ChevronRight className="ms-2" size={22} />
                  </button>
                ) : (
                  <button 
                    className="btn btn-dark btn-lg rounded-4 fw-bold px-5 py-3 shadow-lg d-flex align-items-center cta-button"
                    onClick={() => navigate('/login')}
                    style={{ fontSize: '1.1rem' }}
                  >
                    <Sparkles className="me-2" size={22} />
                    Start Enterprise Trial
                    <ChevronRight className="ms-2" size={22} />
                  </button>
                )}
                
                <button className="btn btn-outline-dark btn-lg rounded-4 fw-bold px-5 py-3 d-flex align-items-center secondary-button"
                        style={{ fontSize: '1.1rem' }}>
                  <Headphones className="me-2" size={22} />
                  Talk to Sales
                </button>
              </div>

              <div className="d-flex justify-content-center align-items-center text-white opacity-75">
                <CheckCircle className="me-2" size={20} />
                <span className="fw-medium">30-day free trial • No credit card required • Enterprise support included</span>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}