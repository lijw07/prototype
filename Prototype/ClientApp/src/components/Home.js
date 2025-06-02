import React from 'react';

export default function Home() {
  return (
    <div>
      <h1>Hello, world!</h1>
      <p>Welcome to your new single-page application, built with:</p>
      <ul>
        <li>
          <a href="https://dotnet.microsoft.com/apps/aspnet" target="_blank" rel="noopener noreferrer">
            ASP.NET Core
          </a>{' '}
          and{' '}
          <a href="https://learn.microsoft.com/en-us/dotnet/csharp/" target="_blank" rel="noopener noreferrer">
            C#
          </a>{' '}
          for cross-platform server-side code
        </li>
        <li>
          <a href="https://reactjs.org/" target="_blank" rel="noopener noreferrer">
            React
          </a>{' '}
          for client-side code
        </li>
        <li>
          <a href="https://getbootstrap.com/" target="_blank" rel="noopener noreferrer">
            Bootstrap
          </a>{' '}
          for layout and styling
        </li>
      </ul>

      <p>To help you get started, we have also set up:</p>
      <ul>
        <li><strong>Client-side navigation</strong>. For example, click <em>Counter</em> then <em>Back</em> to return here.</li>
        <li><strong>Development server integration</strong>. In development mode, the CRA dev server runs automatically in the background for fast feedback.</li>
        <li><strong>Efficient production builds</strong>. In production mode, your app is built and bundled via <code>dotnet publish</code> and <code>npm run build</code>.</li>
      </ul>

      <p>
        The <code>ClientApp</code> subdirectory is a standard React app built with{' '}
        <a href="https://create-react-app.dev/" target="_blank" rel="noopener noreferrer">
          Create React App
        </a>
        . You can run <code>npm test</code>, <code>npm install</code>, and more from that directory.
      </p>
    </div>
  );
}
