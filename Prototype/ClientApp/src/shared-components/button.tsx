// Button component
import React from "react";

const Button = ({ color, label, onClick }) => {
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