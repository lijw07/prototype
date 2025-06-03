import React from 'react';
import { useEffect, useState } from 'react';

const Home = () => {
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
      <h1>Sentinel Prototype</h1>
      <p>Seconds: {seconds}</p>
    </div>
  );
}

export default Home;