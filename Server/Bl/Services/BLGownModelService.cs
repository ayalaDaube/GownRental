using Bl.Api;
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
        public BLGownModelService(IDal gown)
        {
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
        // the id of the details
        public void deleteGown(int id)
        {
            ModelDetails g = gownDetails.GetGownDetailsById(id);
            if (g != null)
            {
                gownDetails.DeleteGownDetails(id);
                gownModel.DeleteGownModel(g.ModelId);
            }
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
