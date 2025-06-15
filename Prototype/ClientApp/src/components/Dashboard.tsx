import React from 'react';
import { useEffect, useState } from 'react';

export default function Dashboard() {
   const [seconds, setSeconds] = useState(0);

useEffect(() => {
    const intervalId = setInterval(() => {
      setSeconds((prevSeconds) => prevSeconds + 1);
    }, 1000);

    // Cleanup function to clear the interval when the component unmounts
    return () => clearInterval(intervalId);
  }, []);

  return (
    <div>
      <h1>CAMS Home Page</h1>
      <p>Seconds: {seconds}</p>
    </div>
  );
}

