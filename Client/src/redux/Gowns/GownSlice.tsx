import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { fetchAllGowns, createGownApi, deleteGownApi, updateGownApi, fetchAllGroupsGowns, updateAvailabilityApi, fetchAvailableGownsByDate } from "../../services/Gowns/task.service";
import GownModel, { GownModelProps } from "../../components/gown/gownModel/GownModel";
import { createRentalApi } from "../../services/Rentals/task.service";
export interface initialStateType {
    gowns: GownModelProps[];
    isLoading: boolean;
    error: string | null;
    Cart: any[];
}

const initialState: initialStateType = {
    gowns: [],
    isLoading: false,
    error: null,
    Cart: [],
};

export const fetchGowns = createAsyncThunk("gowns/fetchGowns", async () => {
    return await fetchAllGowns();
});

export const fetchGroupsGowns = createAsyncThunk("gowns/fetchGroupsGowns", async () => {
    return await fetchAllGroupsGowns();
});

export const addGownThunk = createAsyncThunk("gowns/addGown", async (gownData: GownModelProps) => {
    return await createGownApi(gownData);
});

export const deleteGownThunk = createAsyncThunk("gowns/deleteGown", async (id: number) => {
    await deleteGownApi(id);
    return id;
});

export const updateGownThunk = createAsyncThunk("gowns/updateGown", async (gownData: any) => {
    return await updateGownApi(gownData);
});

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
        addGown: (state, action) => {
            if (state.gowns.find(gown => gown.id === action.payload.id)) {
                const model = state.gowns.find(gown => gown.id === action.payload.id);
                model?.details.push(...action.payload.details);
            }
            else {
                state.gowns.push(action.payload);
            }
        },
        updateGown: (state, action) => {
            const updatedGown = action.payload;
            const index = state.gowns.findIndex(gown => gown.id === updatedGown.id);
            if (index !== -1) {
                state.gowns[index] = updatedGown;
            }
        },
        deleteGown: (state, action) => {
            state.gowns = state.gowns.filter(gown => gown.id !== action.payload);
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
                state.isLoading = true;
            })
            .addCase(fetchGroupsGowns.fulfilled, (state, action) => {
                state.isLoading = false;
                state.gowns = action.payload;
            })
            .addCase(fetchGroupsGowns.rejected, (state, action) => {
                state.error = action.error.message || "Failed to fetch groups gowns";
            })
            .addCase(addGownThunk.fulfilled, (state, action) => {
                const newGown = action.meta.arg;
                const existing = state.gowns.find(gown => gown.id === newGown.id);
                if (existing) {
                    existing.details.push(...newGown.details);
                } else {
                    state.gowns.push(newGown);
                }
            })
            .addCase(addGownThunk.rejected, (state, action) => {
                state.error = action.error.message || "Failed to add gown";
            })
            .addCase(deleteGownThunk.fulfilled, (state, action) => {
                state.gowns = state.gowns.filter(gown => gown.id !== action.payload);
            })
            .addCase(updateGownThunk.fulfilled, (state, action) => {
                const updatedGown = action.payload;
                const index = state.gowns.findIndex(gown => gown.id === updatedGown.id);
                if (index !== -1) {
                    state.gowns[index] = updatedGown;
                }
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

export const { addGown, updateGown, deleteGown, addToCart } = gownSlice.actions;
export default gownSlice.reducer;
