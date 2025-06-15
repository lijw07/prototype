import React from "react";

interface ButtonProps {
  color: string;
  label: React.ReactNode;
  onClick?: () => void;
}

const Button: React.FC<ButtonProps> = ({ color, label, onClick }) => {
  return (
    <button
      className={`padding-2 shadow-none hover:shadow background-light-${color} hover:background-dark-${color}`}
      onClick={onClick}
    >
      {label}
    </button>
  );
};

export default Button;
