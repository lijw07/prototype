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
  Clock
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';

export default function Home() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [animatedCounters, setAnimatedCounters] = useState({
    users: 0,
    applications: 0,
    connections: 0,
    uptime: 0
  });

  // Animated counters effect
  useEffect(() => {
    const targets = { users: 3000, applications: 43, connections: 127, uptime: 99.9 };
    const duration = 2000; // 2 seconds
    const steps = 60;
    const interval = duration / steps;

    let step = 0;
    const timer = setInterval(() => {
      step++;
      const progress = step / steps;
      const easeOut = 1 - Math.pow(1 - progress, 3); // Ease-out cubic

      setAnimatedCounters({
        users: Math.floor(targets.users * easeOut),
        applications: Math.floor(targets.applications * easeOut),
        connections: Math.floor(targets.connections * easeOut),
        uptime: Math.min(targets.uptime, (targets.uptime * easeOut))
      });

      if (step >= steps) {
        clearInterval(timer);
        setAnimatedCounters(targets);
      }
    }, interval);

    return () => clearInterval(timer);
  }, []);

  const features = [
    {
      icon: Database,
      title: "Multi-Database Support",
      description: "Connect to SQL Server, MySQL, MongoDB, and more with unified access control.",
      color: "primary"
    },
    {
      icon: Shield,
      title: "Enterprise Security",
      description: "Advanced authentication, encryption, and compliance-ready audit trails.",
      color: "success"
    },
    {
      icon: Users,
      title: "Role-Based Access",
      description: "Granular permissions and role management for teams of any size.",
      color: "info"
    },
    {
      icon: TrendingUp,
      title: "Real-Time Analytics",
      description: "Comprehensive dashboards and monitoring for operational excellence.",
      color: "warning"
    }
  ];

  const testimonials = [
    {
      quote: "CAMS transformed our database access management. The security and ease of use are unmatched.",
      author: "Sarah Chen",
      role: "CTO, TechCorp",
      rating: 5
    },
    {
      quote: "Finally, a solution that scales with our enterprise needs while maintaining simplicity.",
      author: "Michael Rodriguez",
      role: "IT Director, Global Systems",
      rating: 5
    },
    {
      quote: "The audit capabilities alone saved us months of compliance work. Highly recommended.",
      author: "Dr. Emily Watson",
      role: "Security Lead, FinanceFirst",
      rating: 5
    }
  ];

  return (
    <div className="min-vh-100 bg-light">
      {/* Hero Section */}
      <section className="position-relative overflow-hidden bg-gradient-primary py-5">
        <div className="position-absolute top-0 start-0 w-100 h-100" style={{
          background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
          opacity: 0.9
        }}></div>
        
        {/* Animated Background Elements */}
        <div className="position-absolute top-0 start-0 w-100 h-100 overflow-hidden">
          <div className="position-absolute animate-float" style={{
            top: '10%', left: '10%', animation: 'float 6s ease-in-out infinite'
          }}>
            <Shield size={32} className="text-white opacity-25" />
          </div>
          <div className="position-absolute animate-float" style={{
            top: '20%', right: '15%', animation: 'float 8s ease-in-out infinite 2s'
          }}>
            <Database size={24} className="text-white opacity-25" />
          </div>
          <div className="position-absolute animate-float" style={{
            bottom: '20%', left: '20%', animation: 'float 7s ease-in-out infinite 1s'
          }}>
            <Globe size={28} className="text-white opacity-25" />
          </div>
          <div className="position-absolute animate-float" style={{
            bottom: '30%', right: '10%', animation: 'float 9s ease-in-out infinite 3s'
          }}>
            <Zap size={20} className="text-white opacity-25" />
          </div>
        </div>

        <div className="container position-relative" style={{ zIndex: 10 }}>
          <div className="row align-items-center min-vh-100 py-5">
            <div className="col-lg-6">
              <div className="mb-4">
                <span className="badge bg-white text-primary rounded-pill px-3 py-2 fw-semibold mb-3">
                  <Sparkles size={16} className="me-2" />
                  Enterprise-Grade Solution
                </span>
              </div>
              
              <h1 className="display-3 fw-bold text-white mb-4 lh-1">
                Centralized Access
                <span className="d-block text-warning">Management System</span>
              </h1>
              
              <p className="lead text-white mb-4 opacity-90">
                Secure, scalable, and intelligent database access management. 
                Streamline your operations with enterprise-grade security and 
                comprehensive audit capabilities.
              </p>
              
              <div className="d-flex flex-column flex-sm-row gap-3 mb-5">
                {user ? (
                  <button 
                    className="btn btn-warning btn-lg rounded-3 fw-semibold px-4 py-3 shadow-lg"
                    onClick={() => navigate('/dashboard')}
                  >
                    <BarChart3 className="me-2" size={20} />
                    Go to Dashboard
                    <ArrowRight className="ms-2" size={20} />
                  </button>
                ) : (
                  <button 
                    className="btn btn-warning btn-lg rounded-3 fw-semibold px-4 py-3 shadow-lg"
                    onClick={() => navigate('/login')}
                  >
                    <Shield className="me-2" size={20} />
                    Get Started
                    <ArrowRight className="ms-2" size={20} />
                  </button>
                )}
                
                <button className="btn btn-outline-light btn-lg rounded-3 fw-semibold px-4 py-3">
                  <Eye className="me-2" size={20} />
                  Watch Demo
                </button>
              </div>

              {/* Stats */}
              <div className="row g-4">
                <div className="col-6 col-md-3">
                  <div className="text-center">
                    <div className="h2 fw-bold text-warning mb-1">
                      {animatedCounters.users.toLocaleString()}+
                    </div>
                    <div className="small text-white opacity-75">Active Users</div>
                  </div>
                </div>
                <div className="col-6 col-md-3">
                  <div className="text-center">
                    <div className="h2 fw-bold text-warning mb-1">
                      {animatedCounters.applications}+
                    </div>
                    <div className="small text-white opacity-75">Applications</div>
                  </div>
                </div>
                <div className="col-6 col-md-3">
                  <div className="text-center">
                    <div className="h2 fw-bold text-warning mb-1">
                      {animatedCounters.connections}+
                    </div>
                    <div className="small text-white opacity-75">Connections</div>
                  </div>
                </div>
                <div className="col-6 col-md-3">
                  <div className="text-center">
                    <div className="h2 fw-bold text-warning mb-1">
                      {animatedCounters.uptime.toFixed(1)}%
                    </div>
                    <div className="small text-white opacity-75">Uptime</div>
                  </div>
                </div>
              </div>
            </div>
            
            <div className="col-lg-6">
              <div className="position-relative">
                {/* Floating Dashboard Preview */}
                <div className="card border-0 shadow-lg rounded-4 transform-hover" style={{
                  animation: 'fadeInUp 1s ease-out 0.5s both'
                }}>
                  <div className="card-body p-4">
                    <div className="d-flex align-items-center mb-3">
                      <div className="bg-success rounded-circle p-2 me-3">
                        <CheckCircle className="text-white" size={16} />
                      </div>
                      <div>
                        <div className="fw-semibold">System Status</div>
                        <div className="small text-muted">All systems operational</div>
                      </div>
                    </div>
                    
                    <div className="row g-3">
                      <div className="col-6">
                        <div className="bg-light rounded-3 p-3 text-center">
                          <Database className="text-primary mb-2" size={24} />
                          <div className="fw-bold h6 mb-1">43</div>
                          <div className="small text-muted">Databases</div>
                        </div>
                      </div>
                      <div className="col-6">
                        <div className="bg-light rounded-3 p-3 text-center">
                          <Users className="text-success mb-2" size={24} />
                          <div className="fw-bold h6 mb-1">3,000</div>
                          <div className="small text-muted">Users</div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Floating Security Badge */}
                <div className="position-absolute top-0 end-0 translate-middle">
                  <div className="bg-success rounded-circle p-3 shadow-lg animate-pulse-slow">
                    <Lock className="text-white" size={24} />
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-5">
        <div className="container py-5">
          <div className="text-center mb-5">
            <h2 className="display-5 fw-bold text-dark mb-3">
              Powerful Features for Modern Teams
            </h2>
            <p className="lead text-muted">
              Everything you need to manage database access securely and efficiently
            </p>
          </div>

          <div className="row g-4">
            {features.map((feature, index) => {
              const IconComponent = feature.icon;
              return (
                <div key={index} className="col-lg-3 col-md-6">
                  <div className="card border-0 rounded-4 shadow-sm h-100 feature-card position-relative overflow-hidden">
                    <div className="card-body p-4 text-center">
                      <div className={`rounded-3 p-3 bg-${feature.color} bg-opacity-10 d-inline-flex mb-3`}>
                        <IconComponent className={`text-${feature.color}`} size={32} />
                      </div>
                      <h5 className="fw-bold text-dark mb-3">{feature.title}</h5>
                      <p className="text-muted mb-0">{feature.description}</p>
                    </div>
                    <div className="position-absolute bottom-0 start-0 w-100 h-1" 
                         style={{ background: `var(--bs-${feature.color})` }}></div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* Testimonials Section */}
      <section className="py-5 bg-dark">
        <div className="container py-5">
          <div className="text-center mb-5">
            <h2 className="display-5 fw-bold text-white mb-3">
              Trusted by Industry Leaders
            </h2>
            <p className="lead text-white opacity-75">
              See what our customers are saying about CAMS
            </p>
          </div>

          <div className="row g-4">
            {testimonials.map((testimonial, index) => (
              <div key={index} className="col-lg-4">
                <div className="card border-0 rounded-4 shadow-lg h-100 bg-white">
                  <div className="card-body p-4">
                    <div className="mb-3">
                      {[...Array(testimonial.rating)].map((_, i) => (
                        <Star key={i} className="text-warning" size={16} fill="currentColor" />
                      ))}
                    </div>
                    <p className="text-muted mb-4 fst-italic">
                      "{testimonial.quote}"
                    </p>
                    <div className="d-flex align-items-center">
                      <div className="bg-primary rounded-circle p-2 me-3">
                        <Users className="text-white" size={16} />
                      </div>
                      <div>
                        <div className="fw-semibold text-dark">{testimonial.author}</div>
                        <div className="small text-muted">{testimonial.role}</div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-5 bg-gradient-warning">
        <div className="container py-5 text-center">
          <div className="row justify-content-center">
            <div className="col-lg-8">
              <h2 className="display-5 fw-bold text-dark mb-3">
                Ready to Transform Your Access Management?
              </h2>
              <p className="lead text-dark opacity-75 mb-4">
                Join thousands of organizations already using CAMS to secure their database infrastructure.
              </p>
              
              <div className="d-flex flex-column flex-sm-row gap-3 justify-content-center">
                {user ? (
                  <button 
                    className="btn btn-dark btn-lg rounded-3 fw-semibold px-4 py-3 shadow"
                    onClick={() => navigate('/applications')}
                  >
                    <Settings className="me-2" size={20} />
                    Manage Applications
                    <ChevronRight className="ms-2" size={20} />
                  </button>
                ) : (
                  <button 
                    className="btn btn-dark btn-lg rounded-3 fw-semibold px-4 py-3 shadow"
                    onClick={() => navigate('/login')}
                  >
                    <Layers className="me-2" size={20} />
                    Start Free Trial
                    <ChevronRight className="ms-2" size={20} />
                  </button>
                )}
                
                <button className="btn btn-outline-dark btn-lg rounded-3 fw-semibold px-4 py-3">
                  <Clock className="me-2" size={20} />
                  Schedule Demo
                </button>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}