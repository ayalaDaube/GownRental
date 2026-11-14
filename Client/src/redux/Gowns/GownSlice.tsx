import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { getApiErrorMessage } from "../../services/apiError";
import { fetchAllGowns, createGownApi, deleteGownApi, deleteGownModelApi, updateGownSizeApi, updateGownModelApi, fetchAllGroupsGowns, updateAvailabilityApi, fetchAvailableGownsByDate, NewGownInput, GownSizeChanges, GownModelChanges } from "../../services/Gowns/task.service";
import GownModel, { GownModelProps } from "../../components/gown/gownModel/GownModel";
import { createRentalApi } from "../../services/Rentals/task.service";
export interface initialStateType {
    gowns: GownModelProps[];
    isLoading: boolean;
    error: string | null;
    notice: { type: 'error' | 'success'; text: string } | null;
    Cart: any[];
}

const initialState: initialStateType = {
    gowns: [],
    isLoading: false,
    error: null,
    notice: null,
    Cart: [],
};

export const fetchGowns = createAsyncThunk("gowns/fetchGowns", async () => {
    return await fetchAllGowns();
});

export const fetchGroupsGowns = createAsyncThunk("gowns/fetchGroupsGowns", async () => {
    return await fetchAllGroupsGowns();
});

export const addGownThunk = createAsyncThunk<unknown, NewGownInput, { rejectValue: string }>(
    "gowns/addGown",
    async (input, { rejectWithValue }) => {
        try {
            return await createGownApi(input);
        } catch (error) {
            return rejectWithValue(getApiErrorMessage(error));
        }
    }
);

export const deleteGownThunk = createAsyncThunk<number, number, { rejectValue: string }>(
    "gowns/deleteGown",
    async (gownId, { rejectWithValue }) => {
        try {
            await deleteGownApi(gownId);
            return gownId;
        } catch (error) {
            return rejectWithValue(getApiErrorMessage(error));
        }
    }
);

export const deleteGownModelThunk = createAsyncThunk<number, number, { rejectValue: string }>(
    "gowns/deleteGownModel",
    async (modelId, { rejectWithValue }) => {
        try {
            await deleteGownModelApi(modelId);
            return modelId;
        } catch (error) {
            return rejectWithValue(getApiErrorMessage(error));
        }
    }
);

export const updateGownSizeThunk = createAsyncThunk<unknown, { gownId: number; changes: GownSizeChanges }, { rejectValue: string }>(
    "gowns/updateGownSize",
    async ({ gownId, changes }, { rejectWithValue }) => {
        try {
            return await updateGownSizeApi(gownId, changes);
        } catch (error) {
            return rejectWithValue(getApiErrorMessage(error));
        }
    }
);

export const updateGownModelThunk = createAsyncThunk<unknown, { modelId: number; changes: GownModelChanges }, { rejectValue: string }>(
    "gowns/updateGownModel",
    async ({ modelId, changes }, { rejectWithValue }) => {
        try {
            return await updateGownModelApi(modelId, changes);
        } catch (error) {
            return rejectWithValue(getApiErrorMessage(error));
        }
    }
);

export const placeOrderThunk = createAsyncThunk("gowns/placeOrder",  async ({ cart, rentalInfo }: { 
    cart: any[], 
    rentalInfo: { rentalId: number, customerId: string, rentalDate: string, eventDate: string } 
}) => {
    console.log(cart);
    for (const item of cart) {
        console.log(item);
        await updateAvailabilityApi(item);
        const newRental = {
            ...rentalInfo,
            gownId: item.gownId,
            size: item.size,
        };
        await createRentalApi(newRental);
    }
    alert("Order Placed Successfully");
});

