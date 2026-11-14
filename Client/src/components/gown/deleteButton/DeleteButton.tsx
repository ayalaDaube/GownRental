import React from 'react';
import './DeleteButton.css';
import { FaTrashAlt } from 'react-icons/fa';
import { useDispatch } from 'react-redux';
import { deleteGownModelThunk, fetchGroupsGowns } from '../../../redux/Gowns/GownSlice';

interface DeleteButtonProps {
    gownId: number;
}

const DeleteButton: React.FC<DeleteButtonProps> = ({ gownId }) => {
    const dispatch = useDispatch();

    // gownId is the id of the model card this button belongs to.
    const handleClick = () => {
        if (!window.confirm('Delete this model and all of its sizes?')) return;
        dispatch(deleteGownModelThunk(gownId) as any).then(() => dispatch(fetchGroupsGowns() as any));
    };

    return (
        <button className="gown-delete-btn" title="Delete" onClick={handleClick}>
            <FaTrashAlt />
        </button>
    );
};

export default DeleteButton;
