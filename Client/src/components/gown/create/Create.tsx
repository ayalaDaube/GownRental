import React, { useState } from 'react';
import './Create.css';
import { GownModelProps } from '../../../components/gown/gownModel/GownModel';

interface CreateProps {
    onAdd: (gown: GownModelProps) => void;
}

const Create: React.FC<CreateProps> = ({ onAdd }) => {
    const [isOpen, setIsOpen] = useState(false);
    const [form, setForm] = useState({
        gownId: 0,
        name: '',
        color: '',
        description: '',
        image: '',
        modelId: 0,
        size: 0,
        price: 0,
        isAvailable: true
    });

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setForm({ ...form, [e.target.name]: e.target.value });
    };
    
    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        onAdd({
            id: Number(form.modelId),
            name: form.name,
            color: form.color,
            description: form.description,
            image: form.image,
            details: [{
                gownId: Number(form.gownId),
                size: Number(form.size),
                modelId: Number(form.modelId),
                price: Number(form.price),
                isAvailable: form.isAvailable
            }]
        });
        setIsOpen(false);
        setForm({
            gownId: 0,
            name: '',
            color: '',
            description: '',
            image: '',
            modelId: 0,
            size: 0,
            price: 0,
            isAvailable: true
        });
    }
    return (
        <div className="gown-create-container">
            <button className="gown-create-toggle-btn" onClick={() => setIsOpen(true)}>Click to add a new gown</button>
            {isOpen && (
                <div className="gown-create-overlay" onClick={() => setIsOpen(false)}>
                    <form className="gown-create-form" onSubmit={handleSubmit} onClick={e => e.stopPropagation()}>
                        <h3 className="gown-create-title">Add New Gown</h3>
                        <div className="gown-create-fields">
                            <input type="number" name="gownId" value={form.gownId} onChange={handleChange} placeholder="Gown ID" required />
                            <input type="number" name="modelId" value={form.modelId} onChange={handleChange} placeholder="Model ID" required />
                            <input type="text" name="name" value={form.name} onChange={handleChange} placeholder="Gown Name" required />
                            <input type="text" name="color" value={form.color} onChange={handleChange} placeholder="Color (e.g. Red)" required />
                            <input type="text" name="description" value={form.description} onChange={handleChange} placeholder="Description" required />
                            <input type="number" name="size" value={form.size} onChange={handleChange} placeholder="Size (e.g. 36)" required />
                            <input type="number" name="price" value={form.price} onChange={handleChange} placeholder="Price (₪)" required />
                            <input type="text" name="image" value={form.image} onChange={handleChange} placeholder="Image URL (optional)" />
                            <label style={{fontSize: '13px', color: '#555'}}>
                                <input type="checkbox" name="isAvailable" checked={form.isAvailable} onChange={(e) => setForm({...form, isAvailable: e.target.checked})} />
                                &nbsp;Available
                            </label>
                        </div>
                        <div className="gown-create-actions">
                            <button type="submit" className="gown-create-submit-btn">Add</button>
                            <button type="button" className="gown-create-cancel-btn" onClick={() => setIsOpen(false)}>Cancel</button>
                        </div>
                    </form>
                </div>
            )}
        </div>
    );
}
export default Create;
