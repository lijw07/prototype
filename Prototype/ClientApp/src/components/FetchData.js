import React, { useState, useEffect } from 'react';

export default function FetchData() {
  const [forecasts, setForecasts] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch('api/SampleData/WeatherForecasts')
      .then(response => response.json())
      .then(data => {
        setForecasts(data);
        setLoading(false);
      });
  }, []);

  const renderForecastsTable = (forecasts) => (
    <table className="table table-striped">
      <thead>
        <tr>
          <th>Date</th>
          <th>Temp. (C)</th>
          <th>Temp. (F)</th>
          <th>Summary</th>
        </tr>
      </thead>
      <tbody>
        {forecasts.map(forecast => (
          <tr key={forecast.dateFormatted}>
            <td>{forecast.dateFormatted}</td>
            <td>{forecast.temperatureC}</td>
            <td>{forecast.temperatureF}</td>
            <td>{forecast.summary}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );

  return (
    <div>
      <h1>Weather forecast</h1>
      <p>This component demonstrates fetching data from the server.</p>
      {loading ? <p><em>Loading...</em></p> : renderForecastsTable(forecasts)}
    </div>
  );
}
