import React, { useState } from 'react';
import { 
  Mail, 
  Phone, 
  MapPin, 
  Clock, 
  Send,
  CheckCircle,
  Building2,
  Users,
  Code,
  HeadphonesIcon,
  MessageSquare,
  Calendar
} from 'lucide-react';

export default function Contact() {
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    company: '',
    phone: '',
    inquiryType: '',
    message: ''
  });

  const [isSubmitted, setIsSubmitted] = useState(false);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    // Handle form submission here
    console.log('Form submitted:', formData);
    setIsSubmitted(true);
  };

  const contactMethods = [
    {
      icon: Phone,
      title: "Sales",
      description: "Speak with our sales team about enterprise solutions",
      contact: "+1 (800) CAMS-247",
      action: "tel:+1-800-CAMS-247",
      availability: "Mon-Fri 9AM-6PM PST"
    },
    {
      icon: HeadphonesIcon,
      title: "Support",
      description: "Get technical support for existing customers",
      contact: "support@cams.com",
      action: "mailto:support@cams.com",
      availability: "24/7 Global Support"
    },
    {
      icon: Building2,
      title: "Partnerships",
      description: "Explore partnership and integration opportunities",
      contact: "partners@cams.com",
      action: "mailto:partners@cams.com",
      availability: "Mon-Fri 9AM-5PM PST"
    },
    {
      icon: MessageSquare,
      title: "General Inquiries",
      description: "Questions about our company or solutions",
      contact: "contact@cams.com",
      action: "mailto:contact@cams.com",
      availability: "Response within 24 hours"
    }
  ];

  const offices = [
    {
      city: "San Francisco",
      address: "123 Market Street, Suite 400",
      zipCode: "San Francisco, CA 94105",
      phone: "+1 (415) 555-0100",
      type: "Headquarters"
    },
    {
      city: "New York",
      address: "456 Fifth Avenue, Floor 20",
      zipCode: "New York, NY 10018",
      phone: "+1 (212) 555-0200",
      type: "East Coast Office"
    },
    {
      city: "London",
      address: "789 King's Road",
      zipCode: "London SW3 4NX, UK",
      phone: "+44 20 7946 0958",
      type: "EMEA Headquarters"
    }
  ];

  const inquiryTypes = [
    "Sales Inquiry",
    "Technical Support",
    "Partnership",
    "Demo Request",
    "General Question",
    "Press/Media",
    "Careers"
  ];

  if (isSubmitted) {
    return (
      <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
        <div className="container">
          <div className="row justify-content-center">
            <div className="col-lg-6 text-center">
              <div className="bg-white rounded-4 shadow-sm p-5">
                <div className="bg-success bg-opacity-10 rounded-circle p-4 d-inline-flex mb-4">
                  <CheckCircle className="text-success" size={48} />
                </div>
                <h2 className="fw-bold text-dark mb-3">Thank You!</h2>
                <p className="text-muted mb-4">
                  We've received your message and will get back to you within 24 hours. 
                  For urgent matters, please call our sales team directly.
                </p>
                <button 
                  className="btn btn-primary rounded-3 px-4 py-2"
                  onClick={() => setIsSubmitted(false)}
                >
                  Send Another Message
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-vh-100">
      {/* Hero Section */}
      <section className="py-6 bg-primary text-white">
        <div className="container py-5">
          <div className="row align-items-center">
            <div className="col-lg-6">
              <h1 className="display-4 fw-bold mb-4">
                Get in Touch
              </h1>
              <p className="lead mb-4 opacity-90">
                Ready to transform your access management? Our team of experts is here to help 
                you find the perfect solution for your organization.
              </p>
              <div className="d-flex align-items-center gap-4">
                <div className="d-flex align-items-center">
                  <Clock className="me-2" size={20} />
                  <span>24/7 Support Available</span>
                </div>
                <div className="d-flex align-items-center">
                  <CheckCircle className="me-2" size={20} />
                  <span>Free Consultation</span>
                </div>
              </div>
            </div>
            <div className="col-lg-6">
              <div className="bg-white bg-opacity-10 rounded-4 p-4 backdrop-blur">
                <h5 className="fw-bold mb-3">Quick Contact</h5>
                <div className="d-flex align-items-center mb-3">
                  <Phone className="me-3 text-warning" size={20} />
                  <div>
                    <div className="fw-semibold">Sales Hotline</div>
                    <a href="tel:+1-800-CAMS-247" className="text-light text-decoration-none">
                      +1 (800) CAMS-247
                    </a>
                  </div>
                </div>
                <div className="d-flex align-items-center">
                  <Mail className="me-3 text-warning" size={20} />
                  <div>
                    <div className="fw-semibold">Email Sales</div>
                    <a href="mailto:sales@cams.com" className="text-light text-decoration-none">
                      sales@cams.com
                    </a>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Contact Methods */}
      <section className="py-6 bg-light">
        <div className="container py-5">
          <div className="text-center mb-5">
            <h2 className="display-5 fw-bold text-dark mb-4">How Can We Help?</h2>
            <p className="lead text-muted">
              Choose the best way to reach us based on your needs.
            </p>
          </div>
          <div className="row g-4">
            {contactMethods.map((method, index) => {
              const IconComponent = method.icon;
              return (
                <div key={index} className="col-lg-3 col-md-6">
                  <div className="card border-0 rounded-4 shadow-sm h-100 p-4">
                    <div className="text-center">
                      <div className="bg-primary bg-opacity-10 rounded-circle p-3 d-inline-flex mb-3">
                        <IconComponent className="text-primary" size={24} />
                      </div>
                      <h5 className="fw-bold text-dark mb-2">{method.title}</h5>
                      <p className="text-muted mb-3">{method.description}</p>
                      <a 
                        href={method.action}
                        className="btn btn-outline-primary rounded-3 mb-3 d-block"
                      >
                        {method.contact}
                      </a>
                      <div className="small text-muted">{method.availability}</div>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* Contact Form */}
      <section className="py-6 bg-white">
        <div className="container py-5">
          <div className="row justify-content-center">
            <div className="col-lg-8">
              <div className="text-center mb-5">
                <h2 className="display-5 fw-bold text-dark mb-4">Send Us a Message</h2>
                <p className="lead text-muted">
                  Fill out the form below and we'll get back to you within 24 hours.
                </p>
              </div>
              
              <div className="card border-0 rounded-4 shadow-sm">
                <div className="card-body p-5">
                  <form onSubmit={handleSubmit}>
                    <div className="row g-4">
                      <div className="col-md-6">
                        <label htmlFor="firstName" className="form-label fw-semibold">
                          First Name *
                        </label>
                        <input
                          type="text"
                          className="form-control rounded-3 py-3"
                          id="firstName"
                          name="firstName"
                          value={formData.firstName}
                          onChange={handleInputChange}
                          required
                        />
                      </div>
                      <div className="col-md-6">
                        <label htmlFor="lastName" className="form-label fw-semibold">
                          Last Name *
                        </label>
                        <input
                          type="text"
                          className="form-control rounded-3 py-3"
                          id="lastName"
                          name="lastName"
                          value={formData.lastName}
                          onChange={handleInputChange}
                          required
                        />
                      </div>
                      <div className="col-md-6">
                        <label htmlFor="email" className="form-label fw-semibold">
                          Email Address *
                        </label>
                        <input
                          type="email"
                          className="form-control rounded-3 py-3"
                          id="email"
                          name="email"
                          value={formData.email}
                          onChange={handleInputChange}
                          required
                        />
                      </div>
                      <div className="col-md-6">
                        <label htmlFor="phone" className="form-label fw-semibold">
                          Phone Number
                        </label>
                        <input
                          type="tel"
                          className="form-control rounded-3 py-3"
                          id="phone"
                          name="phone"
                          value={formData.phone}
                          onChange={handleInputChange}
                        />
                      </div>
                      <div className="col-md-6">
                        <label htmlFor="company" className="form-label fw-semibold">
                          Company *
                        </label>
                        <input
                          type="text"
                          className="form-control rounded-3 py-3"
                          id="company"
                          name="company"
                          value={formData.company}
                          onChange={handleInputChange}
                          required
                        />
                      </div>
                      <div className="col-md-6">
                        <label htmlFor="inquiryType" className="form-label fw-semibold">
                          Inquiry Type *
                        </label>
                        <select
                          className="form-select rounded-3 py-3"
                          id="inquiryType"
                          name="inquiryType"
                          value={formData.inquiryType}
                          onChange={handleInputChange}
                          required
                        >
                          <option value="">Select an option</option>
                          {inquiryTypes.map((type) => (
                            <option key={type} value={type}>{type}</option>
                          ))}
                        </select>
                      </div>
                      <div className="col-12">
                        <label htmlFor="message" className="form-label fw-semibold">
                          Message *
                        </label>
                        <textarea
                          className="form-control rounded-3"
                          id="message"
                          name="message"
                          rows={5}
                          value={formData.message}
                          onChange={handleInputChange}
                          placeholder="Tell us about your access management needs..."
                          required
                        ></textarea>
                      </div>
                      <div className="col-12">
                        <button
                          type="submit"
                          className="btn btn-primary btn-lg rounded-3 fw-bold px-5 py-3 d-flex align-items-center"
                        >
                          <Send className="me-2" size={20} />
                          Send Message
                        </button>
                      </div>
                    </div>
                  </form>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Office Locations */}
      <section className="py-6 bg-dark text-white">
        <div className="container py-5">
          <div className="text-center mb-5">
            <h2 className="display-5 fw-bold mb-4">Our Offices</h2>
            <p className="lead opacity-75">
              Visit us at one of our global offices or schedule a virtual meeting.
            </p>
          </div>
          <div className="row g-4">
            {offices.map((office, index) => (
              <div key={index} className="col-lg-4">
                <div className="card bg-transparent border-secondary rounded-4 h-100">
                  <div className="card-body p-4">
                    <div className="d-flex align-items-center mb-3">
                      <MapPin className="text-warning me-2" size={20} />
                      <h5 className="fw-bold text-white mb-0">{office.city}</h5>
                      <span className="badge bg-warning text-dark ms-auto">{office.type}</span>
                    </div>
                    <p className="text-light opacity-75 mb-3">
                      {office.address}<br />
                      {office.zipCode}
                    </p>
                    <div className="d-flex align-items-center">
                      <Phone className="text-warning me-2" size={16} />
                      <a href={`tel:${office.phone}`} className="text-light text-decoration-none">
                        {office.phone}
                      </a>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
          
          <div className="text-center mt-5">
            <button className="btn btn-warning btn-lg rounded-3 fw-bold px-5 py-3 d-flex align-items-center mx-auto">
              <Calendar className="me-2" size={20} />
              Schedule a Meeting
            </button>
          </div>
        </div>
      </section>
    </div>
  );
}