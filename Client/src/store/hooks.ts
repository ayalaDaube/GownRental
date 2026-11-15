import { useDispatch, useSelector } from 'react-redux';
import type { AppDispatch, RootState } from './store';

// Use these instead of the plain react-redux hooks: they know the shape of the store and accept thunks,
// so components need no casts.
export const useAppDispatch = useDispatch.withTypes<AppDispatch>();
export const useAppSelector = useSelector.withTypes<RootState>();
