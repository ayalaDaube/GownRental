import React from 'react';
import './DeleteButton.css';
import { FaTrashAlt } from 'react-icons/fa';
import { useDispatch } from 'react-redux';
import { deleteRentalThunk, getReturning, getRenting } from '../../../redux/Rentals/RentalSlice';

interface DeleteButtonProps {
    rentalId: number;
}

const DeleteButton: React.FC<DeleteButtonProps> = ({ rentalId }) => {
    const dispatch = useDispatch();
    return (
        <button className="delete-btn" title="Delete" onClick={() => {
            if (!window.confirm(`Delete rental #${rentalId}?`)) return;
            dispatch(deleteRentalThunk(rentalId) as any).then(() => {
                dispatch(getReturning() as any);
                dispatch(getRenting() as any);
            });
        }}>
            <FaTrashAlt />
        </button>
    );
};

export default DeleteButton;