using Bl.Api;
using Bl.Exceptions;
using Bl.Models;
using Dal.Api;
using Dal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bl.Services
{
    public class BLGownModelService : IBLGowns
    {
        private readonly IGownModel gownModels;
        private readonly IGownDetails gownDetails;
        private readonly IBLRentals rentals;
        private readonly IDal dal;
        private readonly IRentalDetails rentalDetails;

        public BLGownModelService(IDal gown)
        {
            dal = gown;
            rentalDetails = gown.RentalDetails;
            gownModels = gown.GownModels;
            gownDetails = gown.GownDetails;
            rentals = new BLRentalService(gown);
        }
        /// <summary>
        /// Adds a gown: a new model with its first size, or one more size for an existing model.
        /// The ids are assigned here, never by the caller.
        /// </summary>
        public ModelFullDetails CreateGown(NewGownRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var gownId = dal.RunInTransaction(() =>
            {
                int modelId;
                if (request.ModelId is int existingModelId)
                {
                    if (gownModels.GetGownModelById(existingModelId) == null)
                    {
                        throw new NotFoundException($"Gown model {existingModelId} does not exist.");
                    }
                    if (gownDetails.GetByModel(existingModelId).Any(s => s.Size == request.Size))
                    {
                        throw new ConflictException($"Gown model {existingModelId} already has size {request.Size}.");
                    }
                    modelId = existingModelId;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(request.Name))
                    {
                        throw new InvalidRequestException("A name is required for a new gown model.");
                    }
                    if (string.IsNullOrWhiteSpace(request.Color))
                    {
                        throw new InvalidRequestException("A color is required for a new gown model.");
                    }

                    modelId = gownModels.GetNextId();
                    gownModels.CreateGownModel(new Dal.Models.GownModel
                    {
                        Id = modelId,
                        Name = request.Name.Trim(),
                        Color = request.Color.Trim(),
                        Description = NullIfBlank(request.Description),
                        ImageUrl = NullIfBlank(request.ImageUrl),
                    });
                }

                var newGownId = gownDetails.GetNextId();
                gownDetails.CreateGownDetails(new Dal.Models.ModelDetails
                {
                    GownId = newGownId,
                    ModelId = modelId,
                    Size = request.Size,
                    Price = request.Price,
                    IsActive = true,
                });
                return newGownId;
            });

            return GetGownById(gownId)!;
        }

        private static string? NullIfBlank(string? text) =>
            string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        /// <summary>Deletes one size of a gown. The model is removed too once its last size is gone.</summary>
        public void DeleteGown(int gownId)
        {
            dal.RunInTransaction(() =>
            {
                var size = gownDetails.GetGownDetailsById(gownId)
                    ?? throw new NotFoundException($"Gown {gownId} does not exist.");

                if (rentalDetails.ExistsForGown(gownId))
                {
                    throw new ConflictException(
                        $"Gown {gownId} has rental history and cannot be deleted.");
                }

                gownDetails.DeleteGownDetails(gownId);

                if (!gownDetails.GetByModel(size.ModelId).Any())
                {
                    gownModels.DeleteGownModel(size.ModelId);
                }
            });
        }

        /// <summary>Deletes a model together with all of its sizes, unless one of them was ever rented.</summary>
        public void DeleteGownModel(int modelId)
        {
            dal.RunInTransaction(() =>
            {
                if (gownModels.GetGownModelById(modelId) == null)
                {
                    throw new NotFoundException($"Gown model {modelId} does not exist.");
                }

                var sizes = gownDetails.GetByModel(modelId);
                if (rentalDetails.ExistsForAnyGown(sizes.Select(s => s.GownId)))
                {
                    throw new ConflictException(
                        $"Gown model {modelId} has sizes with rental history and cannot be deleted.");
                }

                foreach (var size in sizes)
                {
                    gownDetails.DeleteGownDetails(size.GownId);
                }
                gownModels.DeleteGownModel(modelId);
            });
        }

        public List<ModelFullDetails> GetFullGowns()
        {
            return (from g in gownModels.GetGownModels()
                    join d in gownDetails.GetGownDetails() on g.Id equals d.ModelId
                    select new ModelFullDetails
                    {
                        Id = d.GownId,
                        Name = g.Name ?? string.Empty,
                        Color = g.Color,
                        Description = g.Description,
                        ImageUrl = g.ImageUrl,
                        Price = d.Price,
                        Size = d.Size,
                        ModelId = g.Id,
                        IsActive = d.IsActive,
                    }).ToList();
        }

        /// <summary>
        /// The gowns grouped by model, with each size marked available or not for an event on the given date.
        /// A size is unavailable when it is out of the inventory or rented within the buffer days around the date.
        /// </summary>
        public List<ModelGrouped> GetGroupedGownsForDate(DateOnly date)
        {
            var rentedGownIds = rentals.GetRentedGownsByDate(date).ToHashSet();
            var allGowns = GetGroupedGowns();
            foreach (var size in allGowns.SelectMany(model => model.Details))
            {
                size.IsAvailable = size.IsActive && !rentedGownIds.Contains(size.GownId);
            }
            return allGowns;
        }

        public ModelFullDetails? GetGownById(int id)
        {
            var size = gownDetails.GetGownDetailsById(id);
            var model = size == null ? null : gownModels.GetGownModelById(size.ModelId);
            if (size == null || model == null)
            {
                return null;
            }

            return new ModelFullDetails
            {
                Id = size.GownId,
                Name = model.Name ?? string.Empty,
                Color = model.Color,
                Description = model.Description,
                ImageUrl = model.ImageUrl,
                IsActive = size.IsActive,
                Price = size.Price,
                Size = size.Size,
                ModelId = model.Id,
            };
        }

        /// <summary>Changes the size number, price or availability of one size of a gown.</summary>
        public ModelFullDetails UpdateGownSize(int gownId, UpdateSizeRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            dal.RunInTransaction(() =>
            {
                var current = gownDetails.GetGownDetailsById(gownId)
                    ?? throw new NotFoundException($"Gown {gownId} does not exist.");

                if (gownDetails.GetByModel(current.ModelId).Any(s => s.GownId != gownId && s.Size == request.Size))
                {
                    throw new ConflictException($"Gown model {current.ModelId} already has size {request.Size}.");
                }
                // The rentals of a gown record the size it had. Correcting the size afterwards would make them lie.
                if (request.Size != current.Size && rentalDetails.ExistsForGown(gownId))
                {
                    throw new ConflictException(
                        $"Gown {gownId} has rental history, so its size cannot be changed. Add a new gown for the other size.");
                }

                gownDetails.UpdateGownDetails(new Dal.Models.ModelDetails
                {
                    GownId = gownId,
                    ModelId = current.ModelId,
                    Size = request.Size,
                    Price = request.Price,
                    IsActive = request.IsActive,
                });
            });

            return GetGownById(gownId)!;
        }

        /// <summary>Changes the name, color, description or image shared by all sizes of a model.</summary>
        public ModelFullDetails UpdateGownModel(int modelId, UpdateModelRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (gownModels.GetGownModelById(modelId) == null)
            {
                throw new NotFoundException($"Gown model {modelId} does not exist.");
            }

            gownModels.UpdateGownModel(new Dal.Models.GownModel
            {
                Id = modelId,
                Name = request.Name.Trim(),
                Color = request.Color.Trim(),
                Description = NullIfBlank(request.Description),
                ImageUrl = NullIfBlank(request.ImageUrl),
            });

            var firstSize = gownDetails.GetByModel(modelId).OrderBy(s => s.GownId).FirstOrDefault();
            return firstSize == null
                ? new ModelFullDetails { ModelId = modelId, Name = request.Name, Color = request.Color }
                : GetGownById(firstSize.GownId)!;
        }

        public List<ModelGrouped> GetGroupedGowns()
        {
            var allGowns = GetFullGowns();
            var groups = allGowns.GroupBy(g => g.ModelId).OrderBy(group => group.Key);
            var result = groups.Select(group => new ModelGrouped
            {
                Id = group.First().ModelId,
                Name = group.First().Name,
                Color = group.First().Color ?? string.Empty,
                Description = group.First().Description,
                Image = group.First().ImageUrl,
                Details = group.OrderBy(g => g.Size).Select(g => new GownSizeInfo
                {
                    GownId = g.Id,
                    ModelId = g.ModelId,
                    Size = g.Size,
                    Price = g.Price,
                    IsActive = g.IsActive,
                    IsAvailable = g.IsActive, // without a chosen date, availability is just "in the inventory"
                }).ToList()
            }).ToList();

            return result;


        }
    }
}
