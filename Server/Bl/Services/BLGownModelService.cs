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
        public void createGown(ModelFullDetaills model)
        {
            var isExisting = gownDetails.GetGownDetails().FirstOrDefault(g => g.GownId == model.Id);
            if (isExisting == null)
            {

                gownModel.createGownModel(new Dal.Models.GownModel
                {
                    Id = model.ModelId,
                    Name = model.Name,
                    Color = model.Color,
                    Description = model.Description,
                    ImageUrl = model.ImageUrl,
                });
                gownDetails.createGownDetails(new Dal.Models.ModelDetails
                {
                    GownId = model.Id,
                    Price = model.Price,
                    Size = model.Size,
                    ModelId = model.ModelId,
                    IsAvailable = model.IsAvailable,
                });
            }
            else
            {
                throw new ArgumentException("this gown already exist");
            }
            }
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

        public void updateGown(ModelFullDetaills model)
        {
            gownModel.updateGownModel(new Dal.Models.GownModel
            {
                Id = model.ModelId,
                Name = model.Name,
                Color = model.Color,
                Description = model.Description,
                ImageUrl = model.ImageUrl,
            });

            gownDetails.updateGownDetails(new Dal.Models.ModelDetails
            {
                GownId = model.Id,
                Price = model.Price,
                Size = model.Size,
                ModelId = model.ModelId,
                IsAvailable = model.IsAvailable,
            });
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
