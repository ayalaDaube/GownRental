import React from 'react';
import './DeleteButton.css';
import { FaTrashAlt } from 'react-icons/fa';
import { useDispatch } from 'react-redux';
import { deleteGown } from '../../../redux/Gowns/GownSlice';

interface DeleteButtonProps {
    gownId: number;
}

const DeleteButton: React.FC<DeleteButtonProps> = ({ gownId }) => {
    const dispatch = useDispatch();
    return (
        <button className="gown-delete-btn" title="Delete" onClick={() => dispatch(deleteGown(gownId))}>
            <FaTrashAlt />
        </button>
    );
};

export default DeleteButton;
