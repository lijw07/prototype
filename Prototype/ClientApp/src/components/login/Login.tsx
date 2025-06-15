import React, { Component, ChangeEvent, FormEvent } from 'react';

// ✅ Define state interface
interface LoginFormState {
  username: string;
  password: string;
  error: string;
}

// ✅ Define props interface (empty for now)
interface LoginFormProps {}

class LoginForm extends Component<LoginFormProps, LoginFormState> {
  constructor(props: LoginFormProps) {
    super(props);
    this.state = {
      username: '',
      password: '',
      error: '',
    };
  }

  handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    this.setState({ [e.target.name]: e.target.value } as Pick<LoginFormState, keyof LoginFormState>);
  };

  handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const { username, password } = this.state;

    try {
      const response = await fetch('/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ username, password }),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Login failed');
      }

      const data = await response.json();
      console.log('Login success, token:', data.token);

      localStorage.setItem('authToken', data.token);
    } catch (error: unknown) {
      const errorMsg =
        error instanceof Error ? error.message : 'Unknown error occurred';
      console.error('Login failed:', errorMsg);
      this.setState({ error: errorMsg });
    }
  };

  render() {
    const { username, password, error } = this.state;

    return (
      <form onSubmit={this.handleSubmit}>
        <div className="mb-3">
          <label>Username</label>
          <input
            name="username"
            type="text"
            className="form-control"
            placeholder="Enter username"
            value={username}
            onChange={this.handleChange}
          />
        </div>

        <div className="mb-3">
          <label>Password</label>
          <input
            name="password"
            type="password"
            className="form-control"
            placeholder="Enter password"
            value={password}
            onChange={this.handleChange}
          />
        </div>

        {error && <div className="alert alert-danger">{error}</div>}

        <div className="d-grid">
          <button type="submit" className="btn btn-primary">
            Submit
          </button>
        </div>
      </form>
    );
  }
}

export default LoginForm;
