import React, { useState } from 'react';
import './Update.css';
import { FaEdit } from 'react-icons/fa';
import { useDispatch } from 'react-redux';
import { updateGown } from '../../../../src/redux/Gowns/GownSlice';
import { GownModelProps} from '../../../components/gown/gownModel/GownModel';
const Update: React.FC<{ gown: GownModelProps }> = ({ gown }) => {
    const dispatch = useDispatch();
    const [isEditing, setIsEditing] = useState(false);
    const [updatedGown, setUpdatedGown] = useState(gown);

    const handleEditClick = () => {
        setIsEditing(true);
    };

    const handleSaveClick = () => {
        dispatch(updateGown(updatedGown));
        setIsEditing(false);
    };

    const handleCancelClick = () => {
        setUpdatedGown(gown);
        setIsEditing(false);
    };

    const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
        const { name, value } = e.target;
        setUpdatedGown(prevState => ({
            ...prevState,
            [name]: value
        }));
    };

    return (
        <>
            <button className="gown-update-btn" title="Update" onClick={handleEditClick}>
                <FaEdit />
            </button>
            {isEditing && (
                <div className="gown-update-overlay" onClick={handleCancelClick}>
                    <form className="gown-update-form" onClick={e => e.stopPropagation()} onSubmit={e => { e.preventDefault(); handleSaveClick(); }}>
                        <h3 className="gown-update-title">Update Gown</h3>
                        <div className="gown-update-fields">
                            <input type="text" name="name" value={updatedGown.name} onChange={handleChange} placeholder="Name" />
                            <input type="text" name="color" value={updatedGown.color} onChange={handleChange} placeholder="Color" />
                            <input type="text" name="description" value={updatedGown.description} onChange={handleChange} placeholder="Description" />
                        </div>
                        <div className="gown-update-actions">
                            <button type="submit" className="gown-update-submit-btn">Save</button>
                            <button type="button" className="gown-update-cancel-btn" onClick={handleCancelClick}>Cancel</button>
                        </div>
                    </form>
                </div>
            )}
        </>
    ); 
};

export default Update;
