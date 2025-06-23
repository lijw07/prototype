import React from 'react';
import { useNavigate } from 'react-router-dom';
import { 
  Shield, 
  Target, 
  Users, 
  Award, 
  ArrowRight,
  CheckCircle,
  Building2,
  Globe,
  TrendingUp,
  Heart,
  Lightbulb,
  Lock
} from 'lucide-react';

export default function About() {
  const navigate = useNavigate();

  const values = [
    {
      icon: Lock,
      title: "Security First",
      description: "We believe security should never be an afterthought. Every feature is built with enterprise-grade security at its core."
    },
    {
      icon: Users,
      title: "Customer Success",
      description: "Our customers' success is our success. We're committed to providing exceptional support and continuous innovation."
    },
    {
      icon: Lightbulb,
      title: "Innovation",
      description: "We constantly push the boundaries of what's possible in access management and automation technology."
    },
    {
      icon: Heart,
      title: "Integrity",
      description: "We operate with transparency, honesty, and ethical practices in everything we do."
    }
  ];

  const stats = [
    { number: "500+", label: "Enterprise Customers" },
    { number: "50M+", label: "Access Requests Processed" },
    { number: "99.99%", label: "Platform Uptime" },
    { number: "24/7", label: "Global Support" }
  ];

  const timeline = [
    {
      year: "2019",
      title: "Company Founded",
      description: "CAMS was founded with a vision to simplify enterprise access management."
    },
    {
      year: "2020",
      title: "First Enterprise Client",
      description: "Secured our first Fortune 500 customer and processed over 1M access requests."
    },
    {
      year: "2021",
      title: "Series A Funding",
      description: "Raised $25M Series A to accelerate product development and market expansion."
    },
    {
      year: "2022",
      title: "Global Expansion",
      description: "Expanded operations to Europe and Asia, serving customers in 50+ countries."
    },
    {
      year: "2023",
      title: "AI-Powered Features",
      description: "Launched intelligent automation and predictive access management capabilities."
    },
    {
      year: "2024",
      title: "Industry Leadership",
      description: "Recognized as a leader in enterprise access management by major analyst firms."
    }
  ];

  const team = [
    {
      name: "Sarah Chen",
      role: "CEO & Co-Founder",
      background: "Former VP of Engineering at Microsoft, 15+ years in enterprise security"
    },
    {
      name: "Michael Rodriguez",
      role: "CTO & Co-Founder",
      background: "Ex-Principal Engineer at Amazon, PhD in Computer Science from Stanford"
    },
    {
      name: "Dr. Jennifer Park",
      role: "Chief Security Officer",
      background: "Former CISO at Goldman Sachs, expert in financial services compliance"
    },
    {
      name: "David Kim",
      role: "VP of Product",
      background: "Product leader from Salesforce, specialized in enterprise SaaS platforms"
    }
  ];

  return (
    <div className="min-vh-100">
      {/* Hero Section */}
      <section className="py-6 bg-primary text-white">
        <div className="container py-5">
          <div className="row align-items-center">
            <div className="col-lg-6">
              <h1 className="display-4 fw-bold mb-4">
                Transforming Enterprise Access Management
              </h1>
              <p className="lead mb-4 opacity-90">
                At CAMS, we're on a mission to make enterprise access management 
                simple, secure, and scalable for organizations worldwide.
              </p>
              <div className="d-flex align-items-center gap-4">
                <button 
                  className="btn btn-warning btn-lg rounded-3 fw-bold px-4 py-3 d-flex align-items-center"
                  onClick={() => navigate('/contact')}
                >
                  Get in Touch
                  <ArrowRight className="ms-2" size={20} />
                </button>
                <button 
                  className="btn btn-outline-light btn-lg rounded-3 fw-semibold px-4 py-3"
                  onClick={() => navigate('/careers')}
                >
                  Join Our Team
                </button>
              </div>
            </div>
            <div className="col-lg-6">
              <div className="position-relative">
                <div className="bg-white bg-opacity-10 rounded-4 p-5 backdrop-blur">
                  <div className="row g-4">
                    {stats.map((stat, index) => (
                      <div key={index} className="col-6 text-center">
                        <div className="h2 fw-bold text-warning mb-1">{stat.number}</div>
                        <div className="small text-white opacity-75">{stat.label}</div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Mission & Vision */}
      <section className="py-6 bg-light">
        <div className="container py-5">
          <div className="row g-5">
            <div className="col-lg-6">
              <div className="d-flex align-items-center mb-4">
                <Target className="text-primary me-3" size={32} />
                <h2 className="h3 fw-bold mb-0">Our Mission</h2>
              </div>
              <p className="text-muted mb-4">
                To democratize enterprise-grade access management by providing intelligent, 
                automated solutions that scale with organizations of any size. We believe 
                every company should have access to the same level of security and efficiency 
                that Fortune 500 companies enjoy.
              </p>
              <ul className="list-unstyled">
                <li className="d-flex align-items-center mb-2">
                  <CheckCircle className="text-success me-2" size={16} />
                  <span>Simplify complex access management workflows</span>
                </li>
                <li className="d-flex align-items-center mb-2">
                  <CheckCircle className="text-success me-2" size={16} />
                  <span>Ensure enterprise-grade security for all</span>
                </li>
                <li className="d-flex align-items-center mb-2">
                  <CheckCircle className="text-success me-2" size={16} />
                  <span>Enable seamless integration across platforms</span>
                </li>
              </ul>
            </div>
            <div className="col-lg-6">
              <div className="d-flex align-items-center mb-4">
                <Globe className="text-primary me-3" size={32} />
                <h2 className="h3 fw-bold mb-0">Our Vision</h2>
              </div>
              <p className="text-muted mb-4">
                A world where access management is invisible, intelligent, and instantaneous. 
                Where employees can focus on their work without security barriers, and IT teams 
                can sleep peacefully knowing their systems are secure and compliant.
              </p>
              <div className="bg-white rounded-4 p-4 shadow-sm">
                <h5 className="fw-bold text-primary mb-3">By 2030, we envision:</h5>
                <ul className="list-unstyled mb-0">
                  <li className="d-flex align-items-center mb-2">
                    <Building2 className="text-warning me-2" size={16} />
                    <span>10,000+ organizations using CAMS</span>
                  </li>
                  <li className="d-flex align-items-center mb-2">
                    <TrendingUp className="text-warning me-2" size={16} />
                    <span>Zero-touch access provisioning</span>
                  </li>
                  <li className="d-flex align-items-center">
                    <Award className="text-warning me-2" size={16} />
                    <span>Industry standard for access management</span>
                  </li>
                </ul>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Values */}
      <section className="py-6 bg-white">
        <div className="container py-5">
          <div className="text-center mb-5">
            <h2 className="display-5 fw-bold text-dark mb-4">Our Core Values</h2>
            <p className="lead text-muted">
              These principles guide everything we do, from product development to customer support.
            </p>
          </div>
          <div className="row g-4">
            {values.map((value, index) => {
              const IconComponent = value.icon;
              return (
                <div key={index} className="col-lg-3 col-md-6">
                  <div className="text-center h-100">
                    <div className="bg-primary bg-opacity-10 rounded-circle p-4 d-inline-flex mb-4">
                      <IconComponent className="text-primary" size={32} />
                    </div>
                    <h5 className="fw-bold text-dark mb-3">{value.title}</h5>
                    <p className="text-muted">{value.description}</p>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* Timeline */}
      <section className="py-6 bg-dark text-white">
        <div className="container py-5">
          <div className="text-center mb-5">
            <h2 className="display-5 fw-bold mb-4">Our Journey</h2>
            <p className="lead opacity-75">
              From startup to industry leader - here's how we've grown over the years.
            </p>
          </div>
          <div className="row justify-content-center">
            <div className="col-lg-10">
              <div className="position-relative">
                {timeline.map((item, index) => (
                  <div key={index} className="d-flex mb-5 position-relative">
                    <div className="flex-shrink-0 me-4">
                      <div className="bg-warning rounded-circle p-3 d-flex align-items-center justify-content-center">
                        <span className="fw-bold text-dark">{item.year}</span>
                      </div>
                    </div>
                    <div className="flex-grow-1">
                      <h5 className="fw-bold text-warning mb-2">{item.title}</h5>
                      <p className="text-light opacity-75 mb-0">{item.description}</p>
                    </div>
                    {index < timeline.length - 1 && (
                      <div 
                        className="position-absolute border-start border-warning opacity-50"
                        style={{ 
                          left: '2rem', 
                          top: '4rem', 
                          height: '100%',
                          width: '2px'
                        }}
                      ></div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Leadership Team */}
      <section className="py-6 bg-light">
        <div className="container py-5">
          <div className="text-center mb-5">
            <h2 className="display-5 fw-bold text-dark mb-4">Leadership Team</h2>
            <p className="lead text-muted">
              Meet the experienced leaders driving CAMS forward.
            </p>
          </div>
          <div className="row g-4">
            {team.map((member, index) => (
              <div key={index} className="col-lg-3 col-md-6">
                <div className="card border-0 rounded-4 shadow-sm h-100">
                  <div className="card-body p-4 text-center">
                    <div className="bg-primary bg-opacity-10 rounded-circle p-4 d-inline-flex mb-3">
                      <Users className="text-primary" size={32} />
                    </div>
                    <h5 className="fw-bold text-dark mb-2">{member.name}</h5>
                    <div className="text-primary fw-semibold mb-3">{member.role}</div>
                    <p className="text-muted small mb-0">{member.background}</p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-6 bg-warning">
        <div className="container py-5 text-center">
          <div className="row justify-content-center">
            <div className="col-lg-8">
              <h2 className="display-5 fw-bold text-dark mb-4">
                Ready to Join Our Mission?
              </h2>
              <p className="lead text-dark opacity-75 mb-5">
                Whether you're looking for a career opportunity or want to transform 
                your organization's access management, we'd love to hear from you.
              </p>
              <div className="d-flex flex-column flex-sm-row gap-4 justify-content-center">
                <button 
                  className="btn btn-dark btn-lg rounded-3 fw-bold px-5 py-3 d-flex align-items-center"
                  onClick={() => navigate('/careers')}
                >
                  <Users className="me-2" size={20} />
                  View Open Positions
                  <ArrowRight className="ms-2" size={20} />
                </button>
                <button 
                  className="btn btn-outline-dark btn-lg rounded-3 fw-bold px-5 py-3 d-flex align-items-center"
                  onClick={() => navigate('/contact')}
                >
                  <Shield className="me-2" size={20} />
                  Contact Sales
                </button>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}