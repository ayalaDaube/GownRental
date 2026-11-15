import React from 'react';
import './DeleteButton.css';
import { FaTrashAlt } from 'react-icons/fa';
import { useAppDispatch } from '../../../store/hooks';
import { deleteRentalThunk, getReturning, getRenting } from '../../../redux/Rentals/RentalSlice';

interface DeleteButtonProps {
    rentalId: number;
}

const DeleteButton: React.FC<DeleteButtonProps> = ({ rentalId }) => {
    const dispatch = useAppDispatch();
    return (
        <button className="delete-btn" title="Delete" onClick={() => {
            if (!window.confirm(`Delete rental #${rentalId}?`)) return;
            dispatch(deleteRentalThunk(rentalId)).then(() => {
                dispatch(getReturning());
                dispatch(getRenting());
            });
        }}>
            <FaTrashAlt />
        </button>
    );
};

export default DeleteButton;