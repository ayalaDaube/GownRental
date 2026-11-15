import React from 'react';
import './DeleteButton.css';
import { FaTrashAlt } from 'react-icons/fa';
import { useAppDispatch } from '../../../store/hooks';
import { deleteGownModelThunk, fetchGroupsGowns } from '../../../redux/Gowns/GownSlice';

interface DeleteButtonProps {
    gownId: number;
}

const DeleteButton: React.FC<DeleteButtonProps> = ({ gownId }) => {
    const dispatch = useAppDispatch();

    // gownId is the id of the model card this button belongs to.
    const handleClick = () => {
        if (!window.confirm('Delete this model and all of its sizes?')) return;
        dispatch(deleteGownModelThunk(gownId)).then(() => dispatch(fetchGroupsGowns()));
    };

    return (
        <button className="gown-delete-btn" title="Delete" onClick={handleClick}>
            <FaTrashAlt />
        </button>
    );
};

export default DeleteButton;
