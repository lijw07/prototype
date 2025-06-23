import React from 'react';
import { useNavigate } from 'react-router-dom';
import { 
  Shield, 
  Mail, 
  Phone, 
  MapPin, 
  Linkedin, 
  Twitter, 
  Github,
  ArrowRight,
  Award,
  Lock,
  Clock,
  CheckCircle
} from 'lucide-react';

export default function WebsiteFooter() {
  const navigate = useNavigate();

  const currentYear = new Date().getFullYear();

  const footerSections = {
    solutions: [
      { name: 'Employee Access Management', path: '/services/access-management' },
      { name: 'Database Integration', path: '/services/database-integration' },
      { name: 'API Connectivity', path: '/services/api-connectivity' },
      { name: 'Compliance & Security', path: '/services/compliance' },
      { name: 'Custom Integration', path: '/services/custom-integration' }
    ],
    company: [
      { name: 'About Us', path: '/about' },
      { name: 'Careers', path: '/careers' },
      { name: 'News & Press', path: '/news' },
      { name: 'Partner Program', path: '/partners' },
      { name: 'Case Studies', path: '/case-studies' }
    ],
    resources: [
      { name: 'Documentation', path: '/docs' },
      { name: 'API Reference', path: '/api-docs' },
      { name: 'Security Center', path: '/security' },
      { name: 'Status Page', path: '/status' },
      { name: 'Support Center', path: '/support' }
    ],
    legal: [
      { name: 'Privacy Policy', path: '/privacy' },
      { name: 'Terms of Service', path: '/terms' },
      { name: 'Security Policy', path: '/security-policy' },
      { name: 'Cookie Policy', path: '/cookies' },
      { name: 'Compliance', path: '/compliance' }
    ]
  };

  const certifications = [
    { name: 'SOC 2 Type II', icon: Award },
    { name: 'ISO 27001', icon: Lock },
    { name: 'GDPR Compliant', icon: CheckCircle },
    { name: '99.99% Uptime', icon: Clock }
  ];

  return (
    <footer className="bg-dark text-white">
      {/* Main Footer Content */}
      <div className="container py-5">
        <div className="row g-5">
          {/* Company Info */}
          <div className="col-lg-4">
            <div className="mb-4">
              <div className="d-flex align-items-center mb-3">
                <Shield className="me-2 text-warning" size={32} />
                <span className="h4 fw-bold text-white mb-0">CAMS</span>
              </div>
              <p className="text-light opacity-75 mb-4">
                The intelligent middleware platform that automates employee access management 
                across all your systems. Trusted by Fortune 500 companies worldwide.
              </p>
            </div>

            {/* Contact Info */}
            <div className="mb-4">
              <h6 className="fw-bold text-warning mb-3">Get in Touch</h6>
              <div className="d-flex flex-column gap-2">
                <div className="d-flex align-items-center">
                  <Phone size={16} className="me-2 text-warning" />
                  <a href="tel:+1-800-CAMS-247" className="text-light text-decoration-none">
                    +1 (800) CAMS-247
                  </a>
                </div>
                <div className="d-flex align-items-center">
                  <Mail size={16} className="me-2 text-warning" />
                  <a href="mailto:contact@cams.com" className="text-light text-decoration-none">
                    contact@cams.com
                  </a>
                </div>
                <div className="d-flex align-items-center">
                  <MapPin size={16} className="me-2 text-warning" />
                  <span className="text-light">San Francisco, CA</span>
                </div>
              </div>
            </div>

            {/* Social Media */}
            <div>
              <h6 className="fw-bold text-warning mb-3">Follow Us</h6>
              <div className="d-flex gap-3">
                <a href="https://linkedin.com/company/cams" className="text-light">
                  <Linkedin size={20} />
                </a>
                <a href="https://twitter.com/cams" className="text-light">
                  <Twitter size={20} />
                </a>
                <a href="https://github.com/cams" className="text-light">
                  <Github size={20} />
                </a>
              </div>
            </div>
          </div>

          {/* Navigation Links */}
          <div className="col-lg-8">
            <div className="row g-4">
              {/* Solutions */}
              <div className="col-md-3">
                <h6 className="fw-bold text-warning mb-3">Solutions</h6>
                <ul className="list-unstyled">
                  {footerSections.solutions.map((item) => (
                    <li key={item.name} className="mb-2">
                      <button
                        className="btn p-0 text-light text-decoration-none border-0 bg-transparent text-start"
                        onClick={() => navigate(item.path)}
                      >
                        {item.name}
                      </button>
                    </li>
                  ))}
                </ul>
              </div>

              {/* Company */}
              <div className="col-md-3">
                <h6 className="fw-bold text-warning mb-3">Company</h6>
                <ul className="list-unstyled">
                  {footerSections.company.map((item) => (
                    <li key={item.name} className="mb-2">
                      <button
                        className="btn p-0 text-light text-decoration-none border-0 bg-transparent text-start"
                        onClick={() => navigate(item.path)}
                      >
                        {item.name}
                      </button>
                    </li>
                  ))}
                </ul>
              </div>

              {/* Resources */}
              <div className="col-md-3">
                <h6 className="fw-bold text-warning mb-3">Resources</h6>
                <ul className="list-unstyled">
                  {footerSections.resources.map((item) => (
                    <li key={item.name} className="mb-2">
                      <button
                        className="btn p-0 text-light text-decoration-none border-0 bg-transparent text-start"
                        onClick={() => navigate(item.path)}
                      >
                        {item.name}
                      </button>
                    </li>
                  ))}
                </ul>
              </div>

              {/* Legal */}
              <div className="col-md-3">
                <h6 className="fw-bold text-warning mb-3">Legal</h6>
                <ul className="list-unstyled">
                  {footerSections.legal.map((item) => (
                    <li key={item.name} className="mb-2">
                      <button
                        className="btn p-0 text-light text-decoration-none border-0 bg-transparent text-start"
                        onClick={() => navigate(item.path)}
                      >
                        {item.name}
                      </button>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          </div>
        </div>

        {/* Newsletter Signup */}
        <div className="row mt-5 pt-4 border-top border-secondary">
          <div className="col-lg-6">
            <h6 className="fw-bold text-warning mb-3">Stay Updated</h6>
            <p className="text-light opacity-75 mb-3">
              Get the latest updates on new features, security enhancements, and industry insights.
            </p>
            <div className="d-flex gap-2">
              <input 
                type="email" 
                className="form-control bg-transparent border-secondary text-light" 
                placeholder="Enter your email"
              />
              <button className="btn btn-warning px-4 d-flex align-items-center">
                Subscribe
                <ArrowRight size={16} className="ms-2" />
              </button>
            </div>
          </div>
          
          {/* Certifications */}
          <div className="col-lg-6">
            <h6 className="fw-bold text-warning mb-3">Security & Compliance</h6>
            <div className="row g-3">
              {certifications.map((cert) => {
                const IconComponent = cert.icon;
                return (
                  <div key={cert.name} className="col-6">
                    <div className="d-flex align-items-center">
                      <div className="bg-warning bg-opacity-20 rounded p-2 me-2">
                        <IconComponent size={16} className="text-warning" />
                      </div>
                      <span className="small text-light">{cert.name}</span>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      </div>

      {/* Bottom Bar */}
      <div className="border-top border-secondary">
        <div className="container py-4">
          <div className="row align-items-center">
            <div className="col-md-6">
              <p className="mb-0 text-light opacity-75">
                © {currentYear} CAMS. All rights reserved. | 
                <button 
                  className="btn p-0 text-light text-decoration-none border-0 bg-transparent ms-1"
                  onClick={() => navigate('/privacy')}
                >
                  Privacy Policy
                </button> | 
                <button 
                  className="btn p-0 text-light text-decoration-none border-0 bg-transparent ms-1"
                  onClick={() => navigate('/terms')}
                >
                  Terms of Service
                </button>
              </p>
            </div>
            <div className="col-md-6 text-md-end">
              <p className="mb-0 text-light opacity-75">
                Made with ❤️ for enterprise security
              </p>
            </div>
          </div>
        </div>
      </div>
    </footer>
  );
}