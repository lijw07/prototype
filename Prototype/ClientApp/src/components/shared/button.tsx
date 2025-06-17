// Button.tsx
import React from 'react';

interface ButtonProps {
  label: string;
  onClick: () => void;
  color?: string;
  size?: 'sm' | 'md' | 'lg';
}

const Button: React.FC<ButtonProps> = ({ label, onClick, color = 'blue', size = 'md' }) => {
  return (
    <button
      onClick={onClick}
      style={{
        backgroundColor: color,
        padding: size === 'lg' ? '1rem 2rem' : '0.5rem 1rem',
        border: 'none',
        borderRadius: '5px',
        color: 'white',
        cursor: 'pointer'
      }}
    >
      {label}
    </button>
  );
};

export default Button;
