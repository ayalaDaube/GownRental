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
        public IGownModel gownModel { get; }
        public IGownDetails gownDetails { get; }
        public IBLRentals rentals { get; }
        private readonly IDal dal;
        private readonly IRentalDetails rentalDetails;

        public BLGownModelService(IDal gown)
        {
            dal = gown;
            rentalDetails = gown.RentalDetails;
            gownModel = gown.gownModel;
            gownDetails = gown.gownDetail;
            rentals = new BLRentalService(gown);
        }
        /// <summary>
        /// Adds a gown: a new model with its first size, or one more size for an existing model.
        /// The ids are assigned here, never by the caller.
        /// </summary>
        public ModelFullDetaills createGown(NewGownRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var gownId = dal.RunInTransaction(() =>
            {
                int modelId;
                if (request.ModelId is int existingModelId)
                {
                    if (gownModel.GetGownModelById(existingModelId) == null)
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

                    modelId = gownModel.GetNextId();
                    gownModel.createGownModel(new Dal.Models.GownModel
                    {
                        Id = modelId,
                        Name = request.Name.Trim(),
                        Color = request.Color.Trim(),
                        Description = NullIfBlank(request.Description),
                        ImageUrl = NullIfBlank(request.ImageUrl),
                    });
                }

                var newGownId = gownDetails.GetNextId();
                gownDetails.createGownDetails(new Dal.Models.ModelDetails
                {
                    GownId = newGownId,
                    ModelId = modelId,
                    Size = request.Size,
                    Price = request.Price,
                    IsAvailable = true,
                });
                return newGownId;
            });

            return getGownById(gownId)!;
        }

        private static string? NullIfBlank(string? text) =>
            string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        /// <summary>Deletes one size of a gown. The model is removed too once its last size is gone.</summary>
        public void deleteGown(int gownId)
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
                    gownModel.DeleteGownModel(size.ModelId);
                }
            });
        }

        /// <summary>Deletes a model together with all of its sizes, unless one of them was ever rented.</summary>
        public void deleteGownModel(int modelId)
        {
            dal.RunInTransaction(() =>
            {
                if (gownModel.GetGownModelById(modelId) == null)
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
                gownModel.DeleteGownModel(modelId);
            });
        }

        public List<ModelFullDetaills> getFullGowns()
        {
            return (from g in gownModel.GetGownModels()
                    join d in gownDetails.GetGownDetails() on g.Id equals d.ModelId
                    select new ModelFullDetaills
                    {
                        Id = d.GownId,
                        Name = g.Name,
                        Color = g.Color,
                        Description = g.Description,
                        ImageUrl = g.ImageUrl,
                        Price = d.Price,
                        Size = d.Size,
                        ModelId = g.Id,
                        IsAvailable = d.IsAvailable,
                    }).ToList();
        }

        public List<ModelGrouped> markingTheOccupieByDate(DateOnly date)
        {
            var gownIds = rentals.GetRentedGownsByDate(date);
            var allGowns = GetModelGroupeds();
            foreach (var gown in allGowns)
                foreach (var detail in gown.Details)
                    if (gownIds.Contains(detail.GownId))
                        detail.IsAvailable = false;
            return allGowns;
        }

        public ModelFullDetaills? getGownById(int id)
        {
           return (from g in gownModel.GetGownModels()
                    join d in gownDetails.GetGownDetails() on g.Id equals d.ModelId
                    where d.GownId == id
                    select new ModelFullDetaills
                    {
                        Id = d.GownId,
                        Name = g.Name,
                        Color = g.Color,
                        Description = g.Description,
                        ImageUrl = g.ImageUrl,
                        IsAvailable = d.IsAvailable,
                        Price = d.Price,
                        Size = d.Size,
                        ModelId = g.Id,
                    }).FirstOrDefault();
        }

        /// <summary>Changes the size number, price or availability of one size of a gown.</summary>
        public ModelFullDetaills updateGownSize(int gownId, UpdateSizeRequest request)
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

                gownDetails.updateGownDetails(new Dal.Models.ModelDetails
                {
                    GownId = gownId,
                    ModelId = current.ModelId,
                    Size = request.Size,
                    Price = request.Price,
                    IsAvailable = request.IsAvailable,
                });
            });

            return getGownById(gownId)!;
        }

        /// <summary>Changes the name, color, description or image shared by all sizes of a model.</summary>
        public ModelFullDetaills updateGownModel(int modelId, UpdateModelRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (gownModel.GetGownModelById(modelId) == null)
            {
                throw new NotFoundException($"Gown model {modelId} does not exist.");
            }

            gownModel.updateGownModel(new Dal.Models.GownModel
            {
                Id = modelId,
                Name = request.Name.Trim(),
                Color = request.Color.Trim(),
                Description = NullIfBlank(request.Description),
                ImageUrl = NullIfBlank(request.ImageUrl),
            });

            var firstSize = gownDetails.GetByModel(modelId).OrderBy(s => s.GownId).FirstOrDefault();
            return firstSize == null
                ? new ModelFullDetaills { ModelId = modelId, Name = request.Name, Color = request.Color }
                : getGownById(firstSize.GownId)!;
        }

        public List<ModelGrouped> GetModelGroupeds()
        {
            var allGowns = getFullGowns();
            var groups = allGowns.GroupBy(g => g.ModelId);
            var result = groups.Select(group => new ModelGrouped
            {
                Id = group.First().ModelId,
                Name = group.First().Name,
                Color = group.First().Color,
                Description = group.First().Description,
                Image = group.First().ImageUrl,
                Details = group.Select(g => new ModelDetails
                {
                    GownId = g.Id,       
                    ModelId = g.ModelId, 
                    Size = g.Size,
                    Price = g.Price,
                    IsAvailable = g.IsAvailable
                }).ToList()
            }).ToList();

            return result;


        }
    }
}
