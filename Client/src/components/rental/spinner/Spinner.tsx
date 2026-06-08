import React from 'react';
import './Spinner.css';

const Spinner: React.FC = () => {
    return (
        <div className="spinner-wrapper">
            <div className="spinner"></div>
        </div>
    );
};

export default Spinner;