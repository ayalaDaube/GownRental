import React, { useState } from 'react';
import './Update.css';
import { FaEdit } from 'react-icons/fa';
import { useAppDispatch } from '../../../store/hooks';
import { fetchGroupsGowns, updateGownModelThunk } from '../../../redux/Gowns/GownSlice';
import { GownModelProps } from '../../../redux/Gowns/GownTypes';

const Update: React.FC<{ gown: GownModelProps }> = ({ gown }) => {
    const dispatch = useAppDispatch();
    const [isEditing, setIsEditing] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [form, setForm] = useState({
        name: gown.name,
        color: gown.color,
        description: gown.description ?? '',
        image: gown.image ?? '',
    });

    const handleEditClick = () => {
        setForm({ name: gown.name, color: gown.color, description: gown.description ?? '', image: gown.image ?? '' });
        setIsEditing(true);
    };

    const handleSaveClick = async () => {
        setIsSaving(true);
        const result = await dispatch(updateGownModelThunk({
            modelId: gown.id,
            changes: {
                name: form.name.trim(),
                color: form.color.trim(),
                description: form.description.trim() || undefined,
                imageUrl: form.image.trim() || null,
            },
        }));
        setIsSaving(false);

        if (updateGownModelThunk.fulfilled.match(result)) {
            dispatch(fetchGroupsGowns());
            setIsEditing(false);
        }
    };

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setForm({ ...form, [e.target.name]: e.target.value });
    };

    return (
        <>
            <button className="gown-update-btn" title="Update" onClick={handleEditClick}>
                <FaEdit />
            </button>
            {isEditing && (
                <div className="gown-update-overlay" onClick={() => setIsEditing(false)}>
                    <form className="gown-update-form" onClick={e => e.stopPropagation()} onSubmit={e => { e.preventDefault(); handleSaveClick(); }}>
                        <h3 className="gown-update-title">Update Gown</h3>
                        <div className="gown-update-fields">
                            <input type="text" name="name" value={form.name} onChange={handleChange} placeholder="Name" maxLength={60} required />
                            <input type="text" name="color" value={form.color} onChange={handleChange} placeholder="Color" maxLength={30} required />
                            <input type="text" name="description" value={form.description} onChange={handleChange} placeholder="Description" maxLength={200} />
                            <input type="url" name="image" value={form.image} onChange={handleChange} placeholder="Image URL (optional)" maxLength={500} />
                        </div>
                        <div className="gown-update-actions">
                            <button type="submit" className="gown-update-submit-btn" disabled={isSaving}>{isSaving ? 'Saving…' : 'Save'}</button>
                            <button type="button" className="gown-update-cancel-btn" onClick={() => setIsEditing(false)}>Cancel</button>
                        </div>
                    </form>
                </div>
            )}
        </>
    );
};

export default Update;
