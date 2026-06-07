import React, { useState } from 'react';
import './GownModel.css';
import GownModelDetails from '../gownModelDetails/GownModelDetails';
import Update from '../../../components/gown/update/Update';
import DeleteButton from '../../../components/gown/deleteButton/DeleteButton';

export interface ModelDetails {
    gownId: number;
    size: number;
    modelId: number;
    price: number;
    isAvailable: boolean;
}

export interface GownModelProps {
    id: number;
    name: string;
    color: string;
    description: string;
    image?: string;
    details: ModelDetails[];
}

function GownModel({ id, name, color, description, image, details }: GownModelProps) {
    const [isOpen, setIsOpen] = useState(false);

    return (
        <div className="gown-model-card">
            <div className="gown-model-header" onClick={() => setIsOpen(!isOpen)}>
                <div className="gown-model-field"><span>ID</span><strong>{id}</strong></div>
                <div className="gown-model-field"><span>Name</span><strong>{name}</strong></div>
                <div className="gown-model-field"><span>Color</span><strong>{color}</strong></div>
                <div className="gown-model-field"><span>Description</span><strong>{description}</strong></div>
                <div className="gown-model-actions" onClick={e => e.stopPropagation()}>
                    <Update gown={{ id, name, color, description, image, details }} />
                    <DeleteButton gownId={id} />
                </div>
                <span className="gown-model-toggle">{isOpen ? '▲' : '▼'}</span>
            </div>
            {isOpen && <GownModelDetails details={details} gownInfo={{ id, name, color, description, image }} />}
        </div>
    );
}

export default GownModel;
