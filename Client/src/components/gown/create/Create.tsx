import React, { useState } from 'react';
import './Create.css';
import { NewGownInput } from '../../../services/Gowns/task.service';

interface CreateProps {
    // Resolves to true when the gown was saved.
    onAdd: (gown: NewGownInput) => Promise<boolean>;
}

const emptyForm = { name: '', color: '', description: '', image: '', size: '', price: '' };

const Create: React.FC<CreateProps> = ({ onAdd }) => {
    const [isOpen, setIsOpen] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [form, setForm] = useState(emptyForm);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setForm({ ...form, [e.target.name]: e.target.value });
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSaving(true);
        const saved = await onAdd({
            name: form.name.trim(),
            color: form.color.trim(),
            description: form.description.trim() || undefined,
            imageUrl: form.image.trim() || null,
            size: Number(form.size),
            price: Number(form.price),
        });
        setIsSaving(false);

        // On failure the form stays open so nothing typed is lost; the reason is shown on the page.
        if (saved) {
            setIsOpen(false);
            setForm(emptyForm);
        }
    };

    return (
        <div className="gown-create-container">
            <button className="gown-create-toggle-btn" onClick={() => setIsOpen(true)}>Click to add a new gown</button>
            {isOpen && (
                <div className="gown-create-overlay" onClick={() => setIsOpen(false)}>
                    <form className="gown-create-form" onSubmit={handleSubmit} onClick={e => e.stopPropagation()}>
                        <h3 className="gown-create-title">Add New Gown</h3>
                        <div className="gown-create-fields">
                            <input type="text" name="name" value={form.name} onChange={handleChange} placeholder="Gown Name" maxLength={60} required />
                            <input type="text" name="color" value={form.color} onChange={handleChange} placeholder="Color (e.g. Red)" maxLength={30} required />
                            <input type="text" name="description" value={form.description} onChange={handleChange} placeholder="Description (optional)" maxLength={200} />
                            <input type="number" name="size" value={form.size} onChange={handleChange} placeholder="Size (e.g. 36)" min={1} max={100} required />
                            <input type="number" name="price" value={form.price} onChange={handleChange} placeholder="Price (₪)" min={1} max={100000} step="any" required />
                            <input type="url" name="image" value={form.image} onChange={handleChange} placeholder="Image URL (optional)" maxLength={500} />
                        </div>
                        <div className="gown-create-actions">
                            <button type="submit" className="gown-create-submit-btn" disabled={isSaving}>{isSaving ? 'Adding…' : 'Add'}</button>
                            <button type="button" className="gown-create-cancel-btn" onClick={() => setIsOpen(false)}>Cancel</button>
                        </div>
                    </form>
                </div>
            )}
        </div>
    );
}
export default Create;