export const getAvailableGownsByDate = createAsyncThunk("gowns/getAvailableGownsByDate", async (date: string) => {
    return await fetchAvailableGownsByDate(date);
})
const gownSlice = createSlice({
    name: "gowns",
    initialState: initialState,
    reducers: {
        dismissNotice: (state) => {
            state.notice = null;
        },
        addToCart: (state, action) => {
            const item = action.payload;
            const exists = state.Cart.find((c: any) => c.gownId === item.gownId);
            if (exists) {
                state.Cart = state.Cart.filter((c: any) => c.gownId !== item.gownId);
            } else {
                state.Cart.push(item);
                alert("Item added to cart");
            }
        }
    }
    ,
    extraReducers: (builder) => {
        builder
            .addCase(fetchGowns.pending, (state) => {
                state.isLoading = true;
            })
            .addCase(fetchGowns.fulfilled, (state, action) => {
                state.isLoading = false;
                state.gowns = action.payload;
            })
            .addCase(fetchGowns.rejected, (state, action) => {
                state.isLoading = false;
                state.error = action.error.message || "Failed to fetch gowns";
            })
            .addCase(fetchGroupsGowns.pending, (state) => {
                // Show the spinner only for the first load; a refresh after an edit keeps the list (and open cards) in place.
                if (state.gowns.length === 0) {
                    state.isLoading = true;
                }
            })
            .addCase(fetchGroupsGowns.fulfilled, (state, action) => {
                state.isLoading = false;
                state.gowns = action.payload;
            })
            .addCase(fetchGroupsGowns.rejected, (state, action) => {
                state.error = action.error.message || "Failed to fetch groups gowns";
            })
            .addCase(addGownThunk.fulfilled, (state) => {
                state.notice = { type: 'success', text: 'The gown was added.' };
            })
            .addCase(addGownThunk.rejected, (state, action) => {
                state.notice = { type: 'error', text: action.payload ?? 'Could not add the gown.' };
            })
            .addCase(deleteGownThunk.fulfilled, (state, action) => {
                // The payload is the id of one size (gown); drop it, and the model too once it has no sizes left.
                state.gowns.forEach(model => {
                    model.details = model.details.filter(detail => detail.gownId !== action.payload);
                });
                state.gowns = state.gowns.filter(model => model.details.length > 0);
                state.Cart = state.Cart.filter((item: any) => item.gownId !== action.payload);
            })
            .addCase(deleteGownThunk.rejected, (state, action) => {
                state.notice = { type: 'error', text: action.payload ?? 'Could not delete the gown.' };
            })
            .addCase(deleteGownModelThunk.fulfilled, (state, action) => {
                state.gowns = state.gowns.filter(model => model.id !== action.payload);
            })
            .addCase(deleteGownModelThunk.rejected, (state, action) => {
                state.notice = { type: 'error', text: action.payload ?? 'Could not delete the gown model.' };
            })
            .addCase(updateGownSizeThunk.fulfilled, (state) => {
                state.notice = { type: 'success', text: 'The changes were saved.' };
            })
            .addCase(updateGownSizeThunk.rejected, (state, action) => {
                state.notice = { type: 'error', text: action.payload ?? 'Could not save the changes.' };
            })
            .addCase(updateGownModelThunk.fulfilled, (state) => {
                state.notice = { type: 'success', text: 'The changes were saved.' };
            })
            .addCase(updateGownModelThunk.rejected, (state, action) => {
                state.notice = { type: 'error', text: action.payload ?? 'Could not save the changes.' };
            })
            .addCase(placeOrderThunk.fulfilled, (state, action) => {
                state.Cart = [];
                state.isLoading = true;
            })
            .addCase(getAvailableGownsByDate.fulfilled, (state, action) => {
                state.isLoading = false;
                state.gowns = action.payload;
            })
            .addCase(getAvailableGownsByDate.pending, (state) => {
                state.isLoading = true;
            })
            .addCase(getAvailableGownsByDate.rejected, (state, action) => {
                state.isLoading = false;
                state.error = action.error.message || "Failed to fetch gowns";
            });
    }
});

export const { addToCart, dismissNotice } = gownSlice.actions;
export default gownSlice.reducer;
