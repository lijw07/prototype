import React, { useState, ChangeEvent, FormEvent } from 'react';
import Button from '../shared/button';

interface SettingsProps {}

export default function Settings(props: SettingsProps) {
  const [seconds, setSeconds] = useState(0);

const handleCreateApplication = async () => {
  try {
    const response = await fetch('/ApplicationSettings', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({})  // empty object, because your DTO has no fields
    });

    if (response.ok) {
      console.log('Success');
    } else {
      console.error('Failed:', response.status, response.statusText);
    }
  } catch (error) {
    console.error('Exception:', error);
  }
};


  return (
    <div>
      <h1>Settings</h1>
      <p>Seconds: {seconds}</p>

      <Button 
        label="Create Application"
        onClick={handleCreateApplication}
        color="blue"
        size="lg"
      />
    </div>
  );

}