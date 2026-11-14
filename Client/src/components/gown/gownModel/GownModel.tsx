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
    /** The gown is in the inventory (not retired or damaged). */
    isActive: boolean;
    /** The gown can be rented: in the inventory and, when a date is chosen, not rented around it. */
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
                <div className="gown-model-top-row">
                    <div className="gown-model-info">
                        <div className="gown-model-id">#{id}</div>
                        <div className="gown-model-name">{name}</div>
                        <div className="gown-model-meta">
                            <span className="gown-model-tag">{color}</span>
                            <span className="gown-model-tag">{description}</span>
                        </div>
                    </div>
                    <div className="gown-model-actions" onClick={e => e.stopPropagation()}>
                        <Update gown={{ id, name, color, description, image, details }} />
                        <DeleteButton gownId={id} />
                    </div>
                </div>
                <div className="gown-model-footer">
                    <span className="gown-model-toggle">
                        {isOpen ? '▲ hide' : '▼ view sizes'}
                    </span>
                </div>
            </div>
            {isOpen && <GownModelDetails details={details} gownInfo={{ id, name, color, description, image }} />}
        </div>
    );
}

export default GownModel;
