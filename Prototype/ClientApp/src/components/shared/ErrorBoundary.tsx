import React, { Component, ErrorInfo, ReactNode } from 'react';
import { AlertTriangle, RefreshCw } from 'lucide-react';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null, errorInfo: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error, errorInfo: null };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('ErrorBoundary caught an error:', error, errorInfo);
    this.setState({
      error,
      errorInfo
    });
  }

  handleReset = () => {
    this.setState({ hasError: false, error: null, errorInfo: null });
  };

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return <>{this.props.fallback}</>;
      }

      return (
        <div className="container py-5">
          <div className="row justify-content-center">
            <div className="col-md-6">
              <div className="card border-0 shadow-sm">
                <div className="card-body text-center py-5">
                  <AlertTriangle size={48} className="text-warning mb-3" />
                  <h4 className="mb-3">Oops! Something went wrong</h4>
                  <p className="text-muted mb-4">
                    We encountered an unexpected error. Please try refreshing the page.
                  </p>
                  {process.env.NODE_ENV === 'development' && this.state.error && (
                    <div className="alert alert-danger text-start mb-4">
                      <small>
                        <strong>{this.state.error.toString()}</strong>
                        {this.state.errorInfo && (
                          <pre className="mt-2 mb-0">{this.state.errorInfo.componentStack}</pre>
                        )}
                      </small>
                    </div>
                  )}
                  <button 
                    onClick={() => window.location.reload()} 
                    className="btn btn-primary"
                  >
                    <RefreshCw size={16} className="me-2" />
                    Refresh Page
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;